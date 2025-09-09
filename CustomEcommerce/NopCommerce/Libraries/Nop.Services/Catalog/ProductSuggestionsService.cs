using LinqToDB;
using LinqToDB.Data;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Logging;

namespace Nop.Services.Catalog;

public class ProductSuggestionsService(
    INopDataProvider dataProvider,
    IStaticCacheManager cacheManager,
    ILogger logger) : IProductSuggestionsService
{
    private readonly INopDataProvider _data = dataProvider;
    private readonly IStaticCacheManager _cacheManager = cacheManager;
    private readonly ILogger _logger = logger;

    private const int MAX_SUGGESTIONS = 10;
    private const int MIN_QUERY_LENGTH = 2;

    public async Task<IList<ProductSuggestion>> GetSuggestionsAsync(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < MIN_QUERY_LENGTH)
                return new List<ProductSuggestion>();

            var normalizedQuery = query.Trim();

            var suggestions = await GetContainsSuggestionsAsync(normalizedQuery);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error obteniendo sugerencias para query: '{query}'", ex);
            return new List<ProductSuggestion>();
        }
    }

    private async Task<IList<ProductSuggestion>> GetContainsSuggestionsAsync(string query)
    {
        try
        {
            var containsQuery = BuildContainsQuery(query);

            var parameters = new List<DataParameter>
                {
                    new("@ContainsQuery", containsQuery),
                    new("@MaxResults", MAX_SUGGESTIONS)
                };

            var results = await _data.QueryProcAsync<ProductSuggestionDb>(
                "dbo.GetProductSuggestions", parameters.ToArray());

            return results
                .Select(r => new ProductSuggestion
                {
                    Id = r.Id,
                    Name = r.Name,
                    Relevance = NormalizeRank(r.FtRank)
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ejecutando consulta CONTAINS para query: '{query}'", ex);
            return new List<ProductSuggestion>();
        }
    }

    private static string BuildContainsQuery(string query)
    {
        // Escape básico y agregar wildcard para autocompletado
        var escaped = query.Replace("\"", "\"\"").Replace("'", "''");
        return $"\"{escaped}*\"";
    }

    private static double NormalizeRank(int ftRank)
        => Math.Max(0, Math.Min(1, ftRank / 1000.0));
}