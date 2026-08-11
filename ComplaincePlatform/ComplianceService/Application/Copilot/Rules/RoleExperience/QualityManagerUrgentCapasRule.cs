using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 3 — QM NBA: urgent / overdue CAPAs.</summary>
public sealed class QualityManagerUrgentCapasRule : RecommendationRuleBase
{
    public override string RuleName => "role.qm_urgent_capas";
    public override RecommendationCategory Category => RecommendationCategory.CAPA;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.QualityManagerAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var overdue = context.CapaMetrics?.OverdueCapas ?? 0;
        var open = context.CapaMetrics?.OpenCapas ?? 0;
        if (overdue <= 0 && open < 5)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["OverdueCapas"] = overdue.ToString(),
                ["OpenCapas"] = open.ToString(),
            },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: overdue >= 3 ? RecommendationSeverity.Critical : RecommendationSeverity.High,
            title: overdue > 0
                ? $"Urgent: {overdue} overdue CAPA{(overdue == 1 ? "" : "s")}"
                : $"{open} open CAPAs need QM triage",
            rationale: $"{overdue} overdue and {open} open CAPAs require quality-manager action in DCC.",
            expectedOutcome: "CAPA owners are chased and effectiveness reviews stay on track.",
            riskIfIgnored: "Overdue CAPAs become audit findings and stage-unlock blockers.",
            rationaleData: rationale,
            deepLink: "/app/audit/corrective-actions",
            expiresAtUtc: DateTime.UtcNow.AddDays(1)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
