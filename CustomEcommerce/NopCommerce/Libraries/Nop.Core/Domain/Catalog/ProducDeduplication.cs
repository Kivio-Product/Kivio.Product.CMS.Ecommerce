namespace Nop.Core.Domain.Catalog;

public class DeduplicationOptions
{
    public WinnerSelectionStrategy WinnerSelectionStrategy { get; set; } = WinnerSelectionStrategy.LowestPrice;
    public double MinJaccardScore { get; set; } = 0.4;
    public double MaxPriceDifferencePercent { get; set; } = 20.0;
    public int MaxDuplicatesPerProduct { get; set; } = 5;
    public int MaxDbCandidates { get; set; } = 200;

    public static DeduplicationOptions Default => new();
}

public enum WinnerSelectionStrategy
{
    LowestPrice,
    HighestPrice,
    MostRecent,
    BestStock
}

public class DeduplicationResult
{
    public int CategoryId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool CompletedSuccessfully { get; set; }
    public DeduplicationOptions Options { get; set; }

    // Estadísticas
    public int InitialProductCount { get; set; }
    public int FinalProductCount { get; set; }
    public int ProductsAnalyzed { get; set; }
    public int ProductsUnpublished { get; set; }

    // Detalles
    public List<DuplicateGroup> DuplicateGroups { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    // Propiedades calculadas
    public int DuplicateGroupsFound => DuplicateGroups.Count;
    public double DeduplicationRate => InitialProductCount > 0 ?
        (double)ProductsUnpublished / InitialProductCount * 100 : 0;
}

public class DuplicateGroup
{
    public Product WinnerProduct { get; set; }
    public List<Product> UnpublishedProducts { get; set; } = new();
    public string Reason { get; set; }
    public Dictionary<int, double> SimilarityScores { get; set; } = new();
}