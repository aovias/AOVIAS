namespace Gateway.DTOs;

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

// ── CMI ───────────────────────────────────────────────────────────────────────

/// <summary>
/// Compliance Maturity Index breakdown.
/// CMI = CoreScore×0.40 + CommitmentScore×0.30 + AchievementScore×0.20 + ExcellenceScore×0.10
/// </summary>
public class ComplianceMaturityIndexDto
{
    /// <summary>Weighted composite score 0–100.</summary>
    public decimal Value { get; set; }
    public decimal CoreScore { get; set; }
    public decimal CommitmentScore { get; set; }
    public decimal AchievementScore { get; set; }
    public decimal ExcellenceScore { get; set; }
}

// ── Stage ─────────────────────────────────────────────────────────────────────

public class MaturityStageDto
{
    public MaturityStage Stage { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public MaturityStageStatus Status { get; set; }
    public int GapCount { get; set; }
    public decimal UnlockThreshold { get; set; }
    public string UnlockMessage { get; set; } = string.Empty;
}

// ── Actual Gaps ───────────────────────────────────────────────────────────────

public class GapItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public GapSeverity Severity { get; set; }
    public string Chapter { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
    public GapItemStatus Status { get; set; }
}

public class GapSummaryDto
{
    public decimal ReadinessScore { get; set; }
    public int OpenGapCount { get; set; }
    public int CriticalCount { get; set; }
    public int MajorCount { get; set; }
    public int MinorCount { get; set; }
    public IReadOnlyList<GapItemDto> Items { get; set; } = Array.Empty<GapItemDto>();
}

// ── Predicted Gaps ────────────────────────────────────────────────────────────

public class PredictedGapItemDto
{
    public string Title { get; set; } = string.Empty;
    public string RiskCategory { get; set; } = string.Empty;
    public string Chapter { get; set; } = string.Empty;
}

public class PredictedGapSummaryDto
{
    public int PredictedGapCount { get; set; }
    public string UnlockCriteria { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public IReadOnlyList<PredictedGapItemDto> Items { get; set; } = Array.Empty<PredictedGapItemDto>();
}

// ── Journey Recommendations ───────────────────────────────────────────────────

public class JourneyRecommendationDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

// ── Top-level Dashboard ───────────────────────────────────────────────────────

public class ComplianceMaturityDashboardDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public Guid FrameworkId { get; set; }
    public string FrameworkName { get; set; } = string.Empty;
    public DateTime? AssessmentDate { get; set; }

    public ComplianceMaturityIndexDto ComplianceMaturityIndex { get; set; } = new();
    public MaturityStage CurrentStage { get; set; }
    public IReadOnlyList<MaturityStageDto> Stages { get; set; } = Array.Empty<MaturityStageDto>();

    public GapSummaryDto CoreSummary { get; set; } = new();
    public PredictedGapSummaryDto CommitmentSummary { get; set; } = new();
    public PredictedGapSummaryDto AchievementSummary { get; set; } = new();
    public PredictedGapSummaryDto ExcellenceSummary { get; set; } = new();

    public IReadOnlyList<JourneyRecommendationDto> NextActions { get; set; } = Array.Empty<JourneyRecommendationDto>();

    public SlaGovernanceSummaryDto? SlaGovernance { get; set; }

    public CapaGovernanceSummaryDto? CapaGovernance { get; set; }
}

public class CapaGovernanceSummaryDto
{
    public double CapaEffectivenessRate { get; set; }
    public int RecurringIssues { get; set; }
    public int PendingReviews { get; set; }
    public double CapaCompletionRate { get; set; }
}

public class SlaGovernanceSummaryDto
{
    public int OpenCriticalEscalations { get; set; }
    public int OverdueDeficiencies { get; set; }
    public double SlaHealthScore { get; set; }
    public IReadOnlyList<string> DepartmentsAtRisk { get; set; } = Array.Empty<string>();
}
