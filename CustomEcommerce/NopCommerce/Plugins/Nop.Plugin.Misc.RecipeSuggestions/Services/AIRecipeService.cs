using DocumentFormat.OpenXml.Office2010.Excel;
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
        public async Task<(string RecipeTitle, List<(string ProductID, string IngredientName)> Ingredients, string Instructions)> GetRecipeFromAIAsync(Product currentProduct, List<Product> availableStoreProducts)
        {
            // Create the prompt for the AI model.
            // Structure  prompt: Crea una receta corta para el producto ID: 2014 - Leche Enterea TetraPack 1000ml y con estos potenciales ingredientes adicionales (Elige 3 o menos): [ID: 1015 - Whisky Jack Daniels 700ml,ID: 4052 - Queso Crema Alqueria 250gr,ID: 3023 - Huevos x30 AAA, ID: 510 - Jabón detergente Fab 1000kg, ID: 511 - Salsa de tomate Fruco 750ml]. IMPORTANTE: Responde siguiendo estrictamente esta estructura, eg:{ Title: 'Titulo de la receta', Ingredients: [{ Id: 'XXXX', Name: 'exactly name of existing principal ingredient'}, { Id: 'XXXX', Name: 'exactly name of existing ingredient on batch'}, { Id: '9999', Name: 'new ingredient 1'}, { Id: '9999', Name: 'new ingredient 2'}...],Instructions: 'Instrucciones breves de la receta'} Consideraciones: Aquellos ingredientes que si te haya proporcionado agrégalos a la lista de ingredientes con id y nombre exactamente igual a como te lo compartí. Para ingredientes que no te haya proporcionado, colócales el Id : '9999'Las instrucciones de la receta deben quedar en español
            var prompt = $"Crea una receta corta para el producto ID: {currentProduct.Id} - {currentProduct.Name} y con estos potenciales ingredientes adicionales (Elige 3 o menos): [{string.Join(",", availableStoreProducts.Select(p => $"ID: {p.Id} - {p.Name}"))}]. IMPORTANTE: Responde siguiendo estrictamente esta estructura, eg: {{ Title: 'Titulo de la receta', Ingredients: [{{ Id: '{currentProduct.Id}', Name: '{currentProduct.Name}' }}, {{Id: 'XXXX', Name: 'exactly name of existing ingredient on batch'}}, {{ Id: '9999', Name: 'nuevo ingrediente 1' }}, {{ Id: '9999', Name: 'nuevo ingrediente 2' }}...], Instructions: 'Instrucciones breves de la receta'}} Consideraciones: Aquellos ingredientes que si te haya proporcionado agrégalos a la lista de ingredientes con id y nombre exactamente igual a como te lo compartí. Para ingredientes que no te haya proporcionado, colócales el Id : '9999'. Las instrucciones de la receta deben quedar en español.";

            _logger.Information($"AI Recipe Prompt: {prompt}");
            // Call the AI model to generate the recipe.
            var response = await _geminiClient.TextPrompt(prompt);

            if (response == null || response.Candidates.Count == 0)
            {
                throw new Exception("AI response is null or empty.");
            }
            // Process the AI response to extract the recipe title and ingredients.
            _logger.Information($"Complete AI response: {response.Candidates[0].Content.Parts.FirstOrDefault()?.Text}");


            // TODO: Parse the response to extract the recipe title and ingredients.
            // This is a placeholder implementation. You would need to implement actual parsing logic.
            var recipeTitle = "Placeholder Recipe Title"; // Extracted from AI response
            var ingredients = new List<(string ProductID, string IngredientName)>
            {
                (currentProduct.Id.ToString(), currentProduct.Name),
                ("9999", "New Ingredient 1"), // Placeholder for new ingredients
                ("9999", "New Ingredient 2")  // Placeholder for new ingredients
            };
            var instructions = "Placeholder instructions for the recipe."; // Extracted from AI response
            // Return the recipe title, ingredients, and instructions.
            return (recipeTitle, ingredients, instructions);
        }

        public async Task<string> GenerateImageForIngredientAsync(string ingredientDescription)
        {
            // Placeholder implementation:
            await Task.Delay(50); // Simulate async work
            return $"https://via.placeholder.com/150/0000FF/808080?Text=AI_Image_for_{Uri.EscapeDataString(ingredientDescription)}";
        }
    }
}
