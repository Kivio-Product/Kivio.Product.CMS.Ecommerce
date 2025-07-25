using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;
using Nop.Web.Models.Catalog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Seo;
using Nop.Services.Caching;
using Nop.Core.Caching;
using Nop.Core.Domain.Stores;

namespace Nop.Web.Components
{
    public class PantryStaplesViewComponent : NopViewComponent
    {
        private readonly IProductService _productService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IStoreContext _storeContext;
        private readonly ICategoryService _categoryService;
        private readonly ISettingService _settingService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IWorkContext _workContext;

        public PantryStaplesViewComponent(
            IProductService productService,
            IProductModelFactory productModelFactory,
            IStoreContext storeContext,
            ICategoryService categoryService,
            ISettingService settingService,
            IUrlRecordService urlRecordService,
            IStaticCacheManager staticCacheManager,
            IWorkContext workContext)
        {
            _productService = productService;
            _productModelFactory = productModelFactory;
            _storeContext = storeContext;
            _categoryService = categoryService;
            _settingService = settingService;
            _urlRecordService = urlRecordService;
            _staticCacheManager = staticCacheManager;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentStore = await _storeContext.GetCurrentStoreAsync();
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();
            
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
                PantryStaplesCacheDefaults.PantryStaplesModelKey, 
                currentStore.Id, 
                workingLanguage.Id
            );

            var model = await _staticCacheManager.GetAsync(cacheKey, async () =>
            {
                return await PrepareModelAsync(currentStore);
            });

            if (model == null || !model.Products.Any())
            {
                return Content("");
            }

            return View(model);
        }

        private async Task<PantryStaplesModel> PrepareModelAsync(Store currentStore)
        {
            var categoryName = await _settingService.GetSettingByKeyAsync<string>("PantryStaples.CategoryName");
            var maxProductsPerCategory = await _settingService.GetSettingByKeyAsync<int>("Catalog.Home.MaxProductsPerCategory", defaultValue: 20);

            if (string.IsNullOrEmpty(categoryName))
            {
                return null;
            }

            var category = await GetCachedCategoryAsync(categoryName, currentStore.Id);
            if (category == null)
            {
                return null;
            }

            var productModels = await GetCachedCategoryProductsAsync(
                category.Id, 
                currentStore.Id, 
                maxProductsPerCategory
            );

            if (!productModels.Any())
            {
                return null;
            }

            var categorySeName = await _urlRecordService.GetSeNameAsync(category);

            return new PantryStaplesModel
            {
                Products = productModels,
                CategoryName = category.Name,
                CategorySeName = categorySeName,
                CategoryId = category.Id
            };
        }

        private async Task<Category> GetCachedCategoryAsync(string categoryName, int storeId)
        {
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
                PantryStaplesCacheDefaults.CategoryByNameKey,
                categoryName,
                storeId
            );

            return await _staticCacheManager.GetAsync(cacheKey, async () =>
            {
                var categoryResult = await _categoryService.GetAllCategoriesAsync(
                    categoryName: categoryName,
                    showHidden: false,
                    storeId: storeId
                );

                return categoryResult.FirstOrDefault();
            });
        }

        private async Task<IList<ProductOverviewModel>> GetCachedCategoryProductsAsync(
            int categoryId, 
            int storeId, 
            int maxProductsPerCategory)
        {
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();
            var minimumDiscountPercentage = await _settingService.GetSettingByKeyAsync<decimal>("Catalog.MinimumDiscountPercentage", defaultValue: 0.2m);
            
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
                PantryStaplesCacheDefaults.CategoryProductsKey,
                categoryId,
                storeId,
                maxProductsPerCategory,
                minimumDiscountPercentage,
                workingLanguage.Id
            );

            return await _staticCacheManager.GetAsync(cacheKey, async () =>
            {
                var products = await _productService.SearchProductsAsync(
                    categoryIds: new List<int> { categoryId },
                    storeId: storeId,
                    visibleIndividuallyOnly: true,
                    pageSize: maxProductsPerCategory
                );

                var categoryProducts = products.ToList();

                if (!categoryProducts.Any())
                {
                    return new List<ProductOverviewModel>();
                }

                var productModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(
                    categoryProducts,
                    preparePriceModel: true,
                    preparePictureModel: true,
                    productThumbPictureSize: 280,
                    prepareSpecificationAttributes: false,
                    forceRedirectionAfterAddingToCart: false
                )).ToList();

                var filteredProducts = productModels
                    .Where(p => p.ProductPrice.OldPriceValue.HasValue &&
                                p.ProductPrice.PriceValue.HasValue &&
                                p.ProductPrice.OldPriceValue.Value > 0)
                    .Where(p =>
                    {
                        var oldPrice = p.ProductPrice.OldPriceValue.Value;
                        var newPrice = p.ProductPrice.PriceValue.Value;
                        var discount = (oldPrice - newPrice) / oldPrice;
                        return discount >= minimumDiscountPercentage;
                    })
                    .ToList();

                return filteredProducts;
            });
        }
    }

    public static class PantryStaplesCacheDefaults
    {
        /// <summary>
        /// Clave para cachear el modelo completo de PantryStaples
        /// {0} : store ID
        /// {1} : language ID
        /// </summary>
        public static CacheKey PantryStaplesModelKey => new("Nop.pantrystaples.model.{0}-{1}", PantryStaplesPrefix);

        /// <summary>
        /// Clave para cachear categoría por nombre
        /// {0} : category name
        /// {1} : store ID
        /// </summary>
        public static CacheKey CategoryByNameKey => new("Nop.pantrystaples.category.name.{0}-{1}", PantryStaplesPrefix);

        /// <summary>
        /// Clave para cachear productos de categoría
        /// {0} : category ID
        /// {1} : store ID  
        /// {2} : max products per category
        /// {3} : minimum discount percentage
        /// {4} : language ID
        /// </summary>
        public static CacheKey CategoryProductsKey => new("Nop.pantrystaples.products.{0}-{1}-{2}-{3}-{4}", PantryStaplesPrefix);

        /// <summary>
        /// Prefijo para todas las claves de caché de PantryStaples
        /// </summary>
        public static string PantryStaplesPrefix => "Nop.pantrystaples.";

        /// <summary>
        /// Tiempo de expiración del caché (en minutos)
        /// </summary>
        public static int CacheTime => 60; // 1 hora
    }

    public class PantryStaplesModel
    {
        public PantryStaplesModel()
        {
            Products = new List<ProductOverviewModel>();
        }

        public IList<ProductOverviewModel> Products { get; set; }
        public string CategoryName { get; set; }
        public string CategorySeName { get; set; }
        public int CategoryId { get; set; }
    }
}