using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Plugin.Misc.RecipeSuggestions.Models;
using static LinqToDB.Reflection.Methods.LinqToDB.Insert;

namespace Nop.Plugin.Misc.RecipeSuggestions.Services
{
    public class PersistentCacheService : ICacheService
    {
        private readonly IAiRecipeSuggestionRepository _aiRecipeSuggestionRepository;

        public PersistentCacheService(IAiRecipeSuggestionRepository repository)
        {
            _aiRecipeSuggestionRepository = repository;
        }

        public Task<T> GetAsync<T>(string productId) where T : class
        {
            _aiRecipeSuggestionRepository.GetRecipeSuggestionByIdAsync(int.Parse(productId)).ContinueWith(task =>
            {
                if (task.Result is AiRecipeSuggestion suggestion)
                {
                    return suggestion as T;
                }
                return null as T;
            });
            return Task.FromResult<T>(null);
        }

        public Task RemoveAsync(string productId)
        {
            return Task.FromResult<T>(null);
        }

        public Task SetAsync<T>(string productId, T data, int cacheTimeInMinutes) where T : class
        {
            throw new NotImplementedException();
        }
    }
}