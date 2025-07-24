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

        public SuperDealsViewComponent(
            IProductService productService,
            IProductModelFactory productModelFactory,
            IStoreContext storeContext,
            ICategoryService categoryService,
            IWorkContext workContext,
            ISettingService settingService)
        {
            _productService = productService;
            _productModelFactory = productModelFactory;
            _storeContext = storeContext;
            _categoryService = categoryService;
            _workContext = workContext;
            _settingService = settingService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new SuperDealsModel();
            var currentStore = await _storeContext.GetCurrentStoreAsync();

            var categoryNames =  await _settingService.GetSettingByKeyAsync<string>("SuperDeals.CategoryNames");

            Console.WriteLine($"SuperDealsViewComponent: Category Names from settings: {categoryNames}");

            if (string.IsNullOrEmpty(categoryNames))
            {
                return Content("");
            }

            var categoryNamesList = categoryNames.Split(',')
                .Select(name => name.Trim())
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            var minimumDiscountPercentage = await _settingService.GetSettingByKeyAsync<decimal>("Catalog.MinimumDiscountPercentage", defaultValue: 0.2m);

            foreach (var categoryName in categoryNamesList)
            {
                var categories = await _categoryService.GetAllCategoriesAsync(
                    categoryName: categoryName,
                    storeId: currentStore.Id,
                    showHidden: false
                );

                var category = categories.FirstOrDefault();

                if (category == null) continue;

                var products = await _productService.SearchProductsAsync(
                    categoryIds: new List<int> { category.Id },
                    storeId: currentStore.Id,
                    visibleIndividuallyOnly: true,
                    overridePublished: true,
                    pageSize: 40,
                    orderBy: ProductSortingEnum.PriceDesc,
                    pageIndex: 0
                );

                var categoryProducts = products.Take(15).ToList();

                if (!categoryProducts.Any()) continue;

                var productModels = await _productModelFactory.PrepareProductOverviewModelsAsync(
                    categoryProducts,
                    preparePriceModel: true,
                    preparePictureModel: true,
                    productThumbPictureSize: 280,
                    prepareSpecificationAttributes: false,
                    forceRedirectionAfterAddingToCart: false
                );

                productModels = productModels
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

                model.CategoryProducts.Add(new CategoryProductsModel
                {
                    CategoryName = category.Name,
                    CategoryId = category.Id,
                    Products = productModels.ToList()
                });
            }

            return View(model);
        }
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