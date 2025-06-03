using System;
using System.Collections.Generic; 
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DotnetGeminiSDK.Client.Interfaces;
using DotnetGeminiSDK.Model.Request;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Plugin.Misc.RecipeSuggestions.Models;
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
            var prompt = $"Crea una receta corta para el producto ID: {currentProduct.Id} - {currentProduct.Name} y con estos potenciales ingredientes adicionales (Elige 3 o menos): [{string.Join(",", availableStoreProducts.Select(p => $"ID: {p.Id} - {p.Name}"))}]. IMPORTANTE: Responde siguiendo estrictamente esta estructura, eg: {{ \"Title\": \"Titulo de la receta\", \"Ingredients\": [{{ \"Id\": \"{currentProduct.Id}\", \"Name\": \"{currentProduct.Name}\" }}, {{\"Id\": \"XXXX\", \"Name\": \"exactly name of existing ingredient on batch\"}}, {{ \"Id\": \"9999\", \"Name\": \"nuevo ingrediente 1\" }}, {{ \"Id\": \"9999\", \"Name\": \"nuevo ingrediente 2\" }}...], \"Instructions\": \"Instrucciones breves de la receta\"}} Consideraciones: Aquellos ingredientes que si te haya proporcionado agrégalos a la lista de ingredientes con id y nombre exactamente igual a como te lo compartí. Para ingredientes que no te haya proporcionado, colócales el Id : '9999'. Las instrucciones de la receta deben quedar en español.";

            _logger.Information($"AI Recipe Prompt: {prompt}");
            
            var response = await _geminiClient.TextPrompt(prompt);

            if (response == null || response.Candidates == null || !response.Candidates.Any())
            {
                _logger.Error("AI response is null or has no candidates.");
                throw new Exception("AI response is null or has no candidates.");
            }
            
            var responseContent = response.Candidates[0].Content;
            if (responseContent == null || responseContent.Parts == null || !responseContent.Parts.Any())
            {
                _logger.Error("AI response candidate has no content or parts.");
                throw new Exception("AI response candidate has no content or parts.");
            }

            var responseText = responseContent.Parts.FirstOrDefault()?.Text;
            _logger.Information($"Raw AI response text: {responseText}");

            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.Error("AI response text is null or whitespace.");
                throw new Exception("AI response text is null or whitespace.");
            }

            string recipeTitle;
            List<(string ProductID, string IngredientName)> ingredients;
            string instructions;

            try
            {
                // Attempt to clean the response text if it's wrapped in markdown code block
                var cleanedResponseText = responseText.Trim();
                if (cleanedResponseText.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                {
                    cleanedResponseText = cleanedResponseText.Substring("```json".Length).TrimStart();
                    if (cleanedResponseText.EndsWith("```"))
                    {
                        cleanedResponseText = cleanedResponseText.Substring(0, cleanedResponseText.Length - 3).TrimEnd();
                    }
                }
                else if (cleanedResponseText.StartsWith("```")) // Simpler ``` ``` block without "json" tag
                {
                     cleanedResponseText = cleanedResponseText.Substring(3).TrimStart();
                     if (cleanedResponseText.EndsWith("```"))
                     {
                        cleanedResponseText = cleanedResponseText.Substring(0, cleanedResponseText.Length - 3).TrimEnd();
                     }
                }
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // Handles "Title" vs "title", "ID" vs "Id"
                };

                AiRecipeResponse parsedRecipe = null;
                try
                {
                    parsedRecipe = JsonSerializer.Deserialize<AiRecipeResponse>(cleanedResponseText, options);
                }
                catch (JsonException jsonEx)
                {
                    _logger.Error($"JSON Deserialization failed: {jsonEx.Message}. Response text was: '{cleanedResponseText}'", jsonEx);
                }

                if (parsedRecipe == null)
                {
                    _logger.Error("Failed to deserialize AI response: parsed object is null.");
                    return ("", new List<(string, string)>(), "");
                }

                recipeTitle = parsedRecipe.Title;
                instructions = parsedRecipe.Instructions;
                ingredients = new List<(string ProductID, string IngredientName)>();

                if (parsedRecipe.Ingredients != null)
                {
                    foreach (var ingredient in parsedRecipe.Ingredients)
                    {
                        // Ensure ingredient and its name are not null/whitespace.
                        // Default Id to "9999" if it's null/empty, as per prompt's intention for new ingredients.
                        if (ingredient != null && !string.IsNullOrWhiteSpace(ingredient.Name))
                        {
                            ingredients.Add((string.IsNullOrWhiteSpace(ingredient.Id) ? "9999" : ingredient.Id, ingredient.Name));
                        }
                        else
                        {
                            _logger.Warning("Parsed an ingredient that was null, or had a null/empty name. Skipping it.");
                        }
                    }
                }

                // Fallbacks for missing critical information
                if (string.IsNullOrWhiteSpace(recipeTitle))
                {
                    _logger.Warning("Recipe title was missing or empty in AI response. Using a default title.");
                    recipeTitle = "AI Generated Recipe"; 
                }
                if (string.IsNullOrWhiteSpace(instructions))
                {
                    _logger.Warning("Recipe instructions were missing or empty in AI response. Using default instructions.");
                    instructions = "No instructions provided by AI.";
                }
                if (!ingredients.Any())
                {
                    _logger.Warning("Recipe ingredients list was empty or null in AI response. Adding the current product as a fallback ingredient.");
                    // As a fallback, add the current product if no ingredients were parsed
                    ingredients.Add((currentProduct.Id.ToString(), currentProduct.Name));
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.Error($"JSON Deserialization failed: {jsonEx.Message}. Response text was: '{responseText}'", jsonEx);
                // Provide a more user-friendly error or rethrow with context
                throw new Exception($"Failed to parse AI response as JSON. The AI's response might not be in the expected JSON format. Details: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors during processing
                _logger.Error($"An unexpected error occurred during AI response processing: {ex.Message}. Response text was: '{responseText}'", ex);
                throw new Exception($"An unexpected error occurred while processing the AI recipe response. Details: {ex.Message}", ex);
            }
            
            return (recipeTitle, ingredients, instructions);
        }

        public async Task<string> GenerateImageForIngredientAsync(string ingredientName)
        {
            var prompt = $"Genera una imagen de alta calidad de un ingrediente llamado '{ingredientName}'. La imagen debe ser clara, bien iluminada y mostrar el ingrediente de manera atractiva con un fondo de mesa de cocina. Dame solo la imagen";
            return await ProcessImagePrompt(prompt);
        }

        public async Task<string> GenerateImageForRecipeAsync(string recipeName)
        {
            var prompt = $"Genera una imagen de alta calidad de una receta llamada {recipeName}. La imagen debe ser clara, bien iluminada y atractiva. Dame solo la imagen";
            return await ProcessImagePrompt(prompt);
        }

        private async Task<string> ProcessImagePrompt(string prompt)
        {
            _logger.Information($"AI Image Generation Prompt: {prompt}");
            var response = await _geminiClient.GenerateImagePrompt(prompt);

            if (response == null || response.Candidates == null || !response.Candidates.Any())
            {
                _logger.Error("AI response is null or has no candidates.");
                throw new Exception("AI response is null or has no candidates.");
            }
            var responseContent = response.Candidates[0].Content;
            if (responseContent == null || responseContent.Parts == null || !responseContent.Parts.Any())
            {
                _logger.Error("AI response candidate has no content or parts.");
                throw new Exception("AI response candidate has no content or parts.");
            }
            // Find the part with inline data
            var imagePart = responseContent.Parts.FirstOrDefault(p => p.InlineData != null && p.InlineData.Data != null);
            if (imagePart == null || string.IsNullOrWhiteSpace(imagePart.InlineData.Data))
            {
                _logger.Error("AI response does not contain a valid image part with inline data.");
                throw new Exception("AI response does not contain a valid image part with inline data.");
            }
            // Decode the base64 image data
            var base64Image = imagePart.InlineData.Data;
            if (string.IsNullOrWhiteSpace(base64Image))
            {
                _logger.Error("Base64 image data is null or empty.");
                throw new Exception("Base64 image data is null or empty.");
            }
            return base64Image;
        }
    }
}