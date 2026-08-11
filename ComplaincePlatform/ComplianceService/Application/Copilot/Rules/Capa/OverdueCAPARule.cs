using ComplianceService.Application.Contracts;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Capa;

/// <summary>
/// Generates a recommendation when CAPA corrective actions are overdue.
/// Data source: <see cref="CapaMetricsDto.OverdueCapas"/>.
/// </summary>
public sealed class OverdueCAPARule : RecommendationRuleBase
{
    public override string RuleName => "capa.overdue_actions";
    public override RecommendationCategory Category => RecommendationCategory.CAPA;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.CapaAdvisor, CopilotPersona.QualityManagerAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var capa = context.CapaMetrics;
        if (capa is null || capa.OverdueCapas <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = capa.OverdueCapas >= 5 ? RecommendationSeverity.Critical
                     : capa.OverdueCapas >= 2 ? RecommendationSeverity.High
                     : RecommendationSeverity.Medium;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["OverdueCapas"]     = capa.OverdueCapas.ToString(),
                ["OpenCapas"]        = capa.OpenCapas.ToString(),
                ["PendingReviews"]   = capa.PendingReviews.ToString(),
                ["AverageClosureDays"] = $"{capa.AverageClosureDays:F1}",
            },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Complete {capa.OverdueCapas} overdue CAPA action{(capa.OverdueCapas != 1 ? "s" : "")} immediately",
                rationale: $"{capa.OverdueCapas} corrective actions are past their target completion date. Average closure takes {capa.AverageClosureDays:F1} days.",
                expectedOutcome: "Overdue CAPA actions are completed, effectiveness rates improve.",
                riskIfIgnored: "Overdue CAPAs indicate unresolved deficiency risk and will be flagged during audit review.",
                rationaleData: rationale,
                sourceEntityType: "DeficiencyCapa",
                deepLink: "/compliance/deficiency-command-center?tab=capa&filter=overdue",
                expiresAtUtc: DateTime.UtcNow.AddDays(1)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
