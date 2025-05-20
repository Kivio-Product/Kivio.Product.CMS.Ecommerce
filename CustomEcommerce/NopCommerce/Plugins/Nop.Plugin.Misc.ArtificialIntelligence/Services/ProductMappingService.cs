using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.ArtificialIntelligence.Services
{
    public partial interface IProductMappingService
    {
        Task<Product> MapIngredientToProductAsync(string ingredientName);
        Task<List<Product>> MapIngredientsToProductsAsync(List<string> ingredientNames);
    }

    public partial class ProductMappingService : IProductMappingService
    {
        private readonly IProductService _productService; // To be injected

        public ProductMappingService(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<Product> MapIngredientToProductAsync(string ingredientName)
        {
            // Placeholder for ingredient mapping logic.
            // Strategies to implement (as per issue description):
            // 1. Exact Name Match
            // 2. Keyword Search
            // 3. Use Tags/Categories
            // 4. Advanced: LLM for Mapping

            // For now, simulate a simple lookup.
            // This is a very simplified placeholder.
            if (string.IsNullOrWhiteSpace(ingredientName))
                return null;

            // Example: Try to find by exact name (case-insensitive for this example)
            var products = await _productService.SearchProductsAsync(keywords: ingredientName.Trim(), pageSize: 1);
            if (products.Any())
            {
                return products.FirstOrDefault();
            }

            return null; // No product found for this placeholder logic
        }

        public async Task<List<Product>> MapIngredientsToProductsAsync(List<string> ingredientNames)
        {
            var mappedProducts = new List<Product>();
            if (ingredientNames == null || !ingredientNames.Any())
            {
                return mappedProducts;
            }

            foreach (var name in ingredientNames)
            {
                var product = await MapIngredientToProductAsync(name);
                if (product != null)
                {
                    mappedProducts.Add(product);
                }
                // If not found, we might still want to display the ingredient name as text,
                // as mentioned in the issue description. This will be handled by RecipeSuggestionService.
            }
            return mappedProducts;
        }
    }
}
