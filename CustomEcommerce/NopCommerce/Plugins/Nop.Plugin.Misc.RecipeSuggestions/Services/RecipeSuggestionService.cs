using Nop.Core; 
using Nop.Core.Domain.Catalog; 
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces; 
using Nop.Plugin.Misc.RecipeSuggestions.Models;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization; 
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.RecipeSuggestions.Services
{
    public class RecipeSuggestionService : IRecipeSuggestionService
    {
        private readonly ICacheService _cacheService;
        private readonly IAIRecipeService _aiRecipeService;
        private readonly IProductService _productService;
        private readonly IPictureService _pictureService;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly IUrlRecordService _urlRecordService; 
        private readonly ILogger _logger;

        private const string CACHE_KEY_PREFIX = "recipesuggestion.product.";
        private const int DEFAULT_CACHE_TIME_MINUTES = 72000;

        public RecipeSuggestionService(
            ICacheService cacheService,
            IAIRecipeService aiRecipeService,
            IProductService productService,
            IPictureService pictureService,
            IStoreContext storeContext,
            ISettingService settingService,
            IUrlRecordService urlRecordService,
            ILogger logger)
        {
            _cacheService = cacheService;
            _aiRecipeService = aiRecipeService;
            _productService = productService;
            _pictureService = pictureService;
            _storeContext = storeContext;
            _settingService = settingService;
            _urlRecordService = urlRecordService;
            _logger = logger;
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
            _logger.Information($"Generating recipe suggestion for product ID {productId} in context '{context}'.");
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || !product.Published || product.Deleted)
            {
                //Logger
                _logger.Warning($"Product with ID {productId} is not valid for recipe suggestion generation.");
                return;
            }

            // Load settings to get cache time, etc.
            var settings = await _settingService.LoadSettingAsync<RecipeSuggestionSettings>(_storeContext.GetCurrentStore().Id);
            int cacheTime = DEFAULT_CACHE_TIME_MINUTES;

            var availableStoreProducts = await _productService.SearchProductsAsync(
                visibleIndividuallyOnly: true,
                orderBy: ProductSortingEnum.NameAsc);

            // Call AIRecipeService to get recipe
            var (recipeTitle, aiIngredients, instructions) = await _aiRecipeService.GetRecipeFromAIAsync(product, availableStoreProducts.ToList());

            if (string.IsNullOrWhiteSpace(recipeTitle) || !aiIngredients.Any())
            {
                _logger.Warning($"AI did not return a valid recipe for product ID {productId}.");
                return;
            }
            
            var recipeSuggestion = new RecipeSuggestionViewModel
            {
                RecipeTitle = recipeTitle,
                RecipeImageUrl = "placeholder_recipe_image.jpg", // TODO: AI could suggest a general recipe image
                RecipeDescription = instructions,
                RecipeDate = DateTime.UtcNow
            };

            foreach (var aiIngredient in aiIngredients)
            {
                var ingredientVm = new IngredientViewModel { Name = aiIngredient.IngredientName };

                var existingProduct = availableStoreProducts.FirstOrDefault(p => p.Id == int.Parse(aiIngredient.ProductID) ||
                                                                                p.Name.Equals(aiIngredient.IngredientName, StringComparison.OrdinalIgnoreCase));

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
                    if (!string.IsNullOrWhiteSpace(aiIngredient.IngredientName))
                    {
                        ingredientVm.ImageUrl = await _aiRecipeService.GenerateImageForIngredientAsync(aiIngredient.IngredientName);
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
