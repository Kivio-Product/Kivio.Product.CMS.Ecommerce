using Nop.Core.Caching;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces;
using Nop.Plugin.Misc.RecipeSuggestions.Models;

namespace Nop.Plugin.Misc.RecipeSuggestions.Services
{
    public class CacheService : ICacheService
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public CacheService(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
        }

        public async Task<RecipeSuggestionViewModel> GetAsync(string productId)
        {
            return await _staticCacheManager.GetAsync<RecipeSuggestionViewModel>(new CacheKey(productId), () => Task.FromResult<RecipeSuggestionViewModel>(null));
        }

        public async Task SetAsync(string cacheKey, RecipeSuggestionViewModel data, int cacheTimeInMinutes)
        {
            var key = new CacheKey(cacheKey);
            key.CacheTime = cacheTimeInMinutes;
            await _staticCacheManager.SetAsync(key, data);
        }

        public async Task RemoveAsync(string cacheKey)
        {
            await _staticCacheManager.RemoveAsync(new CacheKey(cacheKey));
        }
    }
}
