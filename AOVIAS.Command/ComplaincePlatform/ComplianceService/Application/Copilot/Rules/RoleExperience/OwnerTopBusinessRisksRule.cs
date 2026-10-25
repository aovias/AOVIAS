using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 2 — Owner NBA: top business risks (critical / blocking exposure).</summary>
public sealed class OwnerTopBusinessRisksRule : RecommendationRuleBase
{
    public override string RuleName => "role.owner_top_business_risks";
    public override RecommendationCategory Category => RecommendationCategory.Executive;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.ExecutiveAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var critical = context.DeficiencyMetrics?.CriticalOpen ?? 0;
        var blocking = context.DeficiencyMetrics?.BlockingOpen ?? 0;
        var escalated = context.DeficiencyMetrics?.CriticalEscalatedOpen ?? 0;
        if (critical <= 0 && blocking <= 0 && escalated <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = critical >= 3 || escalated >= 1
            ? RecommendationSeverity.Critical
            : RecommendationSeverity.High;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["CriticalOpen"] = critical.ToString(),
                ["BlockingOpen"] = blocking.ToString(),
                ["CriticalEscalatedOpen"] = escalated.ToString(),
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: severity,
            title: "Top business risks need executive review",
            rationale: $"{critical} critical, {blocking} blocking, and {escalated} critically escalated items threaten accreditation outcomes.",
            expectedOutcome: "Leadership prioritizes the highest-risk gaps and allocates remediation ownership.",
            riskIfIgnored: "Board-level readiness erodes and audit findings concentrate in unmanaged risk areas.",
            rationaleData: rationale,
            deepLink: "/app/ccc/risk-escalation",
            expiresAtUtc: DateTime.UtcNow.AddDays(2)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
