namespace KnowledgeBase.Domain.Entities;

public class RequirementScore
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public Guid ClassificationId { get; set; }

    public ComplianceNode Node { get; set; } = null!;
    public FrameworkClassification Classification { get; set; } = null!;
}
