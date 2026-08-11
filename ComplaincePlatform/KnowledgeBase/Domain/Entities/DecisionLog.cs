namespace KnowledgeBase.Domain.Entities;

public class DecisionLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid NodeId { get; set; }
    public Guid AIInteractionId { get; set; }
    public string ProblemStatement { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public string FinalDecision { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }

    public Framework Framework { get; set; } = null!;
    public ComplianceNode Node { get; set; } = null!;
    public AIInteraction AIInteraction { get; set; } = null!;
}
