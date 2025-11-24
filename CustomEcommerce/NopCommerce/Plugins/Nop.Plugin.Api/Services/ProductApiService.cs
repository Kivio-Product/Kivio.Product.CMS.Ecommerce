using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Vendors;
using Nop.Data;
using Nop.Plugin.Api.DataStructures;
using Nop.Plugin.Api.Infrastructure;
using Nop.Services.Stores;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Tax;


namespace Nop.Plugin.Api.Services
{
    public class ProductApiService : IProductApiService
    {
        private readonly IRepository<ProductCategory> _productCategoryMappingRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly ICategoryService _categoryService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ILocalizationService _localizationService;
        private readonly ITaxCategoryService _taxCategoryService;

        public ProductApiService(
            IRepository<Product> productRepository,
            IRepository<ProductCategory> productCategoryMappingRepository,
            IRepository<Vendor> vendorRepository,
            IStoreMappingService storeMappingService,
            IProductService productService,
            ISettingService settingService,
            ICategoryService categoryService,
            ICustomerActivityService customerActivityService,
            ILocalizationService localizationService,
            ITaxCategoryService taxCategoryService)
        {
            _productRepository = productRepository;
            _productCategoryMappingRepository = productCategoryMappingRepository;
            _vendorRepository = vendorRepository;
            _storeMappingService = storeMappingService;
            _productService = productService;
            _settingService = settingService;
            _categoryService = categoryService;
            _customerActivityService = customerActivityService;
            _localizationService = localizationService;
            _taxCategoryService = taxCategoryService;
        }

        public IList<Product> GetProducts(
            IList<int> ids = null,
            DateTime? createdAtMin = null, DateTime? createdAtMax = null, DateTime? updatedAtMin = null, DateTime? updatedAtMax = null,
            int? limit = null, int? page = null,
            int? sinceId = null,
            int? categoryId = null, string vendorName = null, bool? publishedStatus = null, IList<string> manufacturerPartNumbers = null, bool? isDownload = null, string name = null)
        {
            var query = GetProductsQuery(createdAtMin, createdAtMax, updatedAtMin, updatedAtMax, vendorName, publishedStatus, ids, categoryId, manufacturerPartNumbers, isDownload, name);

            if (sinceId > 0)
            {
                query = query.Where(c => c.Id > sinceId);
            }

            return new ApiList<Product>(query, (page ?? Constants.Configurations.DefaultPageValue) - 1, (limit ?? Constants.Configurations.DefaultLimit));
        }

        public async Task<int> GetProductsCountAsync(
            DateTime? createdAtMin = null, DateTime? createdAtMax = null,
            DateTime? updatedAtMin = null, DateTime? updatedAtMax = null, bool? publishedStatus = null, string vendorName = null,
            int? categoryId = null, IList<string> manufacturerPartNumbers = null, bool? isDownload = null, string name = null)
        {
            var query = GetProductsQuery(createdAtMin, createdAtMax, updatedAtMin, updatedAtMax, vendorName,
                                         publishedStatus, ids: null, categoryId, manufacturerPartNumbers, isDownload, name);

            return await query.WhereAwait(async p => await _storeMappingService.AuthorizeAsync(p)).CountAsync();
        }

        public Product GetProductById(int productId)
        {
            if (productId == 0)
            {
                return null;
            }

            return _productRepository.Table.FirstOrDefault(product => product.Id == productId && !product.Deleted);
        }

        public Product GetProductByIdNoTracking(int productId)
        {
            if (productId == 0)
            {
                return null;
            }

            return _productRepository.Table.FirstOrDefault(product => product.Id == productId && !product.Deleted);
        }

        public async Task<ProductRenewalResult> RenewProductAsync(Product newProduct, Product existingProduct, int? extraCategoryId = null)
        {
            if (newProduct == null || existingProduct == null)
            {
                return new ProductRenewalResult
                {
                    Success = false,
                    ErrorMessage = "Product or existing product is null"
                };
            }

            var isProductTaxeslessUnpublishEnabled = await _settingService.GetSettingByKeyAsync<bool>("ApiSettings.EnableProductTaxeslessUnpublish", false);
            var storesToUpdatePublishedStateRaw = await _settingService.GetSettingByKeyAsync<string>("ApiSettings.StoresToUpdatePublishedState", "");

            var storesToUpdatePublishedState = storesToUpdatePublishedStateRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToUpperInvariant())
                .ToList();

