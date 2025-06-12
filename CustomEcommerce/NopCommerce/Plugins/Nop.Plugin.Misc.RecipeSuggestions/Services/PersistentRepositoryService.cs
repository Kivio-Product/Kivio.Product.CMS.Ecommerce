using Nop.Data;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Plugin.Misc.RecipeSuggestions.Models;
using Nop.Services.Media;

namespace Nop.Plugin.Misc.RecipeSuggestions.Services
{
    public class PersistentRepositoryService : IPersistentRepositoryService
    {
        private readonly IRepository<RecipeSuggestion> _aiRecipeSuggestionRepository;
        private readonly IRepository<RecipeIngredient> _aiRecipeIngredientRepository;
        private readonly IPictureService _pictureService;

        public PersistentRepositoryService(IRepository<RecipeSuggestion> aiRecipeSuggestionRepository,
                                      IRepository<RecipeIngredient> aiRecipeIngredientRepository,
                                      IPictureService pictureService)
        {
            _aiRecipeSuggestionRepository = aiRecipeSuggestionRepository;
            _aiRecipeIngredientRepository = aiRecipeIngredientRepository;
            _pictureService = pictureService;
        }

        public async Task<RecipeSuggestionViewModel> GetAsync(int productId)
        {
            var result = new RecipeSuggestionViewModel();
            
            var recipeSuggestion = await _aiRecipeSuggestionRepository.Table
                .FirstOrDefaultAsync(s => s.ProductId == productId);
            if (recipeSuggestion == null)
            {
                return null;
            }
            var ingredients = await _aiRecipeIngredientRepository.Table
                .Where(i => i.RecipeSuggestionId == recipeSuggestion.Id)
                .ToListAsync();
            
            result.RecipeTitle = recipeSuggestion?.RecipeTitle;
            result.RecipeDescription = recipeSuggestion?.Description;
            result.RecipeImageBase64 = recipeSuggestion?.ImageBase64;
            result.RecipeDate = recipeSuggestion?.CreatedOnUtc ?? DateTime.UtcNow;
            result.Ingredients = ingredients?.Select(i => new IngredientViewModel
            {
                Name = i.Name,
                IsNewIngredient = i.IsNewIngredient,
                ImageUrl = (i.IsNewIngredient) ? null :  _pictureService.GetPicturesByProductIdAsync((int) i.NopCommerceProductId, 1)
                    .ContinueWith(t => t.Result.Any() ? _pictureService.GetPictureUrlAsync(t.Result.First()).Result.Url : string.Empty).Result,
                NopCommerceProductId = i.NopCommerceProductId,
                NopCommerceProductSeName = i.NopCommerceProductSeName,
                Base64Image = i.Base64Image
            }).ToList() ?? new List<IngredientViewModel>();

            return result;
        }

        public Task RemoveAsync(int productId)
        {
            return _aiRecipeSuggestionRepository.DeleteAsync(s => s.ProductId == productId);
        }

        public Task SetAsync(int productId, RecipeSuggestionViewModel data, int cacheTimeInMinutes)
        {
            var recipeSuggestion = new RecipeSuggestion
            {
                ProductId = productId,
                RecipeTitle = data.RecipeTitle,
                Description = data.RecipeDescription,
                ImageBase64 = data.RecipeImageBase64,
                CreatedOnUtc = DateTime.UtcNow
            };
            return _aiRecipeSuggestionRepository.InsertAsync(recipeSuggestion)
                .ContinueWith(async t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        foreach (var ingredient in data.Ingredients)
                        {
                            var aiIngredient = new RecipeIngredient
                            {
                                RecipeSuggestionId = recipeSuggestion.Id,
                                Name = ingredient.Name,
                                IsNewIngredient = ingredient.IsNewIngredient,
                                NopCommerceProductId = ingredient.NopCommerceProductId,
                                NopCommerceProductSeName = ingredient.NopCommerceProductSeName,
                                Base64Image = ingredient.Base64Image
                            };
                            await _aiRecipeIngredientRepository.InsertAsync(aiIngredient);
                        }
                    }
                });
        }
    }
}