using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Logging;

namespace Nop.Services.Catalog;

public class ProductDeduplicationService(
    IProductService productService,
    IProductSimilarityService similarityService,
    ILogger logger,
    ICustomerActivityService customerActivityService,
    ICategoryService categoryService,
    ISettingService settingService,
    IStoreContext storeContext) : IProductDeduplicationService
{
    private readonly IStoreContext _storeContext = storeContext;
    private readonly IProductService _productService = productService;
    private readonly ISettingService _settingService = settingService;
    private readonly IProductSimilarityService _similarityService = similarityService;
    private readonly ILogger _logger = logger;
    private readonly ICategoryService _categoryService = categoryService;
    private readonly ICustomerActivityService _customerActivityService = customerActivityService;

    public async Task<DeduplicationResult> DeduplicateCategoryAsync(
        int categoryId,
        DeduplicationOptions options = null)
    {
        options ??= DeduplicationOptions.Default;

        var result = new DeduplicationResult
        {
            CategoryId = categoryId,
            StartTime = DateTime.UtcNow,
            Options = options
        };

        var currentStore = await _storeContext.GetCurrentStoreAsync();

        var minCombinedScorePercentage = await _settingService.GetSettingByKeyAsync<double>(
            "Deduplication.MinCombinedScorePercentage",
            defaultValue: 0.8);

        var duplicatesCategoryName = await _settingService.GetSettingByKeyAsync<string>(
            "Deduplication.DuplicatesCategoryName",
            defaultValue: "Duplicados");

        try
        {
            _logger.Information($"Starting deduplication for category {categoryId}");

            var publishedProducts = await GetPublishedProductsInCategoryAsync(categoryId);
            result.InitialProductCount = publishedProducts.Count;

            if (publishedProducts.Count == 0)
            {
                _logger.Warning($"No published products found in category {categoryId}");
                result.CompletedSuccessfully = true;
                return result;
            }

            _logger.Information($"Found {publishedProducts.Count} published products in category {categoryId}");

            Category duplicatesCategory = null;
            if (!string.IsNullOrEmpty(duplicatesCategoryName))
            {
                duplicatesCategory = await GetCategoryByNameAsync(duplicatesCategoryName, currentStore.Id);
                if (duplicatesCategory == null)
                {
                    _logger.Warning($"Duplicates category '{duplicatesCategoryName}' not found");
                }
            }

            var activeProductIds = new HashSet<int>(publishedProducts.Select(p => p.Id));
            var processedGroups = new HashSet<string>();
            var allProductsToUnpublish = new List<Product>();
            var allProductsToAddToCategory = new List<(Product Product, Category Category)>();

            foreach (var product in publishedProducts.ToList())
            {
                if (!activeProductIds.Contains(product.Id))
                    continue;

                result.ProductsAnalyzed++;

                try
                {
                    var duplicates = await _similarityService.FindDuplicatesStrictAsync(
                        product.Name,
                        product.Price,
                        minCombinedScore: minCombinedScorePercentage);

                    var validDuplicates = duplicates
                        .Where(d => activeProductIds.Contains(d.Product.Id) &&
                                   d.Product.Id != product.Id)
                        .ToList();

                    if (validDuplicates.Count == 0)
                        continue;

                    LogFoundDuplicates(product, validDuplicates);

                    var categoryFilteredDuplicates = await FilterDuplicatesByCategoryAsync(
                        validDuplicates, categoryId);

                    if (categoryFilteredDuplicates.Count == 0)
                        continue;

                    var duplicateGroup = new List<Product> { product };
                    duplicateGroup.AddRange(categoryFilteredDuplicates.Select(d =>
                        publishedProducts.First(p => p.Id == d.Product.Id)));

                    var groupId = GenerateGroupId(duplicateGroup);
                    if (processedGroups.Contains(groupId))
                        continue;

                    processedGroups.Add(groupId);

                    var winner = DetermineWinnerProduct(duplicateGroup, options);
                    var losers = duplicateGroup.Where(p => p.Id != winner.Id).ToList();

                    if (losers.Count != 0)
                    {
                        foreach (var loser in losers)
                        {
                            loser.Published = false;
                            allProductsToUnpublish.Add(loser);
                            activeProductIds.Remove(loser.Id);

                            if (duplicatesCategory != null)
                            {
                                allProductsToAddToCategory.Add((loser, duplicatesCategory));
                            }
                        }

                        var duplicateGroupResult = new DuplicateGroup
                        {
                            WinnerProduct = ToProductResultDto(winner),
                            UnpublishedProducts = losers.Select(ToProductResultDto).ToList(),
                            Reason = GetDuplicationReason(winner, losers),
                            SimilarityScores = categoryFilteredDuplicates.ToDictionary(
                                d => d.Product.Id,
                                d => d.CombinedScore)
                        };

                        result.DuplicateGroups.Add(duplicateGroupResult);
                        result.ProductsUnpublished += losers.Count;

                        _logger.Information($"Prepared {losers.Count} duplicates of product {winner.Id} ({winner.Name}) for unpublishing");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error processing product {product.Id} in deduplication", ex);
                    result.Errors.Add($"Error processing product {product.Id}: {ex.Message}");
                }
            }

            if (allProductsToUnpublish.Any())
            {
                await UnpublishProductsBulkAsync(allProductsToUnpublish);
                _logger.Information($"Bulk unpublished {allProductsToUnpublish.Count} duplicate products");
            }

            if (allProductsToAddToCategory.Any())
            {
                await AddProductsToCategoryBulkAsync(allProductsToAddToCategory);
                _logger.Information($"Added {allProductsToAddToCategory.Count} products to duplicates category");
            }

            result.CompletedSuccessfully = true;
            result.FinalProductCount = activeProductIds.Count;

            _logger.Information($"Deduplication completed for category {categoryId}. " +
                               $"Products analyzed: {result.ProductsAnalyzed}, " +
                               $"Groups found: {result.DuplicateGroups.Count}, " +
                               $"Products unpublished: {result.ProductsUnpublished}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Fatal error during deduplication of category {categoryId}", ex);
            result.Errors.Add($"Fatal error: {ex.Message}");
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime.Value - result.StartTime;
        }

        return result;
    }

    private async Task<List<Product>> GetPublishedProductsInCategoryAsync(int categoryId)
    {
        var products = await _productService.SearchProductsAsync(
            categoryIds: new List<int> { categoryId });

        return products.ToList();
    }

    private async Task<Category> GetCategoryByNameAsync(string categoryName, int storeId = 0)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return null;

        var categoryResult = await _categoryService.GetAllCategoriesAsync(
            categoryName: categoryName,
            showHidden: true,
            storeId: storeId);

        return categoryResult.FirstOrDefault();
    }

    private async Task<bool> IsProductInCategoryAsync(int productId, int categoryId)
    {
        var productCategories = await _categoryService.GetProductCategoriesByCategoryIdAsync(
            categoryId,
            showHidden: true);

        return productCategories.Any(pc => pc.ProductId == productId);
    }

    private async Task<List<ProductMatch>> FilterDuplicatesByCategoryAsync(
        List<ProductMatch> duplicates,
        int categoryId)
    {
        var categoryFilteredDuplicates = new List<ProductMatch>();

        foreach (var duplicate in duplicates)
        {
            if (await IsProductInCategoryAsync(duplicate.Product.Id, categoryId))
            {
                categoryFilteredDuplicates.Add(duplicate);
            }
        }

        return categoryFilteredDuplicates;
    }

    private static ProductResultDto ToProductResultDto(Product product)
    {
        return new ProductResultDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Sku = product.Sku
        };
    }

    private static string GenerateGroupId(List<Product> duplicateGroup)
    {
        return string.Join("-", duplicateGroup.Select(p => p.Id).OrderBy(x => x));
    }

    private void LogFoundDuplicates(Product product, List<ProductMatch> validDuplicates)
    {
        foreach (var duplicate in validDuplicates)
        {
            _logger.Information($"Found duplicate for product {product.Id} ({product.Name}) - " +
                              $"Duplicate {duplicate.Product.Id} ({duplicate.Product.Name}), " +
                              $"Score: {duplicate.CombinedScore:F3}, " +
                              $"MeasurementMatch: {duplicate.MeasurementMatch}");
        }
    }

    private Product DetermineWinnerProduct(List<Product> duplicateGroup, DeduplicationOptions options)
    {
        return options.WinnerSelectionStrategy switch
        {
            WinnerSelectionStrategy.LowestPrice =>
                duplicateGroup.OrderBy(p => p.Price).First(),

            WinnerSelectionStrategy.HighestPrice =>
                duplicateGroup.OrderByDescending(p => p.Price).First(),

            WinnerSelectionStrategy.MostRecent =>
                duplicateGroup.OrderByDescending(p => p.CreatedOnUtc).First(),

            WinnerSelectionStrategy.BestStock =>
                duplicateGroup
                    .OrderByDescending(p => p.StockQuantity)
                    .ThenBy(p => p.Price)
                    .First(),

            _ => duplicateGroup.OrderBy(p => p.Price).First()
        };
    }

    private async Task UnpublishProductsBulkAsync(List<Product> products)
    {
        if (products.Count == 0)
            return;

        await _productService.UpdateProductsAsync(products);
    }

    private async Task AddProductsToCategoryBulkAsync(List<(Product Product, Category Category)> productsWithCategory)
    {
        if (!productsWithCategory.Any())
            return;

        foreach (var (product, category) in productsWithCategory)
        {
            try
            {
                var productCategory = new ProductCategory
                {
                    ProductId = product.Id,
                    CategoryId = category.Id,
                    DisplayOrder = 0,
                    IsFeaturedProduct = false
                };

                await _categoryService.InsertProductCategoryAsync(productCategory);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding product {product.Id} to category {category.Id}", ex);
            }
        }
    }

    private static string GetDuplicationReason(Product winner, List<Product> losers)
    {
        return $"Winner: {winner.Name} (Price: {winner.Price:C}, Stock: {winner.StockQuantity}). " +
               $"Unpublished {losers.Count} duplicate(s) with prices: " +
               $"{string.Join(", ", losers.Select(p => $"{p.Price:C} (Stock: {p.StockQuantity})"))}";
    }
}