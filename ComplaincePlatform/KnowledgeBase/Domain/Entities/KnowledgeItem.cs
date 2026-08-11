namespace KnowledgeBase.Domain.Entities;

public class KnowledgeItem
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
    public string HowToComply { get; set; } = string.Empty;
    public string EvidenceRequired { get; set; } = string.Empty;
    public string AuditChecklist { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public string ImpactLevel { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsActive { get; set; }

    public ComplianceNode Node { get; set; } = null!;
}
