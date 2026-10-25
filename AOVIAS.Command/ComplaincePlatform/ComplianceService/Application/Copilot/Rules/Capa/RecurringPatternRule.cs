using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Capa;

/// <summary>
/// Generates a recommendation when the recurrence rate indicates a systemic unresolved root cause.
/// High recurrence rate shows that preventive actions are missing or ineffective.
/// Data source: <see cref="CapaMetricsDto.RecurrenceRate"/> and <see cref="CapaMetricsDto.RecurringIssues"/>.
/// </summary>
public sealed class RecurringPatternRule : RecommendationRuleBase
{
    private const double HighRecurrenceRate = 0.15;  // 15% recurrence rate triggers
    private const int MinimumRecurringCount = 2;

    public override string RuleName => "capa.recurring_pattern";
    public override RecommendationCategory Category => RecommendationCategory.CAPA;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.CapaAdvisor, CopilotPersona.QualityManagerAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var capa = context.CapaMetrics;
        if (capa is null || capa.RecurringIssues < MinimumRecurringCount)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        if (capa.RecurrenceRate < HighRecurrenceRate)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = capa.RecurrenceRate >= 0.30 ? RecommendationSeverity.High : RecommendationSeverity.Medium;

        var topCauses = capa.TopRootCauses.Take(3).Select(c => c.Label).ToList();
        var causesText = topCauses.Any() ? $" Top causes: {string.Join(", ", topCauses)}." : string.Empty;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["RecurringIssues"] = capa.RecurringIssues.ToString(),
                ["RecurrenceRate"]  = $"{capa.RecurrenceRate:P1}",
                ["OpenCapas"]       = capa.OpenCapas.ToString(),
            },
            thresholds: new() { ["HighRecurrenceRate"] = $"{HighRecurrenceRate:P0}" },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Investigate systemic root cause — {capa.RecurringIssues} recurring issue{(capa.RecurringIssues != 1 ? "s" : "")} detected",
                rationale: $"Recurrence rate is {capa.RecurrenceRate:P1} ({capa.RecurringIssues} recurring issues), indicating inadequate preventive actions.{causesText}",
                expectedOutcome: "Systemic root cause identified and preventive CAPA implemented, reducing recurrence rate.",
                riskIfIgnored: "High recurrence rate will be highlighted as a pattern of non-compliance during external audit.",
                rationaleData: rationale,
                sourceEntityType: "DeficiencyCapa",
                deepLink: "/compliance/deficiency-command-center?tab=rca",
                expiresAtUtc: DateTime.UtcNow.AddDays(7)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
