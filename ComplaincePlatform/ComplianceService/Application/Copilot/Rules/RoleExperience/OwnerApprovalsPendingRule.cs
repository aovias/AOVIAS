using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 2 — Owner NBA: decisions / approvals waiting (reviews queue).</summary>
public sealed class OwnerApprovalsPendingRule : RecommendationRuleBase
{
    public override string RuleName => "role.owner_approvals_pending";
    public override RecommendationCategory Category => RecommendationCategory.Executive;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.ExecutiveAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var capaReviews = context.CapaMetrics?.PendingReviews ?? 0;
        var evidenceReviews = context.EvidenceSummary?.PendingReview ?? 0;
        var total = capaReviews + evidenceReviews;
        if (total <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["CapaPendingReviews"] = capaReviews.ToString(),
                ["EvidencePendingReviews"] = evidenceReviews.ToString(),
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: total >= 8 ? RecommendationSeverity.High : RecommendationSeverity.Medium,
            title: "Decisions needing approval",
            rationale: $"{capaReviews} CAPA effectiveness reviews and {evidenceReviews} evidence reviews await decision.",
            expectedOutcome: "Approval backlog clears and workstreams unblock.",
            riskIfIgnored: "Stalled approvals delay CAPA closure and evidence verification before audit.",
            rationaleData: rationale,
            deepLink: "/app/governance/approvals",
            expiresAtUtc: DateTime.UtcNow.AddDays(1)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
