namespace KnowledgeBase.Domain.Entities;

/// <summary>
/// Pre-materialised assessment questionnaire record for one ObjectiveElement.
/// Derived deterministically from the ComplianceNode hierarchy, RequirementDetails,
/// RequirementScores, and KnowledgeItems — one row per OE per Version.
/// </summary>
public class WizardQuestion
{
    public Guid Id { get; set; }

    // ── Framework context ──────────────────────────────────────────────────
    public Guid FrameworkId { get; set; }
    public string FrameworkVersion { get; set; } = string.Empty;

    // ── Hierarchy anchors (denormalised for fast filtering) ─────────────────
    public Guid ChapterId { get; set; }
    public Guid StandardId { get; set; }
    public Guid ObjectiveElementId { get; set; }

    // ── Question content ───────────────────────────────────────────────────
    /// <summary>{OE.Code}: {OE.Title}</summary>
    public string QuestionTitle { get; set; } = string.Empty;

    /// <summary>{OE.Title} — {Standard.RequirementDetail.Intent}</summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>KnowledgeItem.HowToComply + RequirementDetail.MeasurableElements</summary>
    public string HelpText { get; set; } = string.Empty;

    /// <summary>KnowledgeItem.AuditChecklist</summary>
    public string AssessmentGuidance { get; set; } = string.Empty;

    /// <summary>KnowledgeItem.EvidenceRequired</summary>
    public string ExpectedEvidence { get; set; } = string.Empty;

    // ── Classification ─────────────────────────────────────────────────────
    /// <summary>Governance taxonomy category mapped from chapter code (e.g. AAC → Patient Care).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>RequirementScore.Weight on the parent Standard.</summary>
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>High / Medium / Low derived from Weight thresholds.</summary>
    public string WeightBand { get; set; } = string.Empty;

    /// <summary>KnowledgeItem.RiskLevel</summary>
    public string RiskCategory { get; set; } = string.Empty;

    /// <summary>Global display order: ordered by chapter sequence then OE sequence.</summary>
    public int DisplayOrder { get; set; }

    // ── Maturity classification ────────────────────────────────────────────
    /// <summary>Core | Commitment | Achievement | Excellence</summary>
    public string AssessmentMaturityLevel { get; set; } = string.Empty;

    /// <summary>Onboarding | Full — which assessment phase this question belongs to.</summary>
    public string AssessmentPhase { get; set; } = string.Empty;

    /// <summary>True when a Non-Compliant answer triggers an automatic Critical Failure.</summary>
    public bool IsMandatory { get; set; } = false;

    /// <summary>True when the question must always surface in gap summary output.</summary>
    public bool IsCritical { get; set; } = false;

    /// <summary>Score % required to unlock the next maturity level (set on the first question of each level).</summary>
    public decimal? UnlockThreshold { get; set; }

    /// <summary>Sort order within a maturity level (lower = higher priority).</summary>
    public int DisplayPriority { get; set; } = 0;

    /// <summary>Global cross-maturity sequential order for progression reporting.</summary>
    public int AssessmentJourneyOrder { get; set; } = 0;

    // ── Versioning & audit ─────────────────────────────────────────────────
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ── Navigation ─────────────────────────────────────────────────────────
    public Framework Framework { get; set; } = null!;
    public ComplianceNode ObjectiveElement { get; set; } = null!;
    public ComplianceNode Chapter { get; set; } = null!;
    public ComplianceNode Standard { get; set; } = null!;
}
