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
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Caching;
using Nop.Core.Caching;
using Nop.Core.Domain.Stores;

namespace Nop.Web.Components
{
    public class SuperDealsViewComponent : NopViewComponent
    {
        private readonly IProductService _productService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IStoreContext _storeContext;
        private readonly ICategoryService _categoryService;
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly IStaticCacheManager _staticCacheManager;

        public SuperDealsViewComponent(
            IProductService productService,
            IProductModelFactory productModelFactory,
            IStoreContext storeContext,
            ICategoryService categoryService,
            IWorkContext workContext,
            ISettingService settingService,
            IStaticCacheManager staticCacheManager)
        {
            _productService = productService;
            _productModelFactory = productModelFactory;
            _storeContext = storeContext;
            _categoryService = categoryService;
            _workContext = workContext;
            _settingService = settingService;
            _staticCacheManager = staticCacheManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentStore = await _storeContext.GetCurrentStoreAsync();
            var customer = await _workContext.GetCurrentCustomerAsync();
            
            // Crear clave de caché única para el componente completo
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
                SuperDealsCacheDefaults.SuperDealsModelKey, 
                currentStore.Id, 
                await _workContext.GetWorkingLanguageAsync()
            );

            var model = await _staticCacheManager.GetAsync(cacheKey, async () =>
            {
                return await PrepareModelAsync(currentStore);
            });

            return View(model);
        }

        private async Task<SuperDealsModel> PrepareModelAsync(Store currentStore)
        {
            var model = new SuperDealsModel();
            
            var categoryNames = await _settingService.GetSettingByKeyAsync<string>("SuperDeals.CategoryNames");
            
            Console.WriteLine($"SuperDealsViewComponent: Category Names from settings: {categoryNames}");

            if (string.IsNullOrEmpty(categoryNames))
            {
                return model;
            }

            var categoryNamesList = categoryNames.Split(',')
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            var minimumDiscountPercentage = await _settingService.GetSettingByKeyAsync<decimal>("Catalog.MinimumDiscountPercentage", defaultValue: 0.2m);
            var maxProductsPerCategory = await _settingService.GetSettingByKeyAsync<int>("Catalog.Home.MaxProductsPerCategory", defaultValue: 20);

            foreach (var categoryName in categoryNamesList)
            {
                var categoryModel = await GetCachedCategoryProductsAsync(
                    categoryName, 
                    currentStore.Id, 
                    minimumDiscountPercentage, 
                    maxProductsPerCategory
                );

                if (categoryModel != null && categoryModel.Products.Any())
                {
                    model.CategoryProducts.Add(categoryModel);
                }
            }

            return model;
        }

        private async Task<CategoryProductsModel> GetCachedCategoryProductsAsync(
            string categoryName, 
            int storeId, 
            decimal minimumDiscountPercentage, 
            int maxProductsPerCategory)
        {
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
                SuperDealsCacheDefaults.CategoryProductsKey,
                categoryName,
                storeId,
                minimumDiscountPercentage,
                maxProductsPerCategory,
                await _workContext.GetWorkingLanguageAsync()
            );

            return await _staticCacheManager.GetAsync(cacheKey, async () =>
            {
                var categories = await _categoryService.GetAllCategoriesAsync(
                    categoryName: categoryName,
                    storeId: storeId,
                    showHidden: false
                );

                var category = categories.FirstOrDefault();
                if (category == null) return null;

                var products = await _productService.SearchProductsAsync(
                    categoryIds: new List<int> { category.Id },
                    storeId: storeId,
                    visibleIndividuallyOnly: true
                );

                var categoryProducts = products.ToList();
                if (!categoryProducts.Any()) return null;

                var productModels = await _productModelFactory.PrepareProductOverviewModelsAsync(
                    categoryProducts,
                    preparePriceModel: true,
                    preparePictureModel: true,
                    productThumbPictureSize: 280,
                    prepareSpecificationAttributes: false,
                    forceRedirectionAfterAddingToCart: false
                );

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
                    .Take(maxProductsPerCategory).ToList();

                return new CategoryProductsModel
                {
                    CategoryName = category.Name,
                    CategoryId = category.Id,
                    Products = filteredProducts
                };
            });
        }
    }

    public static class SuperDealsCacheDefaults
    {
        /// <summary>
        /// Clave para cachear el modelo completo de SuperDeals
        /// {0} : store ID
        /// {1} : language ID
        /// </summary>
        public static CacheKey SuperDealsModelKey => new("Nop.superdeals.model.{0}-{1}", SuperDealsPrefix);

        /// <summary>
        /// Clave para cachear productos por categoría
        /// {0} : category name
        /// {1} : store ID  
        /// {2} : minimum discount percentage
        /// {3} : max products per category
        /// {4} : language ID
        /// </summary>
        public static CacheKey CategoryProductsKey => new("Nop.superdeals.categoryproducts.{0}-{1}-{2}-{3}-{4}", SuperDealsPrefix);

        /// <summary>
        /// Prefijo para todas las claves de caché de SuperDeals
        /// </summary>
        public static string SuperDealsPrefix => "Nop.superdeals.";

        /// <summary>
        /// Tiempo de expiración del caché (en minutos)
        /// </summary>
        public static int CacheTime => 60; // 1 hora
    }

    public class SuperDealsModel
    {
        public SuperDealsModel()
        {
            CategoryProducts = new List<CategoryProductsModel>();
        }

        public List<CategoryProductsModel> CategoryProducts { get; set; }
    }

    public class CategoryProductsModel
    {
        public string CategoryName { get; set; }
        public int CategoryId { get; set; }
        public List<ProductOverviewModel> Products { get; set; }
    }
}