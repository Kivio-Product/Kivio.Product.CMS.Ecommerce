using System.Threading.Tasks;

namespace Nop.Plugin.Misc.RecipeSuggestions.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string cacheKey) where T : class; // Return type can be nullable
        Task SetAsync(string cacheKey, object data, int cacheTimeInMinutes);
        Task RemoveAsync(string cacheKey);
    }
}
