using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

public class Finding : BaseEntity
{
    public Guid TenantId { get; set; }
    public FindingScope Scope { get; set; } = FindingScope.Organization;

    public Guid? ComplianceJourneyId { get; set; }
    public ComplianceJourney? ComplianceJourney { get; set; }

    public Guid? ObjectiveElementId { get; set; }
    public ObjectiveElement? ObjectiveElement { get; set; }

    public Guid? TaskId { get; set; }
    public ComplianceTask? Task { get; set; }

    public ComplianceEvaluationStatus ComplianceStatus { get; set; } = ComplianceEvaluationStatus.NonCompliant;
    public bool IsMandatory { get; set; }
    public FindingRiskLevel RiskLevel { get; set; } = FindingRiskLevel.Medium;
    public string? Remarks { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public FindingActorType CreatedByType { get; set; } = FindingActorType.User;
    public decimal? ConfidenceScore { get; set; }

    // Legacy DTO compatibility fields (kept during migration window).
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ClauseReference { get; set; } = string.Empty;
    public FindingSeverity Severity { get; set; } = FindingSeverity.Minor;
    public FindingStatus Status { get; set; } = FindingStatus.Open;
    public FindingDepartment Department { get; set; } = FindingDepartment.Admin;
    /// <summary>
    /// How the finding was raised. <see cref="FindingRaisedBy"/> is numbered from 1, so the CLR
    /// default (0) is **not** a valid member — a caller that never assigns this would otherwise
    /// persist an out-of-range value that serialises to the bare string "0" in API responses.
    /// Coerced to <see cref="FindingRaisedBy.InternalAudit"/>, matching the field default and the
    /// <c>CK_Findings_RaisedBy_Range</c> database constraint.
    /// </summary>
    public FindingRaisedBy RaisedBy
    {
        get => _raisedBy;
        set => _raisedBy = Enum.IsDefined(value) ? value : FindingRaisedBy.InternalAudit;
    }

    private FindingRaisedBy _raisedBy = FindingRaisedBy.InternalAudit;
    public DateTime DateRaised { get; set; } = DateTime.UtcNow;
    public Guid? AssignedToUserId { get; set; }
    public FindingDepartment? AssignedDepartment { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? LastUpdatedBy { get; set; }
    public int EvidenceCount { get; set; }

    /// <summary>Human-readable finding number (e.g. FND-2026-00042).</summary>
    public string FindingNumber { get; set; } = string.Empty;

    /// <summary>Sprint 7A classification taxonomy.</summary>
    public FindingClassification? Classification { get; set; }

    /// <summary>Future Sprint 7B — set when promoted to ComplianceDeficiency.</summary>
    public Guid? PromotedDeficiencyId { get; set; }

    /// <summary>Sprint 14 — links finding to external auditor session.</summary>
    public Guid? AuditSessionId { get; set; }

    // Sprint 7A.1 — structured audit capture (all optional for backward compatibility).
    public DateTime? AuditDate { get; set; }
    public string? AuditorName { get; set; }
    public string? AuditCycle { get; set; }
    public string? Observation { get; set; }
    public string? Requirement { get; set; }
    public string? Impact { get; set; }
    public string? Recommendation { get; set; }

    // Legacy audit compatibility for existing external contract.
    public NonConformanceType? Type { get; set; }

    /// <summary>
    /// Records the latest promoted deficiency (multi-CAPA allowed — always updates to latest).
    /// </summary>
    public void MarkPromoted(Guid deficiencyId, Guid? updatedBy = null)
    {
        PromotedDeficiencyId = deficiencyId;
        LastUpdatedAt = DateTime.UtcNow;
        LastUpdatedBy = updatedBy;
    }

    /// <summary>Alias for <see cref="MarkPromoted"/> — links finding to latest CAPA deficiency.</summary>
    public void LinkPromotedDeficiency(Guid deficiencyId, Guid? updatedBy = null) =>
        MarkPromoted(deficiencyId, updatedBy);

    /// <summary>
    /// Classifies the finding against an objective element after creation (DEF-M3-001).
    /// Standard and chapter are not stored — they are resolved from the objective element's
    /// hierarchy wherever they are needed, so the objective element is the whole mapping.
    /// </summary>
    public void MapComplianceContext(Guid objectiveElementId, Guid? updatedBy = null)
    {
        ObjectiveElementId = objectiveElementId;
        LastUpdatedAt = DateTime.UtcNow;
        LastUpdatedBy = updatedBy;
    }

    public void TransitionStatus(FindingStatus newStatus, Guid? updatedBy = null)
    {
        Status = newStatus;
        LastUpdatedAt = DateTime.UtcNow;
        LastUpdatedBy = updatedBy;
    }
}
