using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Capa;

/// <summary>
/// Generates a recommendation when a significant proportion of CAPAs are rated ineffective.
/// Low effectiveness rate suggests root causes are not being properly addressed.
/// Data source: <see cref="CapaMetricsDto.IneffectiveCapas"/> and <see cref="CapaMetricsDto.CapaEffectivenessRate"/>.
/// </summary>
public sealed class IneffectiveCAPARule : RecommendationRuleBase
{
    private const double IneffectiveThresholdRate = 0.20;  // 20% ineffective triggers recommendation
    private const int MinimumSampleSize = 3;

    public override string RuleName => "capa.ineffective_actions";
    public override RecommendationCategory Category => RecommendationCategory.CAPA;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.CapaAdvisor, CopilotPersona.QualityManagerAssistant, CopilotPersona.ExecutiveAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var capa = context.CapaMetrics;
        if (capa is null || capa.IneffectiveCapas <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        // Only trigger if we have enough sample size
        var totalReviewed = capa.EffectiveCapas + capa.IneffectiveCapas;
        if (totalReviewed < MinimumSampleSize)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var ineffectiveRate = totalReviewed > 0 ? (double)capa.IneffectiveCapas / totalReviewed : 0;
        if (ineffectiveRate < IneffectiveThresholdRate)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = ineffectiveRate >= 0.40 ? RecommendationSeverity.High : RecommendationSeverity.Medium;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["IneffectiveCapas"]    = capa.IneffectiveCapas.ToString(),
                ["EffectiveCapas"]      = capa.EffectiveCapas.ToString(),
                ["EffectivenessRate"]   = $"{capa.CapaEffectivenessRate:P1}",
                ["IneffectiveRate"]     = $"{ineffectiveRate:P1}",
                ["RecurringIssues"]     = capa.RecurringIssues.ToString(),
            },
            thresholds: new() { ["IneffectiveThreshold"] = $"{IneffectiveThresholdRate:P0}" },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"CAPA effectiveness at {capa.CapaEffectivenessRate:P0} — {capa.IneffectiveCapas} actions need rework",
                rationale: $"{capa.IneffectiveCapas} CAPA reviews were rated ineffective ({ineffectiveRate:P0} ineffective rate), suggesting root causes are not being addressed.",
                expectedOutcome: "Revised corrective actions address root causes, improving the effectiveness rate.",
                riskIfIgnored: "Ineffective CAPAs allow deficiencies to recur, compounding compliance debt.",
                rationaleData: rationale,
                sourceEntityType: "DeficiencyCapa",
                deepLink: "/compliance/deficiency-command-center?tab=capa&filter=ineffective",
                expiresAtUtc: DateTime.UtcNow.AddDays(5)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
