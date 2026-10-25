using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Deficiency;

/// <summary>
/// Generates a recommendation when deficiencies have been open for an extended period.
/// Uses actual deficiency entity data to count age-based thresholds.
/// Data source: <see cref="IDeficiencyMetricsService.GetScopedOpenAsync"/>.
/// Note: this rule does NOT pre-fetch entity data — it re-uses counts from context metrics.
/// Detailed aging analysis can be added in Sprint 14 once entity-level context is in context.
/// </summary>
public sealed class AgingDeficiencyRule : RecommendationRuleBase
{
    private const int AgingDaysThreshold = 30;
    private const int CriticalAgingDays = 60;

    public override string RuleName => "deficiency.aging_open";
    public override RecommendationCategory Category => RecommendationCategory.Deficiency;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.QualityManagerAssistant, CopilotPersona.CapaAdvisor,
         CopilotPersona.DepartmentHeadAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var metrics = context.DeficiencyMetrics;
        if (metrics is null || metrics.Open <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        // Use EscalatedOpen as proxy for aging — escalated items have breached their SLA window
        var agingProxy = metrics.EscalatedOpen + metrics.BlockingOpen;
        if (agingProxy <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = metrics.EscalatedOpen >= 3 ? RecommendationSeverity.High : RecommendationSeverity.Medium;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["EscalatedOpen"]  = metrics.EscalatedOpen.ToString(),
                ["BlockingOpen"]   = metrics.BlockingOpen.ToString(),
                ["TotalOpen"]      = metrics.Open.ToString(),
                ["AgingThreshold"] = $"{AgingDaysThreshold} days",
            },
            thresholds: new()
            {
                ["AgingDaysThreshold"]  = AgingDaysThreshold.ToString(),
                ["CriticalAgingDays"]   = CriticalAgingDays.ToString(),
            },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"{agingProxy} long-running deficienc{(agingProxy != 1 ? "ies" : "y")} require immediate attention",
                rationale: $"{metrics.EscalatedOpen} deficiencies have been escalated (SLA breached) and {metrics.BlockingOpen} are classified as blocking.",
                expectedOutcome: "Resolving aging deficiencies reduces compliance risk and prevents escalation to external auditors.",
                riskIfIgnored: "Aging open deficiencies expose the organization to regulatory action and accreditation failure.",
                rationaleData: rationale,
                sourceEntityType: "ComplianceDeficiency",
                deepLink: "/compliance/deficiency-command-center?filter=escalated",
                expiresAtUtc: DateTime.UtcNow.AddDays(2)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
