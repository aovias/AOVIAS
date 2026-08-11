namespace KnowledgeBase.Domain.Entities;

public class RequirementDetail
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public string Intent { get; set; } = string.Empty;
    public string MeasurableElements { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public string ApplicableDepartments { get; set; } = string.Empty;
    public bool RequiresDocumentation { get; set; }

    public ComplianceNode Node { get; set; } = null!;
}
