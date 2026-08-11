namespace KnowledgeBase.Domain.Entities;

public class Framework
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<ComplianceNode> Nodes { get; set; } = new List<ComplianceNode>();
    public ICollection<FrameworkClassification> Classifications { get; set; } = new List<FrameworkClassification>();
    public ScoringConfig? ScoringConfig { get; set; }
}
