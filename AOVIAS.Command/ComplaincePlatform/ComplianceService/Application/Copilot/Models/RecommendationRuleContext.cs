using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Models;

/// <summary>
/// Shared context passed to every recommendation rule.
/// Pre-fetched data is stored here so rules share data without N+1 queries.
/// The engine populates this once before running all rules in parallel.
/// </summary>
public sealed class RecommendationRuleContext
{
    public required AnalyticsScope Scope { get; init; }
    public CopilotContext? CopilotContext { get; init; }

    // ── Pre-fetched operational data ─────────────────────────────────────────
    // Set by RecommendationEngine before dispatching rules.

    /// <summary>Deficiency KPI counts for the scope.</summary>
    public DeficiencyMetricCountsDto? DeficiencyMetrics { get; set; }

    /// <summary>CAPA KPIs for the scope.</summary>
    public CapaMetricsDto? CapaMetrics { get; set; }

    /// <summary>SLA/Escalation KPIs for the scope.</summary>
    public SlaEscalationMetricsDto? SlaMetrics { get; set; }

    /// <summary>Analytics readiness snapshot (journey + framework scores).</summary>
    public AnalyticsReadinessDto? ReadinessMetrics { get; set; }

    /// <summary>Stage unlock evaluation results for all stages.</summary>
    public IReadOnlyList<StageUnlockEvaluationResult>? StageEvaluations { get; set; }

    /// <summary>Evidence library summary (missing, pending review counts).</summary>
    public EvidenceSummaryDto? EvidenceSummary { get; set; }

    /// <summary>Governance KPIs (CAPA effectiveness + SLA governance).</summary>
    public AnalyticsGovernanceDto? GovernanceMetrics { get; set; }
}
