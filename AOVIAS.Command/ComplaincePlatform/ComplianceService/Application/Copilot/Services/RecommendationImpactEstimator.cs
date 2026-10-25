using System.Text.Json;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Estimates the impact of accepting a recommendation based on the operational data
/// in the rule context. Produces a <see cref="RecommendationImpact"/> record that is
/// serialized to JSON and stored on the <see cref="CopilotRecommendation"/> entity.
/// All values are estimates — not guarantees.
/// </summary>
public static class RecommendationImpactEstimator
{
    /// <summary>
    /// Estimates impact for a recommendation candidate based on the rule context.
    /// </summary>
    public static RecommendationImpact Estimate(
        RecommendationCandidate candidate,
        RecommendationRuleContext context)
    {
        return candidate.Category switch
        {
            RecommendationCategory.Deficiency =>
                EstimateDeficiencyImpact(candidate, context),

            RecommendationCategory.CAPA =>
                EstimateCapaImpact(candidate, context),

            RecommendationCategory.Accreditation =>
                EstimateAccreditationImpact(candidate, context),

            RecommendationCategory.Evidence or RecommendationCategory.Verification =>
                EstimateEvidenceImpact(candidate, context),

            RecommendationCategory.Executive or RecommendationCategory.Analytics =>
                EstimateExecutiveImpact(candidate, context),

            RecommendationCategory.Escalation =>
                EstimateSlaImpact(candidate, context),

            _ => RecommendationImpact.Zero,
        };
    }

    /// <summary>Serializes a <see cref="RecommendationImpact"/> to JSON string.</summary>
    public static string Serialize(RecommendationImpact impact) =>
        JsonSerializer.Serialize(impact, new JsonSerializerOptions { WriteIndented = false });

    // ── Category estimators ───────────────────────────────────────────────────

    private static RecommendationImpact EstimateDeficiencyImpact(
        RecommendationCandidate candidate,
        RecommendationRuleContext context)
    {
        var metrics = context.DeficiencyMetrics;
        if (metrics is null)
            return RecommendationImpact.Zero;

        var critical = metrics.CriticalOpen;
        var severity = candidate.Severity;

        return new RecommendationImpact
        {
            EstimatedDeficienciesReduced =
                severity == RecommendationSeverity.Critical ? Math.Min(critical, 3)
                : severity == RecommendationSeverity.High    ? Math.Min(critical, 2)
                : 1,

            EstimatedReadinessImpact =
                severity == RecommendationSeverity.Critical ? 5.0m
                : severity == RecommendationSeverity.High    ? 3.0m
                : 1.5m,

            EstimatedCmiImpact = 0.0m,
            EstimatedSlaBreachesPrevented = 0,
            EstimatedCapaImprovements = 0,
        };
    }

    private static RecommendationImpact EstimateCapaImpact(
        RecommendationCandidate candidate,
        RecommendationRuleContext context)
    {
        var metrics = context.CapaMetrics;
        if (metrics is null)
            return RecommendationImpact.Zero;

        return new RecommendationImpact
        {
            EstimatedCapaImprovements =
                candidate.Severity == RecommendationSeverity.Critical ? 3
                : candidate.Severity == RecommendationSeverity.High    ? 2
                : 1,

            EstimatedReadinessImpact = 2.5m,
            EstimatedCmiImpact       = 0.05m,
            EstimatedDeficienciesReduced = 0,
            EstimatedSlaBreachesPrevented = 0,
        };
    }

    private static RecommendationImpact EstimateAccreditationImpact(
        RecommendationCandidate candidate,
        RecommendationRuleContext context)
    {
        var readiness = context.ReadinessMetrics;
        var gap = readiness is not null
            ? Math.Max(0m, 80m - readiness.JourneyAccreditationReadiness)
            : 5m;

        return new RecommendationImpact
        {
            EstimatedReadinessImpact =
                candidate.Severity == RecommendationSeverity.Critical ? Math.Min(gap, 8m)
                : candidate.Severity == RecommendationSeverity.High    ? Math.Min(gap, 5m)
                : Math.Min(gap, 2.5m),

            EstimatedCmiImpact           = 0.1m,
            EstimatedDeficienciesReduced  = 0,
            EstimatedSlaBreachesPrevented = 0,
            EstimatedCapaImprovements    = 0,
        };
    }

    private static RecommendationImpact EstimateEvidenceImpact(
        RecommendationCandidate candidate,
        RecommendationRuleContext context)
    {
        return new RecommendationImpact
        {
            EstimatedReadinessImpact     = 3.0m,
            EstimatedDeficienciesReduced  = 1,
            EstimatedCmiImpact           = 0.0m,
            EstimatedSlaBreachesPrevented = 0,
            EstimatedCapaImprovements    = 0,
        };
    }

    private static RecommendationImpact EstimateSlaImpact(
        RecommendationCandidate candidate,
        RecommendationRuleContext context)
    {
        var sla = context.SlaMetrics;
        var breaches = sla?.BreachedCount ?? 0;

        return new RecommendationImpact
        {
            EstimatedSlaBreachesPrevented = Math.Max(1, breaches / 3),
            EstimatedReadinessImpact     = 1.5m,
            EstimatedCmiImpact           = 0.0m,
            EstimatedDeficienciesReduced  = 0,
            EstimatedCapaImprovements    = 0,
        };
    }

    private static RecommendationImpact EstimateExecutiveImpact(
        RecommendationCandidate candidate,
        RecommendationRuleContext context)
    {
        return new RecommendationImpact
        {
            EstimatedReadinessImpact     = 4.0m,
            EstimatedCmiImpact           = 0.1m,
            EstimatedDeficienciesReduced  = 2,
            EstimatedSlaBreachesPrevented = 1,
            EstimatedCapaImprovements    = 1,
        };
    }
}
