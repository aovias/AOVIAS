using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Executive;

/// <summary>
/// Generates an executive-level recommendation when accreditation readiness is critically low.
/// Targets the ExecutiveAdvisor persona for board-level awareness.
/// Data source: <see cref="AnalyticsReadinessDto.JourneyAccreditationReadiness"/>.
/// </summary>
public sealed class ReadinessDeclineRule : RecommendationRuleBase
{
    private const decimal CriticalReadinessThreshold = 55m;
    private const decimal HighReadinessThreshold = 65m;

    public override string RuleName => "executive.readiness_concern";
    public override RecommendationCategory Category => RecommendationCategory.Executive;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.ExecutiveAdvisor, CopilotPersona.DepartmentHeadAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var readiness = context.ReadinessMetrics;
        if (readiness is null) return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var r = readiness.JourneyAccreditationReadiness;
        if (r >= HighReadinessThreshold) return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = r < CriticalReadinessThreshold
            ? RecommendationSeverity.Critical
            : RecommendationSeverity.High;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["JourneyReadiness"]   = $"{r:F1}%",
                ["FrameworkReadiness"] = $"{readiness.FrameworkAssessmentReadiness:F1}%",
            },
            thresholds: new()
            {
                ["CriticalThreshold"] = $"{CriticalReadinessThreshold}%",
                ["HighThreshold"]     = $"{HighReadinessThreshold}%",
            },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Executive attention required — accreditation readiness at {r:F0}%",
                rationale: $"Organizational readiness is {r:F1}%, which is below the executive attention threshold of {HighReadinessThreshold}%.",
                expectedOutcome: "Executive oversight drives resource allocation to close readiness gap before accreditation assessment.",
                riskIfIgnored: "Low organizational readiness increases the risk of failed accreditation assessment.",
                rationaleData: rationale,
                deepLink: "/analytics/executive",
                expiresAtUtc: DateTime.UtcNow.AddDays(7)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
