namespace KnowledgeBase.Domain.Entities;

public class AIInteraction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid? ContextNodeId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public DateTime CreatedAt { get; set; }

    public Framework Framework { get; set; } = null!;
    public ComplianceNode? ContextNode { get; set; }
    public ICollection<DecisionLog> DecisionLogs { get; set; } = new List<DecisionLog>();
}
