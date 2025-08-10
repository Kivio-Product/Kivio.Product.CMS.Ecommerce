using Nop.Data;
using Nop.Core.Domain.Catalog;
using System.Text.RegularExpressions;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;

namespace Nop.Services.Catalog;

public partial class ProductSimilarityService(INopDataProvider dataProvider, ILogger<ProductSimilarityService> logger) : IProductSimilarityService
{
    private readonly INopDataProvider _dataProvider = dataProvider;
    private readonly ILogger<ProductSimilarityService> _logger = logger;

    private static readonly Regex _tokenRegex = new(@"\b(?:\d+[a-zA-Z]+|[a-zA-Z]{2,})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Regex para detectar unidades de medida con números
    private static readonly Regex _measurementRegex = new(
        @"\b(\d+(?:[.,]\d+)?)\s*(?:x\s*(\d+(?:[.,]\d+)?))?\s*(ml|cc|l|litros?|gr?|kg|kilogramos?|mg|miligramos?|g|gramos?|oz|onzas?|lb|libras?|mm|cm|m|metros?|pulg|pulgadas?|''|""|in|inches?|ft|pies?|unid|unidades?|pzs?|piezas?|und)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "DE", "LA", "EL", "Y", "CON", "PARA", "EN", "UN", "UNA", "LOS", "LAS", "DEL", "AL",
        "POR", "SIN"
    };

    public async Task<IList<ProductMatch>> FindSimilarByNameAsync(
        string productName,
        decimal? originalPrice = null,
        int maxResults = 10,
        double minJaccardScore = 0.3,
        int maxDbCandidates = 200,
        double? maxPriceDifferencePercent = null,
        bool strictMeasurementMatching = true)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be null or empty", nameof(productName));

        if (productName.Length < 2)
            throw new ArgumentException("Product name must be at least 2 characters long", nameof(productName));

        if (originalPrice.HasValue && originalPrice.Value < 0)
            throw new ArgumentException("Price cannot be negative", nameof(originalPrice));

        if (maxResults <= 0)
            throw new ArgumentException("Max results must be greater than 0", nameof(maxResults));

        if (minJaccardScore < 0 || minJaccardScore > 1)
            throw new ArgumentException("Jaccard score must be between 0 and 1", nameof(minJaccardScore));

        var tokens = ExtractTokens(productName);
        var measurements = ExtractMeasurements(productName);

        if (tokens.Count == 0)
        {
            _logger.LogWarning("No valid tokens extracted from product name: '{ProductName}'", productName);
            return new List<ProductMatch>();
        }

        _logger.LogDebug("Extracted measurements from '{ProductName}': {Measurements}", 
            productName, string.Join(", ", measurements.Select(m => m.ToString())));

        var ftsQuery = BuildFtsQuery(tokens);
        if (string.IsNullOrEmpty(ftsQuery))
        {
            _logger.LogWarning("No valid FTS query could be built from product name: '{ProductName}'", productName);
            return new List<ProductMatch>();
        }

        var parameters = new[]
        {
            new DataParameter("@FtsQuery", ftsQuery),
            new DataParameter("@MaxCandidates", maxDbCandidates)
        };

        var candidates = await _dataProvider.QueryProcAsync<ProductCandidate>(
            "dbo.GetProductsByFtsQuery", parameters);

        if (!candidates.Any())
        {
            _logger.LogInformation("No candidates found for product name: '{ProductName}'", productName);
            return new List<ProductMatch>();
        }

        _logger.LogDebug("Found {CandidateCount} initial candidates for query '{ProductName}'",
            candidates.Count, productName);

        var priceFilteredCandidates = candidates;
        if (originalPrice.HasValue && maxPriceDifferencePercent.HasValue)
        {
            priceFilteredCandidates = candidates.Where(c =>
            {
                var priceDiff = Math.Abs(c.Price - originalPrice.Value);
                var percentDiff = (originalPrice.Value > 0) ? (priceDiff / originalPrice.Value) * 100 : 100;
                return percentDiff <= (decimal)maxPriceDifferencePercent.Value;
            }).ToList();

            _logger.LogDebug("After price filter: {FilteredCount} candidates remain",
                priceFilteredCandidates.Count);
        }

        var results = new List<ProductMatch>();
        var jaccardFilterCount = 0;
        var measurementFilterCount = 0;

        foreach (var candidate in priceFilteredCandidates)
        {
            var candidateTokens = ExtractTokens(candidate.Name);
            var candidateMeasurements = ExtractMeasurements(candidate.Name);
            
            var jaccard = CalculateJaccardSimilarity(tokens, candidateTokens);

            // PRIMER FILTRO: Jaccard
            if (jaccard < minJaccardScore)
                continue;

            jaccardFilterCount++;

            // SEGUNDO FILTRO: Verificación de unidades de medida
            if (strictMeasurementMatching && measurements.Any())
            {
                var measurementCompatible = AreMeasurementsCompatible(measurements, candidateMeasurements);
                if (!measurementCompatible)
                {
                    _logger.LogDebug("Candidate '{CandidateName}' rejected due to measurement mismatch. Original: [{OriginalMeasurements}], Candidate: [{CandidateMeasurements}]",
                        candidate.Name, 
                        string.Join(", ", measurements.Select(m => m.ToString())),
                        string.Join(", ", candidateMeasurements.Select(m => m.ToString())));
                    continue;
                }
            }

            measurementFilterCount++;

            // TERCER FILTRO: Levenshtein
            var levenshtein = NormalizedLevenshtein(
                productName.ToUpperInvariant(),
                candidate.Name.ToUpperInvariant());

            var combined = CalculateCombinedScore(jaccard, candidate.FtRank, levenshtein);

            results.Add(new ProductMatch
            {
                Product = candidate,
                JaccardSimilarity = jaccard,
                LevenshteinSimilarity = levenshtein,
                CombinedScore = combined,
                MeasurementMatch = AreMeasurementsCompatible(measurements, candidateMeasurements)
            });
        }

        _logger.LogDebug("Jaccard filter passed: {JaccardCount} candidates, Measurement filter passed: {MeasurementCount} candidates, Final results: {ResultCount}",
            jaccardFilterCount, measurementFilterCount, results.Count);

        return results
            .OrderByDescending(r => r.CombinedScore)
            .ThenByDescending(r => r.JaccardSimilarity)
            .Take(maxResults)
            .ToList();
    }

