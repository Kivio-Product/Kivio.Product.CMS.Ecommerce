using LinqToDB;
using LinqToDB.Data;
using Nop.Core.Caching;
using Nop.Core;
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

    public async Task<IPagedList<ProductSuggestion>> GetSuggestionsAsync(string query, int pageIndex = 0, int pageSize = 0)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < MIN_QUERY_LENGTH)
                return new PagedList<ProductSuggestion>(new List<ProductSuggestion>(), pageIndex, pageSize > 0 ? pageSize : MAX_SUGGESTIONS);

            var normalizedQuery = query.Trim();

            if (pageSize <= 0)
            {
                pageSize = MAX_SUGGESTIONS;
                pageIndex = 0;
            }

            var suggestions = await GetContainsSuggestionsAsync(normalizedQuery, pageIndex, pageSize);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error obteniendo sugerencias para query: '{query}'", ex);
            return new PagedList<ProductSuggestion>(new List<ProductSuggestion>(), pageIndex, pageSize > 0 ? pageSize : MAX_SUGGESTIONS);
        }
    }

    private async Task<IPagedList<ProductSuggestion>> GetContainsSuggestionsAsync(string query, int pageIndex, int pageSize)
    {
        try
        {
            var containsQuery = BuildContainsQuery(query);

            var countParameters = new List<DataParameter>
            {
                new("@ContainsQuery", containsQuery)
            };

            var totalCount = await _data.QueryProcAsync<int>(
                "dbo.GetProductSuggestionsCount", countParameters.ToArray());

            var total = totalCount.FirstOrDefault();

            if (total == 0)
                return new PagedList<ProductSuggestion>(new List<ProductSuggestion>(), pageIndex, pageSize);

            var parameters = new List<DataParameter>
            {
                new("@ContainsQuery", containsQuery),
                new("@PageIndex", pageIndex),
                new("@PageSize", pageSize)
            };

            var results = await _data.QueryProcAsync<ProductSuggestionDb>(
                "dbo.GetProductSuggestionsPaged", parameters.ToArray());

            var suggestions = results
                .Select(r => new ProductSuggestion
                {
                    Id = r.Id,
                    Name = r.Name,
                    Relevance = NormalizeRank(r.FtRank)
                })
                .ToList();

            return new PagedList<ProductSuggestion>(suggestions, pageIndex, pageSize, total);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ejecutando consulta CONTAINS para query: '{query}'", ex);
            return new PagedList<ProductSuggestion>(new List<ProductSuggestion>(), pageIndex, pageSize);
        }
    }

    public async Task<IList<ProductSuggestion>> GetSuggestionsListAsync(string query)
    {
        var pagedResult = await GetSuggestionsAsync(query);
        return pagedResult.ToList();
    }

    private static string BuildContainsQuery(string query)
    {
        var escaped = query.Replace("\"", "\"\"").Replace("'", "''");
        return $"\"{escaped}*\"";
    }

    private static double NormalizeRank(int ftRank)
        => Math.Max(0, Math.Min(1, ftRank / 1000.0));
}