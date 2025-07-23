using Microsoft.AspNetCore.Mvc;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Services.Configuration;
using Nop.Web.Framework.Components;

namespace Nop.Web.Components;

public partial class HomepageProductsViewComponent : NopViewComponent
{
    protected readonly IAclService _aclService;
    protected readonly IProductModelFactory _productModelFactory;
    protected readonly IProductService _productService;
    private readonly ISettingService _settingService;
    protected readonly IStoreMappingService _storeMappingService;

    public HomepageProductsViewComponent(IAclService aclService,
        IProductModelFactory productModelFactory,
        IProductService productService,
        IStoreMappingService storeMappingService,
        ISettingService settingService)
    {
        _aclService = aclService;
        _productModelFactory = productModelFactory;
        _productService = productService;
        _settingService = settingService;
        _storeMappingService = storeMappingService;
    }

    public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize)
    {
        var products = await (await _productService.GetAllProductsDisplayedOnHomepageAsync())
            //ACL and store mapping
            .WhereAwait(async p => await _aclService.AuthorizeAsync(p) && await _storeMappingService.AuthorizeAsync(p))
            //availability dates
            .Where(p => _productService.ProductIsAvailable(p))
            //visible individually
            .Where(p => p.VisibleIndividually).ToListAsync();

        if (!products.Any())
            return Content("");

        var productModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(products, true, true, productThumbPictureSize)).ToList();

        var minimumDiscountPercentage = await _settingService.GetSettingByKeyAsync<decimal>("Catalog.MinimumDiscountPercentage", defaultValue: 0.2m);

        var model = productModels = productModels
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

        return View(model);
    }
}