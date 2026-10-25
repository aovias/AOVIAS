namespace KnowledgeBase.Domain.Entities;

public class ComplianceNode
{
    public Guid Id { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid? ParentId { get; set; }
    public int Level { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public int Sequence { get; set; }

    public Framework Framework { get; set; } = null!;
    public ComplianceNode? Parent { get; set; }
    public ICollection<ComplianceNode> Children { get; set; } = new List<ComplianceNode>();

    public RequirementDetail? RequirementDetail { get; set; }
    public ICollection<KnowledgeItem> KnowledgeItems { get; set; } = new List<KnowledgeItem>();
    public RequirementScore? RequirementScore { get; set; }
    public ICollection<DecisionLog> DecisionLogs { get; set; } = new List<DecisionLog>();
}
