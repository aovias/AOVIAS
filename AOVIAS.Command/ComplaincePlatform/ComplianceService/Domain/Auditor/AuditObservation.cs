using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain.Auditor;

/// <summary>
/// Non-remediation observation captured during an audit session.
/// Never creates deficiencies.
/// </summary>
public class AuditObservation : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid AuditSessionId { get; set; }
    public AuditSession? AuditSession { get; set; }

    public string ObservationNumber { get; set; } = string.Empty;
    public ObservationType Type { get; set; } = ObservationType.GeneralComment;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid? ObjectiveElementId { get; set; }
    public ObjectiveElement? ObjectiveElement { get; set; }
    public Guid? ChapterId { get; set; }

    /// <summary>Checklist chapter code (e.g. ACC) when OE/chapter GUIDs are unavailable.</summary>
    public string? ChapterCode { get; set; }

    /// <summary>Checklist OE code (e.g. ACC.1) for checklist-keyed observations.</summary>
    public string? OeCode { get; set; }

    public FindingDepartment? Department { get; set; }

    public Guid CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
