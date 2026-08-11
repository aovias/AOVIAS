namespace ComplianceService.Application.DTOs;

// ── Enumerations ──────────────────────────────────────────────────────────────

public enum MaturityStage
{
    Core,
    Commitment,
    Achievement,
    Excellence
}

public enum MaturityStageStatus
{
    Completed,
    InProgress,
    Available,
    Locked
}

public enum GapSeverity
{
    Critical,
    Major,
    Minor
}

public enum GapItemStatus
{
    Open,
    InRemediation,
    Resolved
}

/// <summary>Platform-wide compliance item classification (Sprint 1 — H8).</summary>
public enum ComplianceItemClassification
{
    AuditFinding,
    ComplianceDeficiency,
    PredictedGap
}

// ── CMI ───────────────────────────────────────────────────────────────────────

/// <summary>
/// Compliance Maturity Index.
/// Formula: Value = CoreScore×0.40 + CommitmentScore×0.30 + AchievementScore×0.20 + ExcellenceScore×0.10
/// </summary>
public class ComplianceMaturityIndexDto
{
    public decimal Value { get; init; }
    public decimal CoreScore { get; init; }
    public decimal CommitmentScore { get; init; }
    public decimal AchievementScore { get; init; }
    public decimal ExcellenceScore { get; init; }
}

// ── Stage ─────────────────────────────────────────────────────────────────────

public class MaturityStageDto
{
    public MaturityStage Stage { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal Score { get; init; }
    public MaturityStageStatus Status { get; init; }
    public int GapCount { get; init; }
    public decimal UnlockThreshold { get; init; }
    public string UnlockMessage { get; init; } = string.Empty;
    public decimal ProgressPercent { get; init; }
    public decimal ReadinessPercent { get; init; }
    public decimal UnlockReadinessPercent { get; init; }
    public IReadOnlyList<string> BlockingReasons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
    public string? NextMilestone { get; init; }
}

// ── Actual Gaps ───────────────────────────────────────────────────────────────

public class GapItemDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public GapSeverity Severity { get; init; }
    public string Chapter { get; init; } = string.Empty;
    public string Standard { get; init; } = string.Empty;
    public GapItemStatus Status { get; init; }
    public ComplianceItemClassification Classification { get; init; } = ComplianceItemClassification.ComplianceDeficiency;
}

public class GapSummaryDto
{
    public decimal ReadinessScore { get; init; }
    public int OpenGapCount { get; init; }
    public int CriticalCount { get; init; }
    public int MajorCount { get; init; }
    public int MinorCount { get; init; }
    public IReadOnlyList<GapItemDto> Items { get; init; } = Array.Empty<GapItemDto>();
}

// ── Predicted Gaps ────────────────────────────────────────────────────────────

public class PredictedGapItemDto
{
    public string Title { get; init; } = string.Empty;
    public string RiskCategory { get; init; } = string.Empty;
    public string Chapter { get; init; } = string.Empty;
    public ComplianceItemClassification Classification { get; init; } = ComplianceItemClassification.PredictedGap;
}

public class PredictedGapSummaryDto
{
    public int PredictedGapCount { get; init; }
    public string UnlockCriteria { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
    public IReadOnlyList<PredictedGapItemDto> Items { get; init; } = Array.Empty<PredictedGapItemDto>();
}

// ── Journey Recommendations ───────────────────────────────────────────────────

public class JourneyRecommendationDto
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Priority { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}

// ── Governance (Sprint 3 — H1B) ───────────────────────────────────────────────

/// <summary>Structured blockers for the active or next locked stage.</summary>
public class StageBlockingSummaryDto
{
    public MaturityStage Stage { get; init; }
    public string StageLabel { get; init; } = string.Empty;
    public int OpenCriticalDeficiencies { get; init; }
    public int MissingEvidenceCount { get; init; }
    public int VerificationFailureCount { get; init; }
    public decimal ReadinessGapPercent { get; init; }
    public int RemainingObligationsCount { get; init; }
    public IReadOnlyList<string> BlockingReasons { get; init; } = Array.Empty<string>();
}

/// <summary>Deterministic accreditation readiness forecast (no AI).</summary>
public class AccreditationForecastDto
{
    public decimal CurrentReadinessPercent { get; init; }
    public decimal TargetReadinessPercent { get; init; }
    public decimal EstimatedUnlockReadinessPercent { get; init; }
    public decimal RemainingEffortPercent { get; init; }
    public decimal ComplianceMaturityIndex { get; init; }
}

/// <summary>Unlock governance timeline entry from persisted audit trail.</summary>
public class UnlockAuditTimelineEntryDto
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string EventType { get; init; } = string.Empty;
}

// ── Top-level Dashboard ───────────────────────────────────────────────────────

public class ComplianceMaturityDashboardDto
{
    public Guid OrganizationId { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public Guid FrameworkId { get; init; }
    public string FrameworkName { get; init; } = string.Empty;
    public DateTime? AssessmentDate { get; init; }

    public ComplianceMaturityIndexDto ComplianceMaturityIndex { get; init; } = new();
    public MaturityStage CurrentStage { get; init; }
    public IReadOnlyList<MaturityStageDto> Stages { get; init; } = Array.Empty<MaturityStageDto>();

    public GapSummaryDto CoreSummary { get; init; } = new();
    public PredictedGapSummaryDto CommitmentSummary { get; init; } = new();
    public PredictedGapSummaryDto AchievementSummary { get; init; } = new();
    public PredictedGapSummaryDto ExcellenceSummary { get; init; } = new();

    public IReadOnlyList<JourneyRecommendationDto> NextActions { get; init; } = Array.Empty<JourneyRecommendationDto>();

    public StageBlockingSummaryDto? ActiveStageBlocking { get; init; }
    public AccreditationForecastDto AccreditationForecast { get; init; } = new();
    public IReadOnlyList<UnlockAuditTimelineEntryDto> UnlockAuditTimeline { get; init; } = Array.Empty<UnlockAuditTimelineEntryDto>();

    /// <summary>Open deficiencies promoted from audit findings (Sprint 7B, read-only).</summary>
    public int AuditOriginDeficiencyCount { get; init; }

    /// <summary>Sprint 10 — governance SLA visibility (read-only).</summary>
    public SlaGovernanceSummaryDto SlaGovernance { get; init; } = new();

    /// <summary>Sprint 11 — governance CAPA visibility (read-only).</summary>
    public CapaGovernanceSummaryDto CapaGovernance { get; init; } = new();
}

public class CapaGovernanceSummaryDto
{
    public double CapaEffectivenessRate { get; init; }
    public int RecurringIssues { get; init; }
    public int PendingReviews { get; init; }
    public double CapaCompletionRate { get; init; }
}

public class SlaGovernanceSummaryDto
{
    public int OpenCriticalEscalations { get; init; }
    public int OverdueDeficiencies { get; init; }
    public double SlaHealthScore { get; init; }
    public IReadOnlyList<string> DepartmentsAtRisk { get; init; } = Array.Empty<string>();
}
