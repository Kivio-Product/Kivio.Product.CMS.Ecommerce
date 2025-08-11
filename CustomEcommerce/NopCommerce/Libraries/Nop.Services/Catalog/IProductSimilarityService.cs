using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IProductSimilarityService
{
    public Task<IList<ProductMatch>> FindSimilarForSearchAsync(string productName, decimal? originalPrice = null, int maxResults = 10);
    public Task<IList<ProductMatch>> FindDuplicatesStrictAsync(string productName, decimal? originalPrice = null, int maxResults = 10);
}