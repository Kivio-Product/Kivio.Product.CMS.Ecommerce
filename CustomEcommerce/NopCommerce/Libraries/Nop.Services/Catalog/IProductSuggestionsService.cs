using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IProductSuggestionsService
{
    Task<IList<ProductSuggestion>> GetSuggestionsAsync(string query);
    Task ClearSuggestionsCache();
}