    private static List<MeasurementInfo> ExtractMeasurements(string input)
    {
        var measurements = new List<MeasurementInfo>();
        if (string.IsNullOrWhiteSpace(input)) return measurements;

        var matches = _measurementRegex.Matches(input);

        foreach (Match match in matches)
        {
            var value1Str = match.Groups[1].Value.Replace(',', '.');
            var value2Str = match.Groups[2].Success ? match.Groups[2].Value.Replace(',', '.') : null;
            var unit = match.Groups[3].Value.ToLowerInvariant();

            if (decimal.TryParse(value1Str, out var value1))
            {
                var normalizedUnit = NormalizeUnit(unit);
                measurements.Add(new MeasurementInfo
                {
                    Value = value1,
                    SecondaryValue = value2Str != null && decimal.TryParse(value2Str, out var v2) ? v2 : null,
                    Unit = normalizedUnit,
                    OriginalText = match.Value
                });
            }
        }

        return measurements;
    }

    private static string NormalizeUnit(string unit)
    {
        var normalizedUnit = unit.ToLowerInvariant().Trim();
        
        // Normalizar unidades de volumen
        if (normalizedUnit is "ml" or "cc") return "ml";
        if (normalizedUnit is "l" or "litros" or "litro") return "l";
        
        // Normalizar unidades de peso
        if (normalizedUnit is "g" or "gr" or "gramos" or "gramo") return "g";
        if (normalizedUnit is "kg" or "kilogramos" or "kilogramo") return "kg";
        if (normalizedUnit is "mg" or "miligramos" or "miligramo") return "mg";
        
        // Normalizar unidades de longitud
        if (normalizedUnit is "mm") return "mm";
        if (normalizedUnit is "cm") return "cm";
        if (normalizedUnit is "m" or "metros" or "metro") return "m";
        if (normalizedUnit is "pulg" or "pulgadas" or "pulgada" or "''" or "\"" or "in" or "inches" or "inch") return "in";
        
        // Normalizar unidades de cantidad
        if (normalizedUnit is "unid" or "unidades" or "unidad" or "und" or "pzs" or "pz" or "piezas" or "pieza") return "unit";
        
        return normalizedUnit;
    }

