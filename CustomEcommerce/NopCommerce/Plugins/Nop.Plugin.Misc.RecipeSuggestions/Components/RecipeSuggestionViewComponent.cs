using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog; // Required for ProductDetailsModel check, if used
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces; // For IRecipeSuggestionService
using Nop.Plugin.Misc.RecipeSuggestions.Models; // For RecipeSuggestionViewModel
using Nop.Web.Framework.Components; // For NopViewComponent
using Nop.Web.Models.Catalog; // For ProductDetailsModel
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.RecipeSuggestions.Components
{
    public class RecipeSuggestionViewComponent : NopViewComponent
    {
        private readonly IRecipeSuggestionService _recipeSuggestionService;

        public RecipeSuggestionViewComponent(IRecipeSuggestionService recipeSuggestionService)
        {
            _recipeSuggestionService = recipeSuggestionService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
        {
            if (additionalData is not ProductDetailsModel productDetailsModel)
            {
                // Not on a product details page or model is not as expected, so don't render anything.
                // Or, log this situation if it's unexpected.
                return Content(""); 
            }

            int productId = productDetailsModel.Id;
            RecipeSuggestionViewModel? model = null;

            if (productId > 0)
            {
                model = await _recipeSuggestionService.GetRecipeSuggestionForProductAsync(productId);
            }

            if (model == null || model.Ingredients == null || !model.Ingredients.Any())
            {
                // No suggestion available or ingredients list is empty, don't render the component.
                return Content("");
            }

            // The view name will be "Default" by convention, located in:
            // /Plugins/Misc.RecipeSuggestions/Views/Shared/Components/RecipeSuggestion/Default.cshtml
            return View(model);
        }
    }
}
