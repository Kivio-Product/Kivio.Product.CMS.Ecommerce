using Nop.Plugin.Misc.RecipeSuggestions.Models; // For RecipeSuggestionViewModel
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.RecipeSuggestions.Interfaces
{
    public interface IRecipeSuggestionService
    {
        Task<RecipeSuggestionViewModel> GetRecipeSuggestionForProductAsync(int productId);
        Task GenerateAndCacheRecipeSuggestionAsync(int productId, string context);
        Task GenerateRecipeSuggestionsForNewProductsAsync(int newProductsBatchSize);
        Task RefreshRecipeSuggestionsAsync(int refreshProductsBatchSize, int refreshRecipeAgeDays);
    }
}
