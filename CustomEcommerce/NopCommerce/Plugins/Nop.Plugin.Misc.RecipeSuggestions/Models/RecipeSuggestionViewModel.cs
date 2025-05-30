using System.Collections.Generic;

namespace Nop.Plugin.Misc.RecipeSuggestions.Models
{
    public class RecipeSuggestionViewModel
    {
        public string RecipeTitle { get; set; }
        public string RecipeDescription { get; set; }
        public string? RecipeImageUrl { get; set; } // Optional
        public DateTime RecipeDate { get; set; }
        public List<IngredientViewModel> Ingredients { get; set; }

        public RecipeSuggestionViewModel()
        {
            Ingredients = new List<IngredientViewModel>();
        }
    }
}
