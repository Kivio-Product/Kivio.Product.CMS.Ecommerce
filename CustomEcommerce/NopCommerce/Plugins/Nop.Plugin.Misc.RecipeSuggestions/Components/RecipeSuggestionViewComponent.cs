using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Plugin.Misc.RecipeSuggestions.Models; 
using Nop.Web.Framework.Components;
using Nop.Web.Models.Catalog;

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
                return Content("");
            }

            return View("~/Plugins/Misc.RecipeSuggestions/Views/PublicView.cshtml", model);
        }
    }
}
