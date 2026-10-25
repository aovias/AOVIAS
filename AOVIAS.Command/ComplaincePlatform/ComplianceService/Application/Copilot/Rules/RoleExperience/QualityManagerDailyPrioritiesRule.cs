using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 1 stub — QM daily priority cue from pending reviews / open work.</summary>
public sealed class QualityManagerDailyPrioritiesRule : RecommendationRuleBase
{
    public override string RuleName => "role.qm_daily_priorities";
    public override RecommendationCategory Category => RecommendationCategory.Task;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.QualityManagerAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var pendingReviews = context.CapaMetrics?.PendingReviews ?? 0;
        var open = context.DeficiencyMetrics?.Open ?? 0;
        if (pendingReviews <= 0 && open <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["PendingReviews"] = pendingReviews.ToString(),
                ["OpenDeficiencies"] = open.ToString(),
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: pendingReviews >= 5 ? RecommendationSeverity.High : RecommendationSeverity.Medium,
            title: "Quality Manager: clear today's review and deficiency queue",
            rationale: $"{pendingReviews} pending CAPA reviews and {open} open deficiencies need QM triage.",
            expectedOutcome: "Review backlog shrinks and SLA risk stabilizes.",
            riskIfIgnored: "Effectiveness reviews age out and accreditation blockers accumulate.",
            rationaleData: rationale,
            deepLink: "/app/audit/corrective-actions",
            expiresAtUtc: DateTime.UtcNow.AddDays(1)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
