using LinqToDB;
using LinqToDB.Data;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Logging;

namespace Nop.Services.Catalog;

public class ProductSuggestionsService : IProductSuggestionsService
{
    private readonly INopDataProvider _data;
    private readonly IStaticCacheManager _cacheManager;
    private readonly ILogger _logger;

    private const int MAX_SUGGESTIONS = 10;
    private const int MIN_QUERY_LENGTH = 3;

    public ProductSuggestionsService(
        INopDataProvider dataProvider,
        IStaticCacheManager cacheManager,
        ILogger logger)
    {
        _data = dataProvider;
        _cacheManager = cacheManager;
        _logger = logger;
    }

    public async Task<IList<ProductSuggestion>> GetSuggestionsAsync(string query)
    {
        try
        {
            // Validación básica
            if (string.IsNullOrWhiteSpace(query) || query.Length < MIN_QUERY_LENGTH)
                return new List<ProductSuggestion>();

            var normalizedQuery = query.Trim().ToLower();

            var cacheKey = _cacheManager.PrepareKey(SuggestionsCacheDefaults.SuggestionsModelKey, normalizedQuery);

            var cachedResults = await _cacheManager.GetAsync<List<ProductSuggestion>>(cacheKey);
            if (cachedResults != null)
            {
                return cachedResults;
            }

            var suggestions = await GetFtsSuggestionsAsync(query);

            await _cacheManager.SetAsync(cacheKey, suggestions);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error obteniendo sugerencias para query: '{query}'", ex);
            return new List<ProductSuggestion>();
        }
    }

    private async Task<IList<ProductSuggestion>> GetFtsSuggestionsAsync(string query)
    {
        try
        {
            // Construir query FTS
            var ftsQuery = BuildSimpleFtsQuery(query);

            var parameters = new List<DataParameter>
            {
                new("@FtsQuery", ftsQuery),
                new("@MaxResults", MAX_SUGGESTIONS * 2)
            };

            var results = await _data.QueryProcAsync<ProductSuggestionDb>(
                "dbo.GetProductSuggestions", parameters.ToArray());

            return results
                .Where(r => r.FtRank > 20)
                .Take(MAX_SUGGESTIONS)
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
            _logger.Error($"Error ejecutando consulta FTS para query: '{query}'", ex);
            return new List<ProductSuggestion>();
        }
    }

    public async Task ClearSuggestionsCache()
    {
        try
        {
            await _cacheManager.RemoveByPrefixAsync(SuggestionsCacheDefaults.SuggestionsPrefix);
            _logger.Information("Cache de sugerencias limpiado");
        }
        catch (Exception ex)
        {
            _logger.Error("Error limpiando cache de sugerencias", ex);
        }
    }

    // Query FTS
    private static string BuildSimpleFtsQuery(string query)
    {
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 2)
            .Select(w => $"\"{w}*\"")
            .ToArray();

        return words.Length > 0 ? string.Join(" OR ", words) : query;
    }

    private static double NormalizeRank(int ftRank)
    {
        return Math.Max(0, Math.Min(1, ftRank / 1000.0));
    }

    private static class SuggestionsCacheDefaults
    {
        public static CacheKey SuggestionsModelKey => new("nop.product.suggestions.{0}", SuggestionsPrefix);

        public static string SuggestionsPrefix = "nop.product.suggestions";

        public static int CacheTime { get; set; } = 15;
    }
}