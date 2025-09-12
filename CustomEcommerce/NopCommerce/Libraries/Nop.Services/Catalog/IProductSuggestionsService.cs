using Nop.Core.Domain.Catalog;
using Nop.Core;
namespace Nop.Services.Catalog;

public interface IProductSuggestionsService
{
    public Task<IPagedList<ProductSuggestion>> GetSuggestionsAsync(string query, int pageIndex = 0, int pageSize = 0);
    public Task<IList<ProductSuggestion>> GetSuggestionsListAsync(string query);
}