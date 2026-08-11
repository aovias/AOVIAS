namespace KnowledgeBase.Domain.Entities;

public class ScoringConfig
{
    public Guid Id { get; set; }
    public Guid FrameworkId { get; set; }
    public decimal FullyCompliantScore { get; set; }
    public decimal PartialScore { get; set; }
    public decimal NonCompliantScore { get; set; }
    public string AggregationMethod { get; set; } = string.Empty;
    public string ScaleType { get; set; } = string.Empty;

    public Framework Framework { get; set; } = null!;
}
