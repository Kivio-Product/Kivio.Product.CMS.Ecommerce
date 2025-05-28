using Nop.Core.Caching; // For IStaticCacheManager and CacheKey
using Nop.Plugin.Misc.RecipeSuggestions.Interfaces; // For ICacheService
using System.Threading.Tasks;

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
            await _staticCacheManager.SetAsync(key, data, cacheTimeInMinutes);
        }

        public async Task RemoveAsync(string cacheKey)
        {
            await _staticCacheManager.RemoveAsync(new CacheKey(cacheKey));
        }
    }
}
