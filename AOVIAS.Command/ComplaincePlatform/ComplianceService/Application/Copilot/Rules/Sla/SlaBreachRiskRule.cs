using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Sla;

/// <summary>
/// Generates a recommendation when deficiencies are at-risk of or have already breached their SLA.
/// Data source: <see cref="SlaEscalationMetricsDto.AtRiskCount"/> and <see cref="SlaEscalationMetricsDto.BreachedCount"/>.
/// </summary>
public sealed class SlaBreachRiskRule : RecommendationRuleBase
{
    public override string RuleName => "sla.breach_risk";
    public override RecommendationCategory Category => RecommendationCategory.SLA;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.QualityManagerAssistant, CopilotPersona.CapaAdvisor,
         CopilotPersona.DepartmentHeadAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var sla = context.SlaMetrics;
        if (sla is null) return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var actionableCount = sla.AtRiskCount + sla.BreachedCount;
        if (actionableCount <= 0) return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = sla.BreachedCount >= 3 ? RecommendationSeverity.Critical
                     : sla.BreachedCount >= 1 ? RecommendationSeverity.High
                     : RecommendationSeverity.Medium;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["AtRiskCount"]     = sla.AtRiskCount.ToString(),
                ["BreachedCount"]   = sla.BreachedCount.ToString(),
                ["WarningCount"]    = sla.WarningCount.ToString(),
                ["TotalWithSla"]    = sla.TotalOpenWithSla.ToString(),
                ["BreachRate"]      = $"{sla.BreachRate:P1}",
            },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"{sla.BreachedCount} SLA breach{(sla.BreachedCount != 1 ? "es" : "")} + {sla.AtRiskCount} at-risk — immediate action required",
                rationale: $"{sla.BreachedCount} deficiencies have breached their SLA and {sla.AtRiskCount} are at-risk. Breach rate: {sla.BreachRate:P1}.",
                expectedOutcome: "At-risk deficiencies resolved before breach; breached items escalated with resolution plan.",
                riskIfIgnored: "SLA breaches trigger mandatory escalation and may attract regulatory attention.",
                rationaleData: rationale,
                sourceEntityType: "ComplianceDeficiency",
                deepLink: "/compliance/deficiency-command-center?filter=sla-at-risk",
                expiresAtUtc: DateTime.UtcNow.AddHours(48)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
