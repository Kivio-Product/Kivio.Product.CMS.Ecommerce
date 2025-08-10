namespace Nop.Core.Domain.Catalog;

public class ProductCandidate
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public decimal Price { get; set; }
    public string? ShortDescription { get; set; }
    public int FtRank { get; set; }
}

public class ProductMatch
{
    public ProductCandidate Product { get; set; } = default!;
    public double JaccardSimilarity { get; set; }
    public double LevenshteinSimilarity { get; set; }
    public bool MeasurementMatch { get; set; } = true;
    public double CombinedScore { get; set; }

    public string MatchQuality => CombinedScore switch
    {
        >= 0.8 => "EXCELENTE",
        >= 0.6 => "BUENO",
        >= 0.4 => "REGULAR",
        _ => "BAJO"
    };

    public int JaccardPercentage => (int)(JaccardSimilarity * 100);
    public int CombinedPercentage => (int)(CombinedScore * 100);
    public int LevenshteinPercentage => (int)(LevenshteinSimilarity * 100);
}


public enum ProductSimilarityMode
{
    DuplicateDetection,
    RelatedProducts,
    Balanced
}
