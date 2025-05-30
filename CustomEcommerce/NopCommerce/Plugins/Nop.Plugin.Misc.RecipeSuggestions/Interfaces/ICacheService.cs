using System.Threading.Tasks;

namespace Nop.Plugin.Misc.RecipeSuggestions.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string cacheKey) where T : class;
        Task SetAsync<T>(string cacheKey, T data, int cacheTimeInMinutes) where T : class;
        Task RemoveAsync(string cacheKey);
    }
}
