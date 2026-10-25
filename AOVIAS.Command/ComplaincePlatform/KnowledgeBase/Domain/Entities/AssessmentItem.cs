namespace KnowledgeBase.Domain.Entities;

public class AssessmentItem
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid NodeId { get; set; }
    public string ComplianceStatus { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public bool HasActionPlan { get; set; }
    public DateTime EvaluatedAt { get; set; }

    public Assessment Assessment { get; set; } = null!;
    public ComplianceNode Node { get; set; } = null!;
}
