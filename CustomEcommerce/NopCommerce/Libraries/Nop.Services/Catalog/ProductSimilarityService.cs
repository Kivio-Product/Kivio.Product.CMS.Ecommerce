using System.Data;
using System.Globalization;
using LinqToDB.Data;
using Nop.Core.Domain.Catalog;
using Nop.Data;

namespace Nop.Services.Catalog;

public partial class ProductSimilarityService(
    INopDataProvider dataProvider,
    IProductTokenizationService tokenizationService) : IProductSimilarityService
{
    private readonly INopDataProvider _data = dataProvider;
    private readonly IProductTokenizationService _tokenizer = tokenizationService;

    public bool UseWeightedFts { get; set; } = true;
    public int MaxDbCandidates { get; set; } = 200;
    public double JaccardSoft { get; set; } = 0.35;
    public double JaccardStrict { get; set; } = 0.50;
    public double MaxPriceDiffSoftPct { get; set; } = 0.30;
    public double MaxPriceDiffStrictPct { get; set; } = 0.20;

    private sealed record TokenIdfRow(string Token, double Idf);

    
    public Task<IList<ProductMatch>> FindSimilarForSearchAsync(string productName, decimal? originalPrice = null, int maxResults = 10)
    {
        return FindAsync(productName, originalPrice, maxResults, strict: false);
    }

    public Task<IList<ProductMatch>> FindDuplicatesStrictAsync(string productName, decimal? originalPrice = null, int maxResults = 10)
    {
        return FindAsync(productName, originalPrice, maxResults, strict: true);
    }

    public Task<IList<ProductMatch>> FindSimilarByNameAsync(string productName, decimal? originalPrice = null, int maxResults = 10,
        double minJaccardScore = 0.3, int maxDbCandidates = 200, double? maxPriceDifferencePercent = null, bool strictMeasurementMatching = true)
    {
        return FindDuplicatesStrictAsync(productName, originalPrice, maxResults);
    }

    
    private async Task<IList<ProductMatch>> FindAsync(string name, decimal? originalPrice, int maxResults, bool strict)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new List<ProductMatch>();

        var nameNorm = _tokenizer.Normalize(name);
        var qTokens = _tokenizer.Tokenize(name);

        if (!qTokens.Any())
            return new List<ProductMatch>();

        var idf = await LoadIdfAsync();
        var (minIdf, maxIdf) = GetMinMaxIdf(idf);

        var ftsQuery = UseWeightedFts
            ? BuildIsAboutQuery(qTokens, idf, minIdf, maxIdf)
            : BuildSimpleOrQuery(qTokens);

        double pricePct = strict ? MaxPriceDiffStrictPct : MaxPriceDiffSoftPct;

        Console.WriteLine($"FTS Query: {ftsQuery}");
        Console.WriteLine($"Query tokens: [{string.Join(", ", qTokens)}]");

        var parameters = new List<DataParameter>
        {
            new("@FtsQuery", ftsQuery),
            new("@MaxCandidates", MaxDbCandidates),
            new("@Price", originalPrice ?? (object)DBNull.Value),
            new("@MaxPriceDiffPercent", originalPrice.HasValue ? pricePct : (object)DBNull.Value)
        };

        var rows = await _data.QueryProcAsync<ProductCandidate>(
            "dbo.GetProductsByFtsQuery", parameters.ToArray());

        var list = new List<ProductMatch>();

        foreach (var r in rows)
        {
            var nameC = _tokenizer.Normalize(r.Name);
            var cTokens = _tokenizer.Tokenize(r.Name);

            var jw = WeightedJaccard(qTokens, cTokens, idf);
            var lev = NormalizedLevenshtein(nameNorm, nameC);
            var ft = NormalizeRank(r.FtRank);

            double score = 0.70 * jw + 0.10 * ft + 0.20 * (1.0 - lev);

            double thr = strict ? JaccardStrict : JaccardSoft;
            if (jw >= thr || score >= (strict ? 0.60 : 0.45))
                list.Add(new ProductMatch
                {
                    Product = new ProductCandidate
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Sku = r.Sku ?? string.Empty,
                        Price = r.Price,
                        ShortDescription = r.ShortDescription ?? string.Empty,
                        FtRank = r.FtRank
                    },
                    JaccardSimilarity = jw,
                    LevenshteinSimilarity = lev,
                    MeasurementMatch = true,
                    CombinedScore = score
                });
        }

        return list
            .OrderByDescending(x => x.CombinedScore)
            .Take(maxResults)
            .ToList();
    }

    private static double WeightedJaccard(HashSet<string> a, HashSet<string> b, Dictionary<string, double> idf)
    {
        if (a.Count == 0 && b.Count == 0)
            return 0;

        double intersection = 0, union = 0;
        var allTokens = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
        allTokens.UnionWith(b);

        foreach (var token in allTokens)
        {
            var weight = idf.TryGetValue(token, out var idfValue) ? idfValue : 1.0;
            union += weight;

            if (a.Contains(token) && b.Contains(token))
                intersection += weight;
        }

        return union == 0 ? 0 : intersection / union;
    }

    private static double NormalizedLevenshtein(string a, string b)
    {
        int n = a.Length, m = b.Length;
        if (n == 0)
            return m > 0 ? 1 : 0;
        if (m == 0)
            return n > 0 ? 1 : 0;

        var dp = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++)
            dp[i, 0] = i;
        for (int j = 0; j <= m; j++)
            dp[0, j] = j;

        for (int i = 1; i <= n; i++)
            for (int j = 1; j <= m; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }

        return (double)dp[n, m] / Math.Max(n, m);
    }

    private static double NormalizeRank(int ftRank) => Math.Tanh(ftRank / 400.0);

    private async Task<Dictionary<string, double>> LoadIdfAsync()
    {
        var rows = await _data.QueryAsync<TokenIdfRow>(
            "SELECT Token, Idf FROM dbo.TokenStats WITH (NOLOCK)");
        var dictionary = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine($"Loaded {rows.Count} IDF rows");

        foreach (var row in rows)
            dictionary[row.Token] = row.Idf;

        return dictionary;
    }

    private static (double min, double max) GetMinMaxIdf(IDictionary<string, double> idf)
    {
        if (idf.Count == 0)
            return (1, 7);

        double min = double.PositiveInfinity, max = double.NegativeInfinity;

        foreach (var value in idf.Values)
        {
            if (value < min) min = value;
            if (value > max) max = value;
        }

        if (double.IsInfinity(min) || double.IsInfinity(max))
            return (1, 7);

        return (min, max);
    }

    private static string BuildSimpleOrQuery(HashSet<string> tokens)
    {
        if (!tokens.Any())
            return "\"placeholder\""; 

        var parts = tokens.Select(t => $"\"{t}*\"");
        return string.Join(" OR ", parts);
    }

    private static string BuildIsAboutQuery(HashSet<string> tokens, IDictionary<string, double> idf, double minIdf, double maxIdf)
    {
        if (!tokens.Any())
            return "\"placeholder\"";

        double MapIdfToWeight(double idfValue)
        {
            var normalized = (idfValue - minIdf) / (maxIdf - minIdf + 1e-9);
            return Math.Clamp(normalized, 0, 1);
        }

        var parts = tokens.Select(token =>
        {
            var weight = idf.TryGetValue(token, out var idfValue) ? MapIdfToWeight(idfValue) : 0.10;
            return $"\"{token}*\" WEIGHT({weight.ToString(CultureInfo.InvariantCulture)})";
        });

        return $"ISABOUT({string.Join(", ", parts)})";
    }
}