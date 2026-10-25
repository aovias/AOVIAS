using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Sprint 9B — maps OE free-text department labels to normalized codes (non-breaking layer).</summary>
public class DepartmentLabelMapping : BaseEntity
{
    public string OeDepartmentLabel { get; set; } = string.Empty;
    public string NormalizedCode { get; set; } = string.Empty;
    public string DefaultResponsibleRole { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
