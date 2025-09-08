namespace Nop.Core.Domain.Catalog;

public class ProductSuggestion
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Relevance { get; set; }
}

public class ProductSuggestionDb
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int FtRank { get; set; }
}
