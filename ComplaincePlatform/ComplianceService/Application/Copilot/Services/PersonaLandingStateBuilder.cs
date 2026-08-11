using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>Sprint 13E — builds persona-specific landing headlines from existing SSOT data.</summary>
public sealed class PersonaLandingStateBuilder
{
    private readonly IAnalyticsAggregationService _analytics;
    private readonly IEvidenceLibraryService _evidenceLibrary;

    public PersonaLandingStateBuilder(
        IAnalyticsAggregationService analytics,
        IEvidenceLibraryService evidenceLibrary)
    {
        _analytics = analytics;
        _evidenceLibrary = evidenceLibrary;
    }

    public async Task<(string Headline, string Subheadline, string? Focus, decimal? Readiness, string? KpiLabel, string? KpiValue)>
        BuildAsync(
            CopilotPersona persona,
            AnalyticsScope scope,
            CopilotRecommendation? topRecommendation,
            CancellationToken ct)
    {
        var deficiency = await _analytics.GetDeficiencyCountsAsync(scope, ct);
        var capa       = await _analytics.GetCapaMetricsAsync(scope, ct);
        var readiness  = await _analytics.GetReadinessAsync(scope, ct);
        var sla        = await _analytics.GetSlaMetricsAsync(scope, ct);

        EvidenceSummaryDto? evidence = null;
        if (scope.HospitalId != Guid.Empty && scope.FrameworkId != Guid.Empty)
        {
            try
            {
                evidence = await _evidenceLibrary.GetSummaryAsync(scope.HospitalId, scope.FrameworkId, ct);
            }
            catch { /* evidence optional */ }
        }

        var readinessScore = readiness.JourneyAccreditationReadiness;
        var topTitle = topRecommendation?.Title;

        return persona switch
        {
            CopilotPersona.AccreditationCoach => (
                deficiency.CriticalOpen > 0
                    ? $"{deficiency.CriticalOpen} critical deficiencies are blocking readiness progress."
                    : $"Accreditation readiness is at {readinessScore:F0}%.",
                "Focus on stage unlocks, evidence gaps, and blocking deficiencies.",
                "Accreditation Readiness",
                readinessScore,
                "Critical Open",
                deficiency.CriticalOpen.ToString()),

            CopilotPersona.ExecutiveAdvisor => (
                $"Organization readiness is {readinessScore:F0}%.",
                sla.BreachedCount > 0
                    ? $"{sla.BreachedCount} SLA breaches require leadership attention."
                    : "Review governance, risk exposure, and department performance.",
                "Executive Overview",
                readinessScore,
                "SLA Breaches",
                sla.BreachedCount.ToString()),

            CopilotPersona.CapaAdvisor => (
                capa.OverdueCapas > 0
                    ? $"{capa.OverdueCapas} CAPAs are overdue."
                    : $"{capa.OpenCapas} open CAPAs need attention.",
                capa.PendingReviews > 0
                    ? $"{capa.PendingReviews} effectiveness reviews are pending."
                    : "Review deficiencies, recurring patterns, and remediation tasks.",
                "CAPA Health",
                null,
                "Overdue CAPAs",
                capa.OverdueCapas.ToString()),

            CopilotPersona.EvidenceAdvisor => (
                evidence?.Missing > 0
                    ? $"{evidence.Missing} obligations are missing evidence."
                    : "Evidence coverage looks stable — review verification queue.",
                evidence?.PendingReview > 0
                    ? $"{evidence.PendingReview} items await verification."
                    : "Map evidence to obligations and close verification gaps.",
                "Evidence Coverage",
                null,
                "Missing Evidence",
                evidence?.Missing.ToString() ?? "0"),

            CopilotPersona.AuditorAssistant => (
                deficiency.CriticalOpen > 0
                    ? $"{deficiency.CriticalOpen} critical items need audit attention."
                    : "Review open findings and verification backlog.",
                "Support audit preparation and finding documentation.",
                "Audit Workspace",
                null,
                "Open Critical",
                deficiency.CriticalOpen.ToString()),

            CopilotPersona.DepartmentHeadAssistant => (
                sla.WarningCount > 0
                    ? $"{sla.WarningCount} items are at SLA risk."
                    : "Department compliance tasks are on track.",
                "Prioritize team tasks and pending approvals.",
                "Department Operations",
                null,
                "SLA Warnings",
                sla.WarningCount.ToString()),

            _ => (
                topTitle ?? "Your compliance copilot is ready.",
                "Review prioritized recommendations and suggested actions.",
                "Quality Management",
                readinessScore,
                "Open Deficiencies",
                deficiency.Open.ToString()),
        };
    }
}
