using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;
using Nop.Services.Configuration;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Components
{
    public class FilteredProductsViewComponent : NopViewComponent
    {
        private readonly IProductModelFactory _productModelFactory;
        private readonly IProductService _productService;
        private readonly IProductTagService _productTagService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        public FilteredProductsViewComponent(
            IProductModelFactory productModelFactory,
            IProductService productService,
            IProductTagService productTagService,
            IStoreContext storeContext,
            IWorkContext workContext,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            ILocalizationService localizationService,
            ISettingService settingService)
        {
            _productModelFactory = productModelFactory;
            _productService = productService;
            _productTagService = productTagService;
            _storeContext = storeContext;
            _workContext = workContext;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _settingService = settingService;
            _localizationService = localizationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await PrepareFilteredProductsModelAsync();
            return View(model);
        }

        private async Task<FilteredProductsModel> PrepareFilteredProductsModelAsync()
        {
            var productThumbPictureSize = 280;
            var defaultFilterTagName = "best-prices";

            var localeTagName = await _localizationService.GetResourceAsync("Home.FilteredProducts.DefaultTag");
            var localeBestPricesTag = await _localizationService.GetResourceAsync("Home.FilteredProducts.BestPricesTag");
            var localeMostOrderedTag = await _localizationService.GetResourceAsync("Home.FilteredProducts.MostOrderedTag");
            var localeBestRatedTag = await _localizationService.GetResourceAsync("Home.FilteredProducts.BestRatedTag");

            var localeBestPricesText = await _localizationService.GetResourceAsync("Home.FilteredProducts.BestPricesText");
            var localeMostOrderedText = await _localizationService.GetResourceAsync("Home.FilteredProducts.MostOrderedText");
            var localeBestRatedText = await _localizationService.GetResourceAsync("Home.FilteredProducts.BestRatedText");
            var sectionTitle = await _localizationService.GetResourceAsync("Home.FilteredProducts.SectionTitle");

            var initialFilterTagName = !string.IsNullOrWhiteSpace(localeTagName) && 
                                     localeTagName != "Home.FilteredProducts.DefaultTag" 
                                     ? localeTagName 
                                     : defaultFilterTagName;

            var initialProducts = await GetProductsByTagAsync(initialFilterTagName, defaultFilterTagName, productThumbPictureSize);

            var model = new FilteredProductsModel
            {
                SectionTitle = sectionTitle,
                ProductThumbPictureSize = productThumbPictureSize,
                FilterButtons = new List<FilterButtonModel>
                {
                    new FilterButtonModel 
                    { 
                        TagName = localeBestPricesTag, 
                        Text = localeBestPricesText, 
                        IsActive = true 
                    },
                    new FilterButtonModel 
                    { 
                        TagName = localeMostOrderedTag, 
                        Text = localeMostOrderedText, 
                        IsActive = false 
                    },
                    new FilterButtonModel 
                    { 
                        TagName = localeBestRatedTag, 
                        Text = localeBestRatedText, 
                        IsActive = false 
                    }
                },
                InitialProducts = initialProducts
            };

            return model;
        }

        private async Task<List<ProductOverviewModel>> GetProductsByTagAsync(
            string initialFilterTagName, 
            string defaultFilterTagName, 
            int productThumbPictureSize)
        {
            int? initialProductTagId = null;

            var tags = await _productTagService.GetAllProductTagsAsync(tagName: initialFilterTagName);
            var initialProductTag = tags.FirstOrDefault();

            if (initialProductTag == null && initialFilterTagName != defaultFilterTagName)
            {
                tags = await _productTagService.GetAllProductTagsAsync(tagName: defaultFilterTagName);
                initialProductTag = tags.FirstOrDefault();
            }

            if (initialProductTag != null)
            {
                initialProductTagId = initialProductTag.Id;
            }

            var initialProducts = await _productService.SearchProductsAsync(
                storeId: (await _storeContext.GetCurrentStoreAsync()).Id,
                productTagId: (int)initialProductTagId,
                visibleIndividuallyOnly: true,
                overridePublished: false,
                orderBy: ProductSortingEnum.Position,
                pageSize: 6
            );

            var initialProductModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(
                initialProducts,
                preparePriceModel: true,
                preparePictureModel: true,
                productThumbPictureSize: productThumbPictureSize,
                prepareSpecificationAttributes: false,
                forceRedirectionAfterAddingToCart: false
            )).ToList();

            var minimumDiscountPercentage = await _settingService.GetSettingByKeyAsync<decimal>("Catalog.MinimumDiscountPercentage", defaultValue: 0.2m);

            initialProductModels = initialProductModels
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

            return initialProductModels;
        }
    }

    public class FilteredProductsModel
    {
        public FilteredProductsModel()
        {
            FilterButtons = new List<FilterButtonModel>();
            InitialProducts = new List<ProductOverviewModel>();
        }

        public string SectionTitle { get; set; }
        public int ProductThumbPictureSize { get; set; }
        public List<FilterButtonModel> FilterButtons { get; set; }
        public List<ProductOverviewModel> InitialProducts { get; set; }
    }

    public class FilterButtonModel
    {
        public string TagName { get; set; }
        public string Text { get; set; }
        public bool IsActive { get; set; }
    }
}