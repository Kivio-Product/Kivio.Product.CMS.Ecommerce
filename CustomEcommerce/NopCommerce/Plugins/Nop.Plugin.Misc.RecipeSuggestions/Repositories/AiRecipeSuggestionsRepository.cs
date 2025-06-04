using Nop.Data;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Plugin.Misc.RecipeSuggestions.Models;

namespace Nop.Plugin.Misc.RecipeSuggestions.Repositories
{
    public class AiRecipeSuggestionRepository : IAiRecipeSuggestionRepository
    {
        private readonly IRepository<AiRecipeSuggestion> _suggestionRepository;
        private readonly IRepository<AiRecipeIngredient> _ingredientRepository;

        public AiRecipeSuggestionRepository(
            IRepository<AiRecipeSuggestion> suggestionRepository,
            IRepository<AiRecipeIngredient> ingredientRepository)
        {
            _suggestionRepository = suggestionRepository;
            _ingredientRepository = ingredientRepository;
        }

        public virtual async Task InsertRecipeSuggestionAsync(AiRecipeSuggestion suggestion)
        {
            ArgumentNullException.ThrowIfNull(suggestion);
            await _suggestionRepository.InsertAsync(suggestion);
            // Los ingredientes se guardan por separado, no hay inserción en cascada por navegación.
        }

        public virtual async Task UpdateRecipeSuggestionAsync(AiRecipeSuggestion suggestion)
        {
            ArgumentNullException.ThrowIfNull(suggestion);
            await _suggestionRepository.UpdateAsync(suggestion);
        }

        public virtual async Task DeleteRecipeSuggestionAsync(AiRecipeSuggestion suggestion)
        {
            ArgumentNullException.ThrowIfNull(suggestion);
            await _suggestionRepository.DeleteAsync(suggestion);
        }

        public virtual async Task<AiRecipeSuggestion> GetRecipeSuggestionByIdAsync(int suggestionId)
        {
            if (suggestionId == 0)
                return null;

            // Incluye los ingredientes relacionados
            var query = from s in _suggestionRepository.Table
                        where s.Id == suggestionId
                        select s;
            var suggestion = await query.FirstOrDefaultAsync();
            if (suggestion != null)
            {
                suggestion.Ingredients = await GetIngredientsBySuggestionIdAsync(suggestionId);
            }
            return suggestion;
        }

        public virtual async Task<IList<AiRecipeSuggestion>> GetRecipeSuggestionsByProductIdAsync(int productId)
        {
            if (productId == 0)
                return new List<AiRecipeSuggestion>();

            // IRepository<T>.Table devuelve un IQueryable<T> que puedes usar con Linq.
            // Linq2DB se encarga de traducir esto a SQL.
            var query = from s in _suggestionRepository.Table
                        where s.ProductId == productId
                        orderby s.CreatedOnUtc descending
                        select s;

            return await query.ToListAsync();
        }

        public virtual async Task<IList<AiRecipeIngredient>> GetIngredientsBySuggestionIdAsync(int suggestionId)
        {
            if (suggestionId == 0)
                return new List<AiRecipeIngredient>();

            var query = from i in _ingredientRepository.Table
                        where i.AiRecipeSuggestionId == suggestionId
                        select i;

            return await query.ToListAsync();
        }

        public virtual async Task InsertIngredientAsync(AiRecipeIngredient ingredient)
        {
            ArgumentNullException.ThrowIfNull(ingredient);
            await _ingredientRepository.InsertAsync(ingredient);
        }

        public virtual async Task UpdateIngredientAsync(AiRecipeIngredient ingredient)
        {
            ArgumentNullException.ThrowIfNull(ingredient);
            await _ingredientRepository.UpdateAsync(ingredient);
        }

        public virtual async Task DeleteIngredientAsync(AiRecipeIngredient ingredient)
        {
            ArgumentNullException.ThrowIfNull(ingredient);
            await _ingredientRepository.DeleteAsync(ingredient);
        }
    }
}
