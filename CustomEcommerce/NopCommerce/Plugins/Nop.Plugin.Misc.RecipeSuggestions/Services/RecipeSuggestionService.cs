using Nop.Core; // For IStoreContext, possibly IWorkContext
using Nop.Core.Domain.Catalog; // For Product
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces; // For IRecipeSuggestionService, ICacheService, IGeminiRecipeService
using Nop.Plugin.Misc.RecipeSuggestions.Models;    // For RecipeSuggestionViewModel, IngredientViewModel, RecipeSuggestionSettings
using Nop.Services.Catalog; // For IProductService
using Nop.Services.Configuration; // For ISettingService
using Nop.Services.Localization; // For ILocalizationService (optional, for logging or messages)
using Nop.Services.Media; // For IPictureService (to get product image URLs)
using Nop.Services.Seo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.RecipeSuggestions.Services
{
    public class RecipeSuggestionService : IRecipeSuggestionService
    {
        private readonly ICacheService _cacheService;
        private readonly IGeminiRecipeService _geminiRecipeService;
        private readonly IProductService _productService;
        private readonly IPictureService _pictureService;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly IUrlRecordService _urlRecordService; 

        private const string CACHE_KEY_PREFIX = "recipesuggestion.product.";
        private const int DEFAULT_CACHE_TIME_MINUTES = 72000;  

        public RecipeSuggestionService(
            ICacheService cacheService,
            IGeminiRecipeService geminiRecipeService,
            IProductService productService,
            IPictureService pictureService,
            IStoreContext storeContext,
            ISettingService settingService,
            IUrlRecordService urlRecordService)
        {
            _cacheService = cacheService;
            _geminiRecipeService = geminiRecipeService;
            _productService = productService;
            _pictureService = pictureService;
            _storeContext = storeContext;
            _settingService = settingService;
            _urlRecordService = urlRecordService;
        }

        public async Task<RecipeSuggestionViewModel?> GetRecipeSuggestionForProductAsync(int productId)
        {
            // If settings not enabled
            var settings = await _settingService.LoadSettingAsync<RecipeSuggestionSettings>(_storeContext.GetCurrentStore().Id);
            if (settings == null || !settings.Enabled)
            {
                return null;
            }

            string cacheKey = $"{CACHE_KEY_PREFIX}{productId}";
            var cachedSuggestion = await _cacheService.GetAsync<RecipeSuggestionViewModel>(cacheKey);

            if (cachedSuggestion != null)
            {
                return cachedSuggestion;
            }

            // If not in cache, generate and cache it.
            // The generation context here is "ondemand" as per the requirements.
            await GenerateAndCacheRecipeSuggestionAsync(productId, "ondemand");
            
            // Attempt to get it again from cache after generation.
            return await _cacheService.GetAsync<RecipeSuggestionViewModel>(cacheKey);
        }

        public async Task GenerateAndCacheRecipeSuggestionAsync(int productId, string context)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || !product.Published || product.Deleted)
            {
                // Log or handle cases where product is not found or suitable.
                return;
            }

            // Load settings to get cache time, etc.
            var settings = await _settingService.LoadSettingAsync<RecipeSuggestionSettings>(_storeContext.GetCurrentStore().Id);
            int cacheTime = DEFAULT_CACHE_TIME_MINUTES;

            // Get available store products (potential ingredients)
            // This might need refinement based on performance and actual requirements (e.g., specific categories, stock status)
            var availableStoreProducts = await _productService.SearchProductsAsync(
                visibleIndividuallyOnly: true,
                orderBy: ProductSortingEnum.NameAsc);

            // Call GeminiRecipeService to get recipe
            var (recipeTitle, aiIngredients) = await _geminiRecipeService.GetRecipeFromAIAsync(product, availableStoreProducts.ToList());

            if (string.IsNullOrWhiteSpace(recipeTitle) || !aiIngredients.Any())
            {
                // Log or handle cases where AI returns no valid recipe.
                return;
            }
            
            var recipeSuggestion = new RecipeSuggestionViewModel
            {
                RecipeTitle = recipeTitle,
                // RecipeImageUrl = "placeholder_recipe_image.jpg" // Optionally, AI could suggest a general recipe image
            };

            foreach (var aiIngredient in aiIngredients)
            {
                var ingredientVm = new IngredientViewModel { Name = aiIngredient.Name };

                // Try to find this ingredient as an existing product in NopCommerce
                // This is a simple name match; more sophisticated logic might be needed.
                var existingProduct = availableStoreProducts.FirstOrDefault(p => p.Name.Equals(aiIngredient.Name, StringComparison.OrdinalIgnoreCase));

                if (existingProduct != null)
                {
                    ingredientVm.IsNewIngredient = false;
                    ingredientVm.NopCommerceProductId = existingProduct.Id;
                    ingredientVm.LinkToProductPage = $"/{await _urlRecordService.GetSeNameAsync(existingProduct)}"; // Assuming default URL structure
                    
                    var productPictures = await _pictureService.GetPicturesByProductIdAsync(existingProduct.Id, 1);
                    ingredientVm.ImageUrl = productPictures.Any() ? (await _pictureService.GetPictureUrlAsync(productPictures.First())).Url ?? string.Empty : string.Empty;

                }
                else
                {
                    ingredientVm.IsNewIngredient = true;
                    if (!string.IsNullOrWhiteSpace(aiIngredient.DescriptionForImage))
                    {
                        ingredientVm.ImageUrl = await _geminiRecipeService.GenerateImageForIngredientAsync(aiIngredient.DescriptionForImage);
                    }
                    else
                    {
                        ingredientVm.ImageUrl = await _pictureService.GetDefaultPictureUrlAsync();
                    }
                }
                recipeSuggestion.Ingredients.Add(ingredientVm);
            }
            
            string cacheKey = $"{CACHE_KEY_PREFIX}{productId}";
            await _cacheService.SetAsync(cacheKey, recipeSuggestion, cacheTime);
        }

        public Task GenerateRecipeSuggestionsForNewProductsAsync(int newProductsBatchSize)
        {
            throw new NotImplementedException();
        }

        public Task RefreshRecipeSuggestionsAsync(int refreshProductsBatchSize, int refreshRecipeAgeDays)
        {
            throw new NotImplementedException();
        }
    }
}
