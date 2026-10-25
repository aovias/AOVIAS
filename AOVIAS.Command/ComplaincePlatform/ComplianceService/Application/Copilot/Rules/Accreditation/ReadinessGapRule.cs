using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Accreditation;

/// <summary>
/// Generates a recommendation when overall accreditation readiness falls below safe thresholds.
/// Data source: <see cref="AnalyticsReadinessDto"/>.
/// </summary>
public sealed class ReadinessGapRule : RecommendationRuleBase
{
    private const decimal CriticalReadiness = 50m;
    private const decimal HighReadiness = 65m;
    private const decimal MediumReadiness = 75m;

    public override string RuleName => "accreditation.readiness_gap";
    public override RecommendationCategory Category => RecommendationCategory.Accreditation;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.AccreditationCoach, CopilotPersona.QualityManagerAssistant,
         CopilotPersona.ExecutiveAdvisor, CopilotPersona.DepartmentHeadAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var readiness = context.ReadinessMetrics;
        if (readiness is null) return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var journeyReadiness = readiness.JourneyAccreditationReadiness;
        if (journeyReadiness >= MediumReadiness) return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = journeyReadiness < CriticalReadiness
            ? RecommendationSeverity.Critical
            : journeyReadiness < HighReadiness
                ? RecommendationSeverity.High
                : RecommendationSeverity.Medium;

        var gap = MediumReadiness - journeyReadiness;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["JourneyReadiness"]    = $"{journeyReadiness:F1}%",
                ["FrameworkReadiness"]  = $"{readiness.FrameworkAssessmentReadiness:F1}%",
                ["ReadinessGap"]        = $"{gap:F1}%",
            },
            thresholds: new()
            {
                ["CriticalThreshold"] = $"{CriticalReadiness}%",
                ["TargetReadiness"]   = $"{MediumReadiness}%",
            },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Accreditation readiness at {journeyReadiness:F0}% — {gap:F0}% below target",
                rationale: $"Overall readiness is {journeyReadiness:F1}% against the {MediumReadiness}% target. Framework assessment readiness is {readiness.FrameworkAssessmentReadiness:F1}%.",
                expectedOutcome: $"Closing readiness gap to above {MediumReadiness}% will meet stage unlock threshold.",
                riskIfIgnored: "Low readiness may result in failed stage evaluations and delayed accreditation.",
                rationaleData: rationale,
                deepLink: "/analytics/readiness",
                expiresAtUtc: DateTime.UtcNow.AddDays(7)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