            try
            {

                if (storesToUpdatePublishedState.Count > 0 && 
                    storesToUpdatePublishedState.Any(store => 
                        existingProduct.Sku?.Contains(store, StringComparison.OrdinalIgnoreCase) == true))
                {
                    existingProduct.Published = newProduct.Published;
                }

                if(isProductTaxeslessUnpublishEnabled)
                {
                    var hasTaxCategory = await CheckProductWithoutTaxAsync(existingProduct);
                    if (!hasTaxCategory)
                    {
                        existingProduct.Published = false;
                    }
                }

                existingProduct.StockQuantity = newProduct.StockQuantity;
                existingProduct.OrderMaximumQuantity = newProduct.OrderMaximumQuantity;
                existingProduct.ManageInventoryMethodId = newProduct.ManageInventoryMethodId;
                existingProduct.Price = newProduct.Price;
                existingProduct.OldPrice = newProduct.OldPrice;

                existingProduct.UpdatedOnUtc = DateTime.UtcNow;

                await _productService.UpdateProductAsync(existingProduct);

                if (extraCategoryId.HasValue && extraCategoryId.Value > 0)
                {
                    // Verificar si ya existe el mapping
                    var existingMapping = await _productCategoryMappingRepository.Table
                        .FirstOrDefaultAsync(pcm => pcm.ProductId == existingProduct.Id && pcm.CategoryId == extraCategoryId.Value);

                    if (existingMapping == null)
                    {
                        var newProductCategory = new ProductCategory
                        {
                            ProductId = existingProduct.Id,
                            CategoryId = extraCategoryId.Value
                        };

                        await _categoryService.InsertProductCategoryAsync(newProductCategory);
                    }
                }

                await _customerActivityService.InsertActivityAsync("RenewProduct",
                    await _localizationService.GetResourceAsync("ActivityLog.RenewProduct"), existingProduct);

                return new ProductRenewalResult
                {
                    Success = true,
                    RenewedProduct = existingProduct
                };
            }
            catch (Exception ex)
            {
                return new ProductRenewalResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while renewing the product",
                    Exception = ex
                };
            }
        }

        public async Task<bool> CheckProductWithoutTaxAsync(Product product)
        {
            if (product == null)
                return false;

            var hasTaxCategory = product.TaxCategoryId > 0;
            
            if (hasTaxCategory)
            {
                var taxCategory = await _taxCategoryService.GetTaxCategoryByIdAsync(product.TaxCategoryId);
                hasTaxCategory = taxCategory != null;
            }

            if (!hasTaxCategory)
            {
                await _customerActivityService.InsertActivityAsync("ProductWithoutTax",
                    await _localizationService.GetResourceAsync("ActivityLog.ProductWithoutTax") ?? "Product without tax category detected", 
                    product);
            }

            return hasTaxCategory;
        }

        
        private IQueryable<Product> GetProductsQuery(
            DateTime? createdAtMin = null, DateTime? createdAtMax = null,
            DateTime? updatedAtMin = null, DateTime? updatedAtMax = null, string vendorName = null,
            bool? publishedStatus = null, IList<int> ids = null, int? categoryId = null, IList<string> manufacturerPartNumbers = null, bool? isDownload = null, string name = null)

        {
            var query = _productRepository.Table;

            if (ids != null && ids.Count > 0)
            {
                query = query.Where(p => ids.Contains(p.Id));
            }

            if (manufacturerPartNumbers != null && manufacturerPartNumbers.Count > 0)
            {
                query = query.Where(p => manufacturerPartNumbers.Contains(p.ManufacturerPartNumber));
            }

            if (publishedStatus != null)
            {
                query = query.Where(p => p.Published == publishedStatus.Value);
            }

            if (isDownload != null)
            {
                query = query.Where(p => p.IsDownload == isDownload.Value);
            }

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(p => p.Name.Contains(name));
            }

            // always return products that are not deleted!!!
            query = query.Where(p => !p.Deleted);

            if (createdAtMin != null)
            {
                query = query.Where(p => p.CreatedOnUtc > createdAtMin.Value);
            }

            if (createdAtMax != null)
            {
                query = query.Where(p => p.CreatedOnUtc < createdAtMax.Value);
            }

            if (updatedAtMin != null)
            {
                query = query.Where(p => p.UpdatedOnUtc > updatedAtMin.Value);
            }

            if (updatedAtMax != null)
            {
                query = query.Where(p => p.UpdatedOnUtc < updatedAtMax.Value);
            }

            if (!string.IsNullOrEmpty(vendorName))
            {
                query = from vendor in _vendorRepository.Table
                        join product in _productRepository.Table on vendor.Id equals product.VendorId
                        where vendor.Name == vendorName && !vendor.Deleted && vendor.Active
                        select product;
            }

            if (categoryId != null)
            {
                var categoryMappingsForProduct = from productCategoryMapping in _productCategoryMappingRepository.Table
                                                 where productCategoryMapping.CategoryId == categoryId
                                                 select productCategoryMapping;

                query = from product in query
                        join productCategoryMapping in categoryMappingsForProduct on product.Id equals productCategoryMapping.ProductId
                        select product;
            }

            query = query.OrderBy(product => product.Id);

            return query;
        }
    }

    public class ProductRenewalResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Product RenewedProduct { get; set; }
        public Exception Exception { get; set; }
    }
}