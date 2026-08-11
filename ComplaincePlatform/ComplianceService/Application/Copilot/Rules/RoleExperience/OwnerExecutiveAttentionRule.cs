using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 1 stub — executive attention for hospital owner/admin.</summary>
public sealed class OwnerExecutiveAttentionRule : RecommendationRuleBase
{
    public override string RuleName => "role.owner_executive_attention";
    public override RecommendationCategory Category => RecommendationCategory.Executive;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.ExecutiveAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var critical = context.DeficiencyMetrics?.CriticalOpen ?? 0;
        var overdueCapas = context.CapaMetrics?.OverdueCapas ?? 0;
        if (critical <= 0 && overdueCapas <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = critical >= 3 || overdueCapas >= 5
            ? RecommendationSeverity.Critical
            : RecommendationSeverity.High;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["CriticalOpen"] = critical.ToString(),
                ["OverdueCapas"] = overdueCapas.ToString(),
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: severity,
            title: "Leadership attention: critical compliance exposure",
            rationale: $"{critical} critical open items and {overdueCapas} overdue CAPAs require executive review.",
            expectedOutcome: "Leadership prioritizes remediation and unblocks accreditation risk.",
            riskIfIgnored: "Board-level readiness and audit confidence continue to erode.",
            rationaleData: rationale,
            deepLink: "/app/control-center",
            expiresAtUtc: DateTime.UtcNow.AddDays(2)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
