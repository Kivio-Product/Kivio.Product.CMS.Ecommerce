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

        public PantryStaplesViewComponent(
            IProductService productService,
            IProductModelFactory productModelFactory,
            IStoreContext storeContext,
            ICategoryService categoryService,
            ISettingService settingService,
            IUrlRecordService urlRecordService)
        {
            _productService = productService;
            _productModelFactory = productModelFactory;
            _storeContext = storeContext;
            _categoryService = categoryService;
            _settingService = settingService;
            _urlRecordService = urlRecordService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categoryName = await _settingService.GetSettingByKeyAsync<string>("PantryStaples.CategoryName");

            if (string.IsNullOrEmpty(categoryName))
            {
                return Content("");
            }

            var categoryResult = await _categoryService.GetAllCategoriesAsync(
                categoryName: categoryName,
                showHidden: false,
                storeId: (await _storeContext.GetCurrentStoreAsync()).Id
            );

            var category = categoryResult.FirstOrDefault();
            if (category == null)
            {
                return Content("");
            }

            var products = await _productService.SearchProductsAsync(
                categoryIds: new List<int> { category.Id },
                storeId: (await _storeContext.GetCurrentStoreAsync()).Id,
                visibleIndividuallyOnly: true,
                overridePublished: false,
                pageSize: 15
            );

            var categoryProducts = products.Take(15).ToList();

            var productModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(
                categoryProducts,
                preparePriceModel: true,
                preparePictureModel: true,
                productThumbPictureSize: 280,
                prepareSpecificationAttributes: false,
                forceRedirectionAfterAddingToCart: false
            )).ToList();

            var minimumDiscountPercentage = await _settingService.GetSettingByKeyAsync<decimal>("Catalog.MinimumDiscountPercentage", defaultValue: 0.2m);

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

            var categorySeName = await _urlRecordService.GetSeNameAsync(category);

            var model = new PantryStaplesModel
            {
                Products = productModels,
                CategoryName = category.Name,
                CategorySeName = categorySeName,
                CategoryId = category.Id
            };

            return View(model);
        }
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