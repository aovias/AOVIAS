using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Models;

/// <summary>
/// Pre-fetched operational data passed to templates and content generators.
/// Follows the same batch-loading pattern as <see cref="RecommendationRuleContext"/>
/// to prevent N+1 queries during draft generation.
/// </summary>
public sealed class DraftGenerationContext
{
    public required DraftGenerationRequest Request { get; init; }

    // ── Source recommendation (when triggered from a recommendation) ─────────
    public CopilotRecommendation? SourceRecommendation { get; init; }

    // ── Analytics snapshots (actual service DTOs) ────────────────────────────
    public DeficiencyMetricCountsDto? DeficiencyMetrics { get; init; }
    public CapaMetricsDto? CapaMetrics { get; init; }
    public SlaEscalationMetricsDto? SlaMetrics { get; init; }
    public AnalyticsReadinessDto? ReadinessMetrics { get; init; }
    public AnalyticsGovernanceDto? GovernanceMetrics { get; init; }
    public ExecutiveAnalyticsDto? ExecutiveAnalytics { get; init; }

    // ── Stage / maturity data ────────────────────────────────────────────────
    public IReadOnlyList<StageEvaluationSummary>? StageEvaluations { get; init; }

    // ── Evidence data ────────────────────────────────────────────────────────
    public EvidenceSummaryContext? EvidenceSummary { get; init; }

    // ── Snapshot timestamp ───────────────────────────────────────────────────
    public DateTime SnapshotUtc { get; init; } = DateTime.UtcNow;
}

// ── Light summary types to avoid direct coupling to heavy infra DTOs ────────

/// <summary>Summary of stage evaluation data for draft generation.</summary>
public sealed class StageEvaluationSummary
{
    public string StageName { get; init; } = string.Empty;
    public decimal CompletionPercent { get; init; }
    public bool IsBlocked { get; init; }
    public int BlockingIssueCount { get; init; }
    public int MissingEvidenceCount { get; init; }
    public int PendingVerificationCount { get; init; }
}

/// <summary>Summary of evidence gaps for draft generation.</summary>
public sealed class EvidenceSummaryContext
{
    public int TotalObligations { get; init; }
    public int MissingEvidence { get; init; }
    public int PendingVerification { get; init; }
    public int VerifiedEvidence { get; init; }
    public IReadOnlyList<string> TopMissingObligations { get; init; } = [];
}
