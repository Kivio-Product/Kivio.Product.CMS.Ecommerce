using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Stores;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Components;

public partial class HomepageProductsViewComponent : NopViewComponent
{
    protected readonly IAclService _aclService;
    protected readonly IProductModelFactory _productModelFactory;
    protected readonly IProductService _productService;
    private readonly ISettingService _settingService;
    protected readonly IStoreMappingService _storeMappingService;
    protected readonly IStaticCacheManager _staticCacheManager;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    public HomepageProductsViewComponent(IAclService aclService,
        IProductModelFactory productModelFactory,
        IProductService productService,
        IStoreMappingService storeMappingService,
        ISettingService settingService,
        IStaticCacheManager staticCacheManager,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _aclService = aclService;
        _productModelFactory = productModelFactory;
        _productService = productService;
        _settingService = settingService;
        _storeMappingService = storeMappingService;
        _staticCacheManager = staticCacheManager;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize)
    {
        var currentStore = await _storeContext.GetCurrentStoreAsync();
        var customer = await _workContext.GetCurrentCustomerAsync();

        // Crear clave de caché única para el componente completo
        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
            HomepageProductsCacheDefaults.HomepageProductsModelKey,
            currentStore.Id,
            await _workContext.GetWorkingLanguageAsync()
        );

        var model = await _staticCacheManager.GetAsync(cacheKey, async () =>
        {
            return await PrepareModelAsync(productThumbPictureSize);
        });

        if (model == null || !model.Any())
        {
            return Content("");
        }

        return View(model);
    }

    private async Task<IEnumerable<ProductOverviewModel>> PrepareModelAsync(int? productThumbPictureSize)
    {
        var products = await (await _productService.GetAllProductsDisplayedOnHomepageAsync())
        //ACL and store mapping
        .WhereAwait(async p => await _aclService.AuthorizeAsync(p) && await _storeMappingService.AuthorizeAsync(p))
        //availability dates
        .Where(p => _productService.ProductIsAvailable(p))
        //visible individually
        .Where(p => p.VisibleIndividually).ToListAsync();

        var model = new List<ProductOverviewModel>();

        if (!products.Any())
            return model;

        var productModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(products, true, true, productThumbPictureSize)).ToList();

        var maxProductsPerCategory = await _settingService.GetSettingByKeyAsync<int>("Catalog.MaxProductsPerCategory", defaultValue: 20);

        model = productModels
                .Where(p => p.HasHighDiscount)
                .Take(maxProductsPerCategory)
                .ToList();

        return model;
    }

    public static class HomepageProductsCacheDefaults
    {
        /// <summary>
        /// Key to cache the complete model of HomepageProducts
        /// {0} : store ID
        /// </summary>
        public static CacheKey HomepageProductsModelKey => new("Nop.homepageproducts.model.{0}-{1}", HomePageProductPrefix);

        public static string HomePageProductPrefix = "Nop.homepageproducts.model";
    }
}