    private static bool AreMeasurementsCompatible(List<MeasurementInfo> measurements1, List<MeasurementInfo> measurements2)
    {
        // Si no hay medidas en el producto original, no aplicar filtro
        if (!measurements1.Any()) return true;
        
        // Si el producto original tiene medidas pero el candidato no, rechazar
        if (measurements1.Any() && !measurements2.Any()) return false;

        // Verificar que al menos una medida sea compatible
        foreach (var m1 in measurements1)
        {
            foreach (var m2 in measurements2)
            {
                if (IsSameMeasurementType(m1.Unit, m2.Unit))
                {
                    // Convertir a la misma unidad base para comparar
                    var convertedValue1 = ConvertToBaseUnit(m1.Value, m1.Unit);
                    var convertedValue2 = ConvertToBaseUnit(m2.Value, m2.Unit);
                    
                    // Permitir una tolerancia del 5% en las medidas
                    var tolerance = Math.Max(convertedValue1, convertedValue2) * 0.05m;
                    if (Math.Abs(convertedValue1 - convertedValue2) <= tolerance)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsSameMeasurementType(string unit1, string unit2)
    {
        var volumeUnits = new HashSet<string> { "ml", "l" };
        var weightUnits = new HashSet<string> { "g", "kg", "mg" };
        var lengthUnits = new HashSet<string> { "mm", "cm", "m", "in" };
        var countUnits = new HashSet<string> { "unit" };

        return (volumeUnits.Contains(unit1) && volumeUnits.Contains(unit2)) ||
               (weightUnits.Contains(unit1) && weightUnits.Contains(unit2)) ||
               (lengthUnits.Contains(unit1) && lengthUnits.Contains(unit2)) ||
               (countUnits.Contains(unit1) && countUnits.Contains(unit2));
    }

    private static decimal ConvertToBaseUnit(decimal value, string unit)
    {
        return unit switch
        {
            // Volumen - base: ml
            "ml" => value,
            "l" => value * 1000m,
            
            // Peso - base: g
            "g" => value,
            "kg" => value * 1000m,
            "mg" => value / 1000m,
            
            // Longitud - base: mm
            "mm" => value,
            "cm" => value * 10m,
            "m" => value * 1000m,
            "in" => value * 25.4m,
            
            // Cantidad - base: unidad
            "unit" => value,
            
            _ => value
        };
    }

    private static double CalculateCombinedScore(
        double jaccardSimilarity,
        double ftRank,
        double levenshteinSimilarity,
        ProductSimilarityMode mode = ProductSimilarityMode.DuplicateDetection)
    {
        var (jaccardWeight, ftWeight, levenshteinWeight) = mode switch
        {
            ProductSimilarityMode.DuplicateDetection => (0.70, 0.10, 0.20),
            ProductSimilarityMode.RelatedProducts => (0.50, 0.35, 0.15),
            ProductSimilarityMode.Balanced => (0.65, 0.15, 0.20),
            _ => (0.65, 0.15, 0.20)
        };

        var ftRankNormalized = Math.Clamp(ftRank / 1000.0, 0.0, 1.0);

        return (jaccardSimilarity * jaccardWeight) +
               (ftRankNormalized * ftWeight) +
               (levenshteinSimilarity * levenshteinWeight);
    }

    private static HashSet<string> ExtractTokens(string input)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(input)) return tokens;

        var matches = _tokenRegex.Matches(input);

        foreach (Match match in matches)
        {
            var token = match.Value.Trim();
            if (!IsStopWord(token) && token.Length >= 2)
                tokens.Add(token);
        }

        return tokens;
    }

    private static string BuildFtsQuery(IEnumerable<string> tokens)
    {
        var safeTokens = tokens
            .Select(t => Regex.Replace(t, @"[^\p{L}\p{N}]", ""))
            .Where(t => t.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(t => $"\"{t}*\"");

        return string.Join(" OR ", safeTokens);
    }

    private static double CalculateJaccardSimilarity(HashSet<string> tokensA, HashSet<string> tokensB)
    {
        if (tokensA.Count == 0 || tokensB.Count == 0) return 0.0;

        var intersection = tokensA.Intersect(tokensB, StringComparer.OrdinalIgnoreCase).Count();
        var union = tokensA.Union(tokensB, StringComparer.OrdinalIgnoreCase).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    private static double NormalizedLevenshtein(string s1, string s2)
    {
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        return maxLength == 0 ? 1.0 : 1.0 - ((double)distance / maxLength);
    }

    private static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        var d = new int[s.Length + 1, t.Length + 1];

        for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= t.Length; j++) d[0, j] = j;

        for (int i = 1; i <= s.Length; i++)
        {
            for (int j = 1; j <= t.Length; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[s.Length, t.Length];
    }

    private static bool IsStopWord(string word)
    {
        return _stopWords.Contains(word);
    }
}

// Clase para almacenar información de medidas
public class MeasurementInfo
{
    public decimal Value { get; set; }
    public decimal? SecondaryValue { get; set; } // Para casos como "10 x 5 cm"
    public string Unit { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    
    public override string ToString()
    {
        return SecondaryValue.HasValue 
            ? $"{Value}x{SecondaryValue}{Unit}" 
            : $"{Value}{Unit}";
    }
}