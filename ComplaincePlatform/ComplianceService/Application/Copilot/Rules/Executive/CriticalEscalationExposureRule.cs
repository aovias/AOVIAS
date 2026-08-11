using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Executive;

/// <summary>
/// Generates a board-level recommendation when critical escalations are active.
/// Critical escalations represent the highest compliance risk exposure.
/// Data source: <see cref="SlaEscalationMetricsDto.CriticalEscalations"/>.
/// </summary>
public sealed class CriticalEscalationExposureRule : RecommendationRuleBase
{
    public override string RuleName => "executive.critical_escalation_exposure";
    public override RecommendationCategory Category => RecommendationCategory.Risk;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.ExecutiveAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var sla = context.SlaMetrics;
        if (sla is null || sla.CriticalEscalations <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = sla.CriticalEscalations >= 3
            ? RecommendationSeverity.Critical
            : RecommendationSeverity.High;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["CriticalEscalations"] = sla.CriticalEscalations.ToString(),
                ["TotalEscalated"]      = sla.EscalatedCount.ToString(),
                ["BreachedCount"]       = sla.BreachedCount.ToString(),
                ["EscalationRate"]      = $"{sla.EscalationRate:P1}",
            },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"{sla.CriticalEscalations} critical escalation{(sla.CriticalEscalations != 1 ? "s" : "")} require board-level response",
                rationale: $"{sla.CriticalEscalations} critical escalations are active. Total escalated: {sla.EscalatedCount}. Escalation rate: {sla.EscalationRate:P1}.",
                expectedOutcome: "Executive response plan documented, escalation acknowledged, resources allocated for resolution.",
                riskIfIgnored: "Unresolved critical escalations are the highest regulatory and reputational risk indicator.",
                rationaleData: rationale,
                sourceEntityType: "DeficiencyEscalation",
                deepLink: "/analytics/executive?focus=escalations",
                expiresAtUtc: DateTime.UtcNow.AddHours(24)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
