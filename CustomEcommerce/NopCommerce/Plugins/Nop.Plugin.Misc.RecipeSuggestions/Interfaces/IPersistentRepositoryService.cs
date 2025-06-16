using Nop.Plugin.Misc.RecipeSuggestions.Models;

namespace Nop.Plugin.Misc.RecipeSuggestions.Interfaces
{
    public interface IPersistentRepositoryService
    {
        Task<RecipeSuggestionViewModel> GetAsync(int productId);
        Task SetAsync(int productId, RecipeSuggestionViewModel data);
        Task RemoveAsync(int productId);
    }
}
