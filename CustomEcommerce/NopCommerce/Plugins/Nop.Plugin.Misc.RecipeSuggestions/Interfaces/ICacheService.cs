using Nop.Plugin.Misc.RecipeSuggestions.Models;

namespace Nop.Plugin.Misc.RecipeSuggestions.Interfaces
{
    public interface ICacheService
    {
        Task<RecipeSuggestionViewModel> GetAsync(string productId);
        Task SetAsync(string cacheKey, RecipeSuggestionViewModel data, int cacheTimeInMinutes);
        Task RemoveAsync(string cacheKey);
    }
}
