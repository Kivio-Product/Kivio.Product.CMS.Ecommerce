namespace Nop.Core.Domain.Catalog;

public class ValidationJobResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ValidationJobData Data { get; set; } = new();
    public ValidationSummary Summary { get; set; } = new();
}

public class ValidationJobData
{
    public int JobId { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public string StatusUrl { get; set; } = string.Empty;
}

public class ValidationSummary
{
    public int TotalRequested { get; set; }
    public int TotalValid { get; set; }
    public bool JobCreated { get; set; }
}

public class ValidationJobStatusResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ValidationJobStatusData Data { get; set; } = new();
}

public class ValidationJobStatusData
{
    public int JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public ValidationProgress Progress { get; set; } = new();
    public ValidationTiming Timing { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool IsCompleted { get; set; }
}

public class ValidationProgress
{
    public int TotalProducts { get; set; }
    public int ProcessedProducts { get; set; }
    public int SuccessfulProducts { get; set; }
    public int FailedProducts { get; set; }
    public double ProgressPercentage { get; set; }
}

public class ValidationTiming
{
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public long? EstimatedTimeRemainingMs { get; set; }
}

public class ProductSnapshot
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool Published { get; set; }
    public DateTime SnapshotDate { get; set; }
    public int StockQuantity { get; set; }
}

public class ProductChangeResult
{
    public bool HasChanges { get; set; }
    public IList<ProductChange> Changes { get; set; } = new List<ProductChange>();
    public DateTime CheckedAt { get; set; }
    public bool IsJobCompleted { get; set; }
}

public class ProductChange
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty; // "price_changed", "unpublished", "deleted"
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CheckoutValidation 
{
    public bool CanProceed { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool ShouldRedirectHome { get; set; }
    public bool HasProductChanges { get; set; }
    public IList<ProductChange> ProductChanges { get; set; } = new List<ProductChange>();
    public bool IsJobCompleted { get; set; }
}