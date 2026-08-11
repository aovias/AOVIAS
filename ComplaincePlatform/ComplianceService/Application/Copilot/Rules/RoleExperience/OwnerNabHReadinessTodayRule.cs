using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 2 — Owner NBA: NABH / accreditation readiness snapshot for today.</summary>
public sealed class OwnerNabHReadinessTodayRule : RecommendationRuleBase
{
    private const decimal AttentionThreshold = 75m;

    public override string RuleName => "role.owner_nabh_readiness_today";
    public override RecommendationCategory Category => RecommendationCategory.Executive;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.ExecutiveAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var readiness = context.ReadinessMetrics;
        if (readiness is null)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var journey = readiness.JourneyAccreditationReadiness;
        // Always surface a concise readiness cue for Owner when below target, or a confirm cue near target.
        if (journey >= AttentionThreshold)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = journey < 55m
            ? RecommendationSeverity.Critical
            : journey < 65m
                ? RecommendationSeverity.High
                : RecommendationSeverity.Medium;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["JourneyReadiness"] = $"{journey:F1}%",
                ["FrameworkReadiness"] = $"{readiness.FrameworkAssessmentReadiness:F1}%",
                ["AttentionThreshold"] = $"{AttentionThreshold}%",
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: severity,
            title: $"NABH readiness today: {journey:F0}%",
            rationale: $"Accreditation readiness is {journey:F1}% (framework {readiness.FrameworkAssessmentReadiness:F1}%) — below the {AttentionThreshold}% executive target.",
            expectedOutcome: "Owner reviews readiness report and unblocks the largest readiness gaps.",
            riskIfIgnored: "Assessment window approaches with unresolved readiness debt.",
            rationaleData: rationale,
            deepLink: "/app/reports/readiness",
            expiresAtUtc: DateTime.UtcNow.AddDays(1)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
