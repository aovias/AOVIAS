namespace ComplianceService.Domain.Copilot.Enums;

/// <summary>
/// Classifies the type of AI-generated compliance draft.
/// Each value maps to a corresponding template in the draft template registry.
/// </summary>
public enum DraftType
{
    /// <summary>Corrective and Preventive Action proposal draft.</summary>
    CapaDraft = 0,

    /// <summary>Leadership-facing compliance briefing with risk, readiness, and escalation summary.</summary>
    ExecutiveComplianceBriefing = 1,

    /// <summary>Current accreditation readiness status with stage progress and blocking issues.</summary>
    ReadinessSummary = 2,

    /// <summary>Checklist of required evidence items mapped to obligations and verification status.</summary>
    EvidenceChecklist = 3,

    /// <summary>Preparation brief for upcoming regulatory or accreditation audit.</summary>
    AuditPreparationBrief = 4,

    /// <summary>Management review summary for governance and quality oversight meetings.</summary>
    ManagementReviewSummary = 5,
}
