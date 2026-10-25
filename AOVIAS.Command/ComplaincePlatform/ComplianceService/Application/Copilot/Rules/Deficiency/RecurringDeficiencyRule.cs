using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Deficiency;

/// <summary>
/// Generates a recommendation when recurring deficiencies are detected.
/// Recurring deficiencies indicate systemic root causes that require CAPA review.
/// Data source: <see cref="DeficiencyMetricCountsDto.RecurringOpen"/>.
/// </summary>
public sealed class RecurringDeficiencyRule : RecommendationRuleBase
{
    private const int HighRecurrenceThreshold = 3;

    public override string RuleName => "deficiency.recurring_pattern";
    public override RecommendationCategory Category => RecommendationCategory.Deficiency;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.QualityManagerAssistant, CopilotPersona.CapaAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var metrics = context.DeficiencyMetrics;
        if (metrics is null || metrics.RecurringOpen <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = metrics.RecurringOpen >= HighRecurrenceThreshold
            ? RecommendationSeverity.High
            : RecommendationSeverity.Medium;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["RecurringOpen"]   = metrics.RecurringOpen.ToString(),
                ["TotalOpen"]       = metrics.Open.ToString(),
                ["RecurrenceRate"]  = metrics.RecurringOpen > 0 && metrics.Open > 0
                                      ? $"{(decimal)metrics.RecurringOpen / metrics.Open * 100:F1}%"
                                      : "0%",
            },
            thresholds: new() { ["HighRecurrenceThreshold"] = HighRecurrenceThreshold.ToString() },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Review {metrics.RecurringOpen} recurring deficiency pattern{(metrics.RecurringOpen != 1 ? "s" : "")} — systemic root cause suspected",
                rationale: $"{metrics.RecurringOpen} deficiencies are flagged as recurring, suggesting unresolved systemic root causes.",
                expectedOutcome: "Root cause investigation and stronger CAPA will prevent recurrence.",
                riskIfIgnored: "Recurring deficiencies compound compliance risk and signal ineffective corrective actions to auditors.",
                rationaleData: rationale,
                sourceEntityType: "ComplianceDeficiency",
                deepLink: "/compliance/deficiency-command-center?filter=recurring",
                expiresAtUtc: DateTime.UtcNow.AddDays(3)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
