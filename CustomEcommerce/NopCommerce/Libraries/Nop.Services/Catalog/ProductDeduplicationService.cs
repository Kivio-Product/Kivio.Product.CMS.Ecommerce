using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Logging;

namespace Nop.Services.Catalog;

public class ProductDeduplicationService(
    IProductService productService,
    IProductSimilarityService similarityService,
    ILogger logger,
    ICustomerActivityService customerActivityService,
    ICategoryService categoryService) : IProductDeduplicationService
{
    private readonly IProductService _productService = productService;
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

        try
        {
            _logger.Information($"Starting deduplication for category {categoryId}");

            var publishedProducts = await GetPublishedProductsInCategory(categoryId);
            result.InitialProductCount = publishedProducts.Count;

            if (publishedProducts.Count == 0)
            {
                _logger.Warning($"No published products found in category {categoryId}");
                result.CompletedSuccessfully = true;
                return result;
            }

            _logger.Information($"Found {publishedProducts.Count} published products in category {categoryId}");

            var activeProductIds = new HashSet<int>(publishedProducts.Select(p => p.Id));
            var processedGroups = new HashSet<string>();

            foreach (var product in publishedProducts.ToList())
            {
                if (!activeProductIds.Contains(product.Id))
                    continue;

                result.ProductsAnalyzed++;

                try
                {
                    var duplicates = await _similarityService.FindSimilarByNameAsync(
                        product.Name,
                        product.Price,
                        options.MaxDuplicatesPerProduct,
                        options.MinJaccardScore,
                        options.MaxDbCandidates,
                        options.MaxPriceDifferencePercent);

                    var validDuplicates = duplicates
                        .Where(d => activeProductIds.Contains(d.Product.Id) &&
                                   d.Product.Id != product.Id)
                        .ToList();

                    // print valid duplicates for debugging

                    foreach (var duplicate in validDuplicates)
                    {
                        _logger.Information($"Found duplicate for product {product.Id} ({product.Name}) " +
                                      $"Duplicate {duplicate.Product.Id} ({duplicate.Product.Name}), " +
                                      $"Score: {duplicate.CombinedScore}, " + $"MeasurementMatch: {duplicate.MeasurementMatch}");
                    }

                    var categoryFilteredDuplicates = new List<ProductMatch>();
                    foreach (var duplicate in validDuplicates)
                    {
                        if (await IsProductInCategoryAsync(duplicate.Product.Id, categoryId))
                        {
                            categoryFilteredDuplicates.Add(duplicate);
                        }
                    }

                    if (categoryFilteredDuplicates.Count == 0)
                        continue;

                    var duplicateGroup = new List<Product> { product };
                    duplicateGroup.AddRange(categoryFilteredDuplicates.Select(d =>
                        publishedProducts.First(p => p.Id == d.Product.Id)));

                    var groupId = string.Join("-", duplicateGroup.Select(p => p.Id).OrderBy(x => x));
                    if (processedGroups.Contains(groupId))
                        continue;

                    processedGroups.Add(groupId);

                    var winner = DetermineWinnerProduct(duplicateGroup, options);
                    var losers = duplicateGroup.Where(p => p.Id != winner.Id).ToList();

                    if (losers.Any())
                    {
                        await UnpublishProductsAsync(losers);

                        foreach (var loser in losers)
                        {
                            activeProductIds.Remove(loser.Id);
                        }

                        result.DuplicateGroups.Add(new DuplicateGroup
                        {
                            WinnerProduct = winner,
                            UnpublishedProducts = losers.ToList(),
                            Reason = GetDuplicationReason(winner, losers),
                            SimilarityScores = categoryFilteredDuplicates.ToDictionary(
                                d => d.Product.Id,
                                d => d.CombinedScore)
                        });

                        result.ProductsUnpublished += losers.Count;

                        _logger.Information($"Unpublished {losers.Count} duplicates of product {winner.Id} ({winner.Name})");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error processing product {product.Id} in deduplication", ex);
                    result.Errors.Add($"Error processing product {product.Id}: {ex.Message}");
                }
            }

            result.CompletedSuccessfully = true;
            result.FinalProductCount = activeProductIds.Count;

            _logger.Information($"Deduplication completed for category {categoryId}. " +
                               $"Products analyzed: {result.ProductsAnalyzed}, Groups found: {result.DuplicateGroups.Count}, Products unpublished: {result.ProductsUnpublished}");

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

    private async Task<List<Product>> GetPublishedProductsInCategory(int categoryId)
    {
        var products = await _productService.SearchProductsAsync(
            categoryIds: new List<int> { categoryId });

        return products.ToList();
    }

    private async Task<bool> IsProductInCategoryAsync(int productId, int categoryId)
    {
        var productCategories = await _categoryService.GetProductCategoriesByCategoryIdAsync(
            categoryId, 
            showHidden: false);

        Console.WriteLine($"Checking if product {productId} is in category {categoryId}: " +
                          $"{productCategories.Any(pc => pc.ProductId == productId)}");

        return productCategories.Any(pc => pc.ProductId == productId);
    }

    private Product DetermineWinnerProduct(List<Product> duplicateGroup, DeduplicationOptions options)
    {
        return options.WinnerSelectionStrategy switch
        {
            WinnerSelectionStrategy.LowestPrice => duplicateGroup.OrderBy(p => p.Price).First(),

            WinnerSelectionStrategy.HighestPrice => duplicateGroup.OrderByDescending(p => p.Price).First(),

            WinnerSelectionStrategy.MostRecent => duplicateGroup.OrderByDescending(p => p.CreatedOnUtc).First(),

            WinnerSelectionStrategy.BestStock => duplicateGroup
                .OrderByDescending(p => p.StockQuantity)
                .ThenBy(p => p.Price)
                .First(),

            _ => duplicateGroup.OrderBy(p => p.Price).First()
        };
    }

    private async Task UnpublishProductsAsync(List<Product> products)
    {
        foreach (var product in products)
        {
            product.Published = false;
            await _productService.UpdateProductAsync(product);

            await _customerActivityService.InsertActivityAsync("AutoDeduplication.UnpublishProduct",
                $"Product automatically unpublished due to duplication detection", product);
        }
    }

    private static string GetDuplicationReason(Product winner, List<Product> losers)
    {
        return $"Winner: {winner.Name} (Price: {winner.Price:C}). " +
               $"Unpublished {losers.Count} duplicate(s) with prices: {string.Join(", ", losers.Select(p => p.Price.ToString("C")))}";
    }
}