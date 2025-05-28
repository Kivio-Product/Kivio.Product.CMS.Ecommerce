using Nop.Core.Domain.Catalog; // For Product
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.RecipeSuggestions.Interfaces
{
    public interface IGeminiRecipeService
    {
        Task<(string RecipeTitle, List<(string Name, string DescriptionForImage)> Ingredients)> GetRecipeFromAIAsync(Product currentProduct, List<Product> availableStoreProducts);
        Task<string> GenerateImageForIngredientAsync(string ingredientDescription);
    }
}
