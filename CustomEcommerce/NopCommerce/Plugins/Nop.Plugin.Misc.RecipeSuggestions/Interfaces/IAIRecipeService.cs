using Nop.Core.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.RecipeSuggestions.Interfaces
{
    public interface IAIRecipeService
    {
        Task<(string RecipeTitle, List<(string ProductID, string IngredientName)> Ingredients, string Instructions)> GetRecipeFromAIAsync(Product currentProduct, List<Product> availableStoreProducts);
        Task<string> GenerateImageForIngredientAsync(string ingredientDescription);
    }
}
