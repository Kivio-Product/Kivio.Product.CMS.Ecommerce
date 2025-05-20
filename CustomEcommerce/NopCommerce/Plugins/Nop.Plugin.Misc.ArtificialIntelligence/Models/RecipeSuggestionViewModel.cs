using Nop.Web.Framework.Models;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.ArtificialIntelligence.Models
{
    public record RecipeSuggestionViewModel : BaseNopModel
    {
        public string CurrentProductName { get; set; }
        public List<SuggestedIngredientViewModel> Ingredients { get; set; }
        public string RecipeName { get; set; }
        public string FullRecipeLink { get; set; } // Link to Google search or similar

        public RecipeSuggestionViewModel()
        {
            Ingredients = new List<SuggestedIngredientViewModel>();
        }
    }
}
