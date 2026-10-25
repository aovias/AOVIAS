namespace KnowledgeBase.Domain.Entities;

public class FrameworkClassification
{
    public Guid Id { get; set; }
    public Guid FrameworkId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public bool IsMandatory { get; set; }

    public Framework Framework { get; set; } = null!;
}
