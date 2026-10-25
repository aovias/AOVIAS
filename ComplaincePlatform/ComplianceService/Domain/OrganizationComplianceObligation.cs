using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Hospital-specific operational projection of an Objective Element.
///
/// Represents "how this organization is currently fulfilling a specific NABH requirement".
/// One record is created per (Hospital × Framework × ObjectiveElement) when a framework is enrolled.
///
/// The existing compliance hierarchy (Framework → Chapter → Standard → ObjectiveElement)
/// remains unchanged and is the authoritative source of compliance truth.
/// This entity tracks operational state only — it never duplicates compliance content.
/// </summary>
public class OrganizationComplianceObligation : BaseEntity
{
    // ─── Multi-tenant scoping (matches ComplianceJourney pattern) ───────────
    public Guid TenantId { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid HospitalId { get; set; }

    // ─── Compliance hierarchy references (IDs only — no content duplication) ─
    public Guid FrameworkId { get; set; }

    public Guid ChapterId { get; set; }

    public Guid StandardId { get; set; }

    public Guid ObjectiveElementId { get; set; }

    /// <summary>KnowledgeBase ComplianceNode reference. Nullable — cross-DB boundary, not enforced by FK.</summary>
    public Guid? ComplianceNodeId { get; set; }

    // ─── Denormalized display data (copied from ObjectiveElement at sync time) ─
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Classification band (Core / Commitment / Achievement / Excellence).</summary>
    public ObjectiveElementClassification MaturityLevel { get; set; }

    public string? RiskCategory { get; set; }

    public decimal Weight { get; set; } = 1.0m;

    // ─── Operational status ──────────────────────────────────────────────────
    public ImplementationStatus ImplementationStatus { get; set; } = ImplementationStatus.Unknown;

    public ObligationAssessmentStatus AssessmentStatus { get; set; } = ObligationAssessmentStatus.NotAssessed;

    public ObligationJourneyStatus JourneyStatus { get; set; } = ObligationJourneyStatus.NotStarted;

    public EvidenceStatus EvidenceStatus { get; set; } = EvidenceStatus.Missing;

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.NotVerified;

    // ─── Computed scores ─────────────────────────────────────────────────────
    /// <summary>0–100 readiness score derived from assessment and journey progress.</summary>
    public decimal ReadinessScore { get; set; }

    /// <summary>0–100 completion percentage of the obligation workflow.</summary>
    public decimal CompletionPercentage { get; set; }

    // ─── Activity timestamps ─────────────────────────────────────────────────
    public DateTime? LastAssessmentDate { get; set; }

    public DateTime? LastEvidenceDate { get; set; }

    public DateTime? LastVerificationDate { get; set; }

    public DateTime? LastSnapshotRefreshDate { get; set; }

    public DateTime UpdatedAt { get; set; }

    // ─── Audit ───────────────────────────────────────────────────────────────
    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    // ─── Navigation properties (no lazy loading) ─────────────────────────────
    public ComplianceFramework? Framework { get; set; }

    public ObjectiveElement? ObjectiveElement { get; set; }
}
