using Nop.Core.Caching;
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces; 

namespace Nop.Plugin.Misc.RecipeSuggestions.Services
{
    public class CacheService : ICacheService
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public CacheService(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
        }

        public async Task<T?> GetAsync<T>(string cacheKey) where T : class
        {
            return await _staticCacheManager.GetAsync<T?>(new CacheKey(cacheKey), () => Task.FromResult<T?>(null));
        }

        public async Task SetAsync(string cacheKey, object data, int cacheTimeInMinutes)
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
