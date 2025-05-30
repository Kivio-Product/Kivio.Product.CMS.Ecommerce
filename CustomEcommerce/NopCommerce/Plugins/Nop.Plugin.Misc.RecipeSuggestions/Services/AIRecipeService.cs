using DotnetGeminiSDK.Client.Interfaces;
using Nop.Core.Domain.Catalog; 
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Services.Logging;

namespace Nop.Plugin.Misc.RecipeSuggestions.Services
{
    public class AIRecipeService : IAIRecipeService
    {
        private readonly IGeminiClient _geminiClient;
        private readonly ILogger _logger;

        public AIRecipeService(IGeminiClient geminiClient, ILogger logger)
        {
            _geminiClient = geminiClient;
            _logger = logger;
        }
        public async Task<(string RecipeTitle, List<(string Name, string DescriptionForImage)> Ingredients)> GetRecipeFromAIAsync(Product currentProduct, List<Product> availableStoreProducts)
        {
            // Create the prompt for the AI model.
            // Structure prompt: Generate a short recipe using ID: 2014 - Leche Enterea TetraPack 1000ml and potentially these ingredients (chose only four or less): [ID: 1015 - Whisky Jack Daniels 700ml,ID: 4052 - Queso Crema Alqueria 250gr,ID: 3023 - Huevos x30 AAA, ID: 510 - Jabón detergente Fab 1000kg, ID: 511 - Salsa de tomate Fruco 750ml]. Provide recipe title and a list of ingredients with id (id - exactly ingredient name) if apply. For new ingredients, provide a description for image generation and add the 9999 ID. Give me the ingredients on this format eg: [2014 - Leche Enterea TetraPack 1000ml, 1015 - Whisky Jack Daniels 700ml, 9999 - 'new ingredient 1', 9999 - 'new ingredient 2', ... ]: 
            var prompt = $"Generate a short recipe using ID: {currentProduct.Id} - {currentProduct.Name} and potentially these ingredients (choose only four or less): [{string.Join(",", availableStoreProducts.GetRange(0,3).Select(p => $"{p.Id} - {p.Name}"))}]. Provide recipe title and a list of ingredients with id (id - exactly ingredient name) if apply. For new ingredients, provide a description for image generation and add the 9999 ID. Give me the ingredients on this format eg: [{currentProduct.Id} - {currentProduct.Name}, {string.Join(", ", availableStoreProducts.GetRange(0, 3).Select(p => $"{p.Id} - {p.Name}"))}, 9999 - 'new ingredient 1', 9999 - 'new ingredient 2', ... ]:";

            // Call the AI model to generate the recipe.
            var response = await _geminiClient.TextPrompt(prompt);

            if (response == null || response.Candidates.Count == 0)
            {
                throw new Exception("AI response is null or empty.");
            }
            // Process the AI response to extract the recipe title and ingredients.
            _logger.Information($"Complete AI response: {response.Candidates[0].Content}");

            // TODO: Parse the response to extract the recipe title and ingredients.

            // Placeholder implementation:
            await Task.Delay(100); // Simulate async work
            var ingredients = new List<(string Name, string DescriptionForImage)>
            {
                ("Ingredient A (from AI)", "A bright red apple"),
                (availableStoreProducts.FirstOrDefault()?.Name ?? "Store Product X", "") // Simulate using an existing product
            };
            return ($"AI Recipe for {currentProduct.Name}", ingredients);
        }

        public async Task<string> GenerateImageForIngredientAsync(string ingredientDescription)
        {
            // Placeholder implementation:
            await Task.Delay(50); // Simulate async work
            return $"https://via.placeholder.com/150/0000FF/808080?Text=AI_Image_for_{Uri.EscapeDataString(ingredientDescription)}";
        }
    }
}
