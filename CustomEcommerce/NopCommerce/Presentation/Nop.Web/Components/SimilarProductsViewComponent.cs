using Microsoft.AspNetCore.Mvc;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;
using Nop.Services.Configuration;

namespace Nop.Web.Components;

public partial class SimilarProductsViewComponent(IAclService aclService,
    IProductModelFactory productModelFactory,
    IProductService productService,
    ISettingService settingService,
    IStoreMappingService storeMappingService) : NopViewComponent
{
    protected readonly IAclService _aclService = aclService;
    protected readonly IProductModelFactory _productModelFactory = productModelFactory;
    protected readonly IProductService _productService = productService;
    protected readonly IStoreMappingService _storeMappingService = storeMappingService;
    protected readonly ISettingService _settingService = settingService;

    public async Task<IViewComponentResult> InvokeAsync(int productId)
    {

        var minSimilarProducts = await _settingService.GetSettingByKeyAsync<int>("SimilarProducts.Min", defaultValue: 8);
        var maxSimilarProducts = await _settingService.GetSettingByKeyAsync<int>("SimilarProducts.Max", defaultValue: 12);

        var products = await (await _productService.GetSameCategoryProductsByDiscountAsync(productId, maxSimilarProducts))
            .WhereAwait(async p => await _aclService.AuthorizeAsync(p) && await _storeMappingService.AuthorizeAsync(p))
            .Where(p => _productService.ProductIsAvailable(p))
            .Where(p => p.VisibleIndividually).ToListAsync();

        if (products.Count < minSimilarProducts)
        {
            var homepageProducts = await (await _productService.GetAllProductsDisplayedOnHomepageAsync())
                .WhereAwait(async p => await _aclService.AuthorizeAsync(p) && await _storeMappingService.AuthorizeAsync(p))
                .Where(p => _productService.ProductIsAvailable(p))
                .Where(p => p.VisibleIndividually)
                .Where(p => !products.Any(sp => sp.Id == p.Id))
                .Take(maxSimilarProducts - products.Count)
                .ToListAsync();

            products.AddRange(homepageProducts);
        }

        if (!products.Any())
            return Content(string.Empty);

        var model = (await _productModelFactory.PrepareProductOverviewModelsAsync(products, true, true)).ToList();
        return View(model);
    }
}