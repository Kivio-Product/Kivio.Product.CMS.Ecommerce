using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IProductSimilarityService
{
    public Task<IList<ProductMatch>> FindSimilarByNameAsync(
        string productName,
        decimal? originalPrice = null,
        int maxResults = 10,
        double minJaccardScore = 0.3,
        int maxDbCandidates = 200,
        double? maxPriceDifferencePercent = null,
        bool strictMeasurementMatching = true);
}