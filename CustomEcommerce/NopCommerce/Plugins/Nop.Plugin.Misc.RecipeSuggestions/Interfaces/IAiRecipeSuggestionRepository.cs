using Nop.Plugin.Misc.RecipeSuggestions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.RecipeSuggestions.Interfaces
{
    public interface IAiRecipeSuggestionRepository
    {
        Task InsertRecipeSuggestionAsync(AiRecipeSuggestion suggestion);
        Task UpdateRecipeSuggestionAsync(AiRecipeSuggestion suggestion);
        Task DeleteRecipeSuggestionAsync(AiRecipeSuggestion suggestion);
        Task<AiRecipeSuggestion> GetRecipeSuggestionByIdAsync(int suggestionId);
        Task<IList<AiRecipeSuggestion>> GetRecipeSuggestionsByProductIdAsync(int productId);
        Task<IList<AiRecipeIngredient>> GetIngredientsBySuggestionIdAsync(int suggestionId);
        Task InsertIngredientAsync(AiRecipeIngredient ingredient);
        Task UpdateIngredientAsync(AiRecipeIngredient ingredient);
        Task DeleteIngredientAsync(AiRecipeIngredient ingredient);
    }
}
