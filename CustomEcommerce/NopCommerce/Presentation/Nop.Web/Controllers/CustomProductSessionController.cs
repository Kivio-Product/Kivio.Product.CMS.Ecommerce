// Nop.Web/Controllers/CustomProductSectionController.cs
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using System.Linq;
using System.Threading.Tasks;
using Nop.Services.Configuration;

namespace Nop.Web.Controllers
{
    public class CustomProductSectionController : BasePublicController
    {
        private readonly IProductService _productService;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IProductTagService _productTagService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ISettingService _settingService;

        public CustomProductSectionController(
            IProductService productService,
            IProductModelFactory productModelFactory,
            IProductTagService productTagService,
            IStoreContext storeContext,
            IWorkContext workContext,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            ISettingService settingService)
        {
            _productService = productService;
            _productModelFactory = productModelFactory;
            _productTagService = productTagService;
            _storeContext = storeContext;
            _workContext = workContext;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _settingService = settingService;
        }

        public async Task<IActionResult> GetFilteredProducts(string productTagName, int productThumbPictureSize = 280)
        {
            if (string.IsNullOrEmpty(productTagName))
            {
                return PartialView("_CustomProductGridItems", new List<Nop.Web.Models.Catalog.ProductOverviewModel>());
            }

            int? currentProductTagId = null;

            if (!string.IsNullOrWhiteSpace(productTagName))
            {
                // Use GetAllProductTagsAsync and take the first result
                var tags = await _productTagService.GetAllProductTagsAsync(tagName: productTagName);
                var productTag = tags.FirstOrDefault();
                if (productTag != null)
                {
                    currentProductTagId = productTag.Id;
                }
            }
            
            if (currentProductTagId == null)
            {
                 return PartialView("_CustomProductGridItems", new List<Nop.Web.Models.Catalog.ProductOverviewModel>());
            }


            var products = await _productService.SearchProductsAsync(
                storeId: (await _storeContext.GetCurrentStoreAsync()).Id,
                productTagId: (int) currentProductTagId, 
                visibleIndividuallyOnly: true,
                overridePublished: false,
                orderBy: ProductSortingEnum.Position,
                pageSize: 6
            );

            var productModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(
                products,
                preparePriceModel: true,
                preparePictureModel: true,
                productThumbPictureSize: productThumbPictureSize,
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

            return PartialView("_CustomProductGridItems", productModels);
        }
    }
}