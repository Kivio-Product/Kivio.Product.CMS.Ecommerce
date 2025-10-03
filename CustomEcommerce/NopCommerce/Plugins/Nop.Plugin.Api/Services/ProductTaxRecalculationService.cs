using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Tax;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Tax;

namespace Nop.Plugin.Api.Services;

/// <summary>
/// Service for recalculating product prices with tax
/// </summary>
public class ProductTaxRecalculationService : IProductTaxRecalculationService
{
    private const string PRICE_FROM_SCRAPE_KEY = "priceFromScrape";

    private readonly IProductService _productService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly ITaxPluginManager _taxPluginManager;
    private readonly TaxSettings _taxSettings;
    private string SCRAPE_CATEGORY_SKU_IDENTIFIER = "SCRAPED";

    public ProductTaxRecalculationService(
        IProductService productService,
        IGenericAttributeService genericAttributeService,
        ITaxPluginManager taxPluginManager,
        TaxSettings taxSettings)
    {
        _productService = productService;
        _genericAttributeService = genericAttributeService;
        _taxPluginManager = taxPluginManager;
        _taxSettings = taxSettings;
    }

    /// <summary>
    /// Processes tax recalculation for product creation or update
    /// </summary>
    /// <param name="product">Product entity from request</param>
    /// <param name="isInsert">True if this is an insert operation, false for update</param>
    public async Task ProcessProductTaxRecalculationAsync(Product product, bool isInsert)
    {
        if (isInsert)
        {
            if (string.IsNullOrEmpty(product.Sku))
            {
                return;
            }

            var existingProduct = await _productService.GetProductBySkuAsync(product.Sku);

            if (existingProduct == null || !IsScrapeCategory(existingProduct))
            {
                return;
            }

            await ProcessTaxRecalculationForExistingProductAsync(product, existingProduct);
        }
        else
        {
            if (product.Id <= 0 || IsScrapeCategory(product))
            {
                return;
            }

            await ProcessTaxRecalculationForExistingProductAsync(product, product);
        }
    }

    /// <summary>
    /// Process tax recalculation for an existing product
    /// </summary>
    private async Task ProcessTaxRecalculationForExistingProductAsync(Product product, Product existingProduct)
    {
        decimal? priceFromScrape = null;

        // Step 1: Validate if price comes in the request
        if (product.Price > 0)
        {
            // Save the price in generic attribute
            priceFromScrape = product.Price;
            await _genericAttributeService.SaveAttributeAsync(existingProduct, PRICE_FROM_SCRAPE_KEY, priceFromScrape.Value);
        }
        else
        {
            // If price doesn't come, try to get it from generic attributes
            priceFromScrape = await _genericAttributeService.GetAttributeAsync<decimal?>(existingProduct, PRICE_FROM_SCRAPE_KEY);

            // If generic attribute doesn't exist, do nothing
            if (!priceFromScrape.HasValue || priceFromScrape.Value == 0)
            {
                return;
            }
        }

        // Step 2: Determine the tax category
        int taxCategoryOfProduct;

        if (product.TaxCategoryId > 0)
        {
            // Use the tax category from the request
            taxCategoryOfProduct = product.TaxCategoryId;
        }
        else
        {
            // Get it from the existing product
            taxCategoryOfProduct = existingProduct.TaxCategoryId;
        }

        // Step 3: Recalculate price and old_price with tax
        await RecalculatePricesWithTaxAsync(product, existingProduct, priceFromScrape.Value, taxCategoryOfProduct);
    }

    /// <summary>
    /// Recalculates prices with tax based on the price from scrape
    /// </summary>
    private async Task RecalculatePricesWithTaxAsync(Product product, Product existingProduct, decimal priceFromScrape, int taxCategoryId)
    {
        var taxRate = await GetTaxRateAsync(existingProduct, taxCategoryId);

        var priceWithoutTax = priceFromScrape / (1 + taxRate);

        product.Price = priceWithoutTax;

        // If old_price is present in the request, also calculate it without tax
        if (product.OldPrice > 0)
        {
            var oldPriceWithoutTax = product.OldPrice / (1 + taxRate);
            product.OldPrice = oldPriceWithoutTax;
        }
    }

    /// <summary>
    /// Gets the tax rate for a specific tax category
    /// </summary>
    private async Task<decimal> GetTaxRateAsync(Product product, int taxCategoryId)
    {
        // Get the active tax provider
        var taxProvider = await _taxPluginManager.LoadPrimaryPluginAsync(customer: null, storeId: 0);
        if (taxProvider == null)
        {
            return 0; // No tax if no provider is configured
        }

        // Create tax rate request
        var taxRateRequest = new TaxRateRequest
        {
            TaxCategoryId = taxCategoryId,
            Product = product,
            Address = new Core.Domain.Common.Address
            {
                CountryId = 49,
                StateProvinceId = 11,
            },
            CurrentStoreId = 0
        };

        try
        {
            var taxRateResult = await taxProvider.GetTaxRateAsync(taxRateRequest);
            return taxRateResult.TaxRate / 100; // Convert percentage to decimal (19% -> 0.19)
        }
        catch
        {
            return 0; // Return 0 if there's any error
        }
    }

    /// <summary>
    /// Determines if the product belongs to the "scrape" category
    /// </summary>
    /// <param name="product">Product to check</param>
    /// <returns>True if product is in scrape category, false otherwise</returns>
    private bool IsScrapeCategory(Product product)
    {
        return product.Sku != null && product.Sku.ToUpper().Contains(SCRAPE_CATEGORY_SKU_IDENTIFIER);
    }
}
