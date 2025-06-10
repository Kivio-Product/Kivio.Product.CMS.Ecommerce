using Nop.Core; 
using Nop.Core.Domain.Catalog; 
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces; 
using Nop.Plugin.Misc.RecipeSuggestions.Models;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Logging;
using Nop.Data;

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
        private readonly IRepository<RecipeSuggestion> _recipeSuggestionRepository;
        private readonly ILogger _logger;

        private string CACHE_KEY_PREFIX = "";
        private const int DEFAULT_CACHE_TIME_MINUTES = 72000;

        public RecipeSuggestionService(
            ICacheService cacheService,
            IAIRecipeService aiRecipeService,
            IProductService productService,
            IPictureService pictureService,
            IStoreContext storeContext,
            ISettingService settingService,
            IUrlRecordService urlRecordService,
            ILogger logger,
            IRepository<RecipeSuggestion> recipeSuggestionRepository)
        {
            _cacheService = cacheService;
            _aiRecipeService = aiRecipeService;
            _productService = productService;
            _pictureService = pictureService;
            _storeContext = storeContext;
            _settingService = settingService;
            _urlRecordService = urlRecordService;
            _recipeSuggestionRepository = recipeSuggestionRepository;
            _logger = logger;
            CACHE_KEY_PREFIX = _cacheService is PersistentCacheService ? "" : "recipesuggestion.product.";
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
            var cachedSuggestion = await _cacheService.GetAsync(cacheKey);

            if (cachedSuggestion != null)
            {
                return cachedSuggestion;
            }

            // If not in cache, generate and cache it.
            // The generation context here is "ondemand" as per the requirements.
            await GenerateAndCacheRecipeSuggestionAsync(productId, "ondemand");
            
            // Attempt to get it again from cache after generation.
            return await _cacheService.GetAsync(cacheKey);
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
                RecipeImageBase64 = await _aiRecipeService.GenerateImageForRecipeAsync(recipeTitle, 75),
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
                    ingredientVm.NopCommerceProductSeName = $"{await _urlRecordService.GetSeNameAsync(existingProduct)}"; // Assuming default URL structure
                    
                    var productPictures = await _pictureService.GetPicturesByProductIdAsync(existingProduct.Id, 1);
                    ingredientVm.ImageUrl = productPictures.Any() ? (await _pictureService.GetPictureUrlAsync(productPictures.First())).Url ?? string.Empty : string.Empty;

                }
                else
                {
                    ingredientVm.IsNewIngredient = true;
                    if (!string.IsNullOrWhiteSpace(aiIngredient.IngredientName))
                    {
                        ingredientVm.Base64Image = await _aiRecipeService.GenerateImageForIngredientAsync(aiIngredient.IngredientName, 60);
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

        public async Task GenerateRecipeSuggestionsForNewProductsAsync(int newProductsBatchSize)
        {
            _logger.Information($"Generating recipe suggestions for new products, batch size: {newProductsBatchSize}.");
            var productIdsOnRecipeSuggestion = await _recipeSuggestionRepository.Table
                .Select(rs => rs.ProductId)
                .ToListAsync();

            var newProducts = await _productService.SearchProductsAsync(
                visibleIndividuallyOnly: true,
                orderBy: ProductSortingEnum.CreatedOn,
                pageIndex: 0,
                pageSize: 100);

            var newProductsWithoutRecipe = newProducts.Where(p => !productIdsOnRecipeSuggestion.Contains(p.Id))
                                     .Take(newProductsBatchSize)
                                     .ToList();

            if (newProductsWithoutRecipe.Count == 0)
            {
                _logger.Information("No new products found for recipe suggestion generation.");
                return;
            }

            _logger.Information($"Found {newProductsWithoutRecipe.Count} new products without recipe suggestions. Generating suggestions...");
            foreach (var product in newProductsWithoutRecipe)
            {
                if (!ExistsRecipeOnCache(product.Id))
                {
                    _logger.Information($"Generating recipe suggestion for new product ID {product.Id} - {product.Name}.");
                    await GenerateAndCacheRecipeSuggestionAsync(product.Id, "batch");
                    _logger.Information($"Recipe suggestion for product ID {product.Id} - {product.Name} generated and cached successfully.");
                }
            }

        }

        public async Task RefreshRecipeSuggestionsAsync(int refreshProductsBatchSize, int refreshRecipeAgeDays)
        {
            _logger.Information($"Refreshing recipe suggestions for products older than {refreshRecipeAgeDays} days, batch size: {refreshProductsBatchSize}.");
            
            var cutoffDate = DateTime.UtcNow.AddDays(-refreshRecipeAgeDays);
            var productIdsToRefresh = _recipeSuggestionRepository.Table
                .Where(rs => rs.CreatedOnUtc < cutoffDate)
                .Select(rs => rs.ProductId)
                .Take(refreshProductsBatchSize)
                .ToList();
            
            foreach (var productId in productIdsToRefresh)
            {
                if (ExistsRecipeOnCache(productId))
                {
                    _logger.Information($"Refreshing recipe suggestion for product ID {productId}.");
                    await _cacheService.RemoveAsync($"{CACHE_KEY_PREFIX}{productId}");
                    await GenerateAndCacheRecipeSuggestionAsync(productId, "refresh");
                    _logger.Information($"Recipe suggestion for product ID {productId} refreshed successfully.");
                }
            }
        }

        public bool ExistsRecipeOnCache(int productId)
        {
            string cacheKey = $"{CACHE_KEY_PREFIX}{productId}";
            return _cacheService.GetAsync(cacheKey).Result != null;
        }
    }
}
