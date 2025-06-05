using Nop.Data;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Plugin.Misc.RecipeSuggestions.Models;
using static LinqToDB.Reflection.Methods.LinqToDB.Insert;

namespace Nop.Plugin.Misc.RecipeSuggestions.Services
{
    public class PersistentCacheService : ICacheService
    {
        private readonly IRepository<RecipeSuggestion> _aiRecipeSuggestionRepository;
        private readonly IRepository<RecipeIngredient> _aiRecipeIngredientRepository;

        public PersistentCacheService(IRepository<RecipeSuggestion> aiRecipeSuggestionRepository,
                                      IRepository<RecipeIngredient> aiRecipeIngredientRepository)
        {
            _aiRecipeSuggestionRepository = aiRecipeSuggestionRepository;
            _aiRecipeIngredientRepository = aiRecipeIngredientRepository;
        }

        public async Task<RecipeSuggestionViewModel> GetAsync(string productId)
        {
            var result = new RecipeSuggestionViewModel();
            
            var recipeSuggestion = await _aiRecipeSuggestionRepository.Table
                .FirstOrDefaultAsync(s => s.ProductId.ToString() == productId);
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
                ImageUrl = i.ImageUrl,
                IsNewIngredient = i.IsNewIngredient,
                NopCommerceProductId = i.NopCommerceProductId,
                NopCommerceProductSeName = i.NopCommerceProductSeName,
                Base64Image = i.Base64Image
            }).ToList() ?? new List<IngredientViewModel>();

            return result;
        }

        public Task RemoveAsync(string productId)
        {
            return _aiRecipeSuggestionRepository.DeleteAsync(s => s.ProductId.ToString() == productId);
        }

        public Task SetAsync(string productId, RecipeSuggestionViewModel data, int cacheTimeInMinutes)
        {
            var recipeSuggestion = new RecipeSuggestion
            {
                ProductId = int.Parse(productId),
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
                                ImageUrl = ingredient.ImageUrl,
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