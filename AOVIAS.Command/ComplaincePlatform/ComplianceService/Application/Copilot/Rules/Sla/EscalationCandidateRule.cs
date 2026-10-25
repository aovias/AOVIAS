using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Sla;

/// <summary>
/// Generates a recommendation when deficiencies have been escalated, particularly critical escalations.
/// Escalated items require senior management awareness and expedited resolution.
/// Data source: <see cref="SlaEscalationMetricsDto.EscalatedCount"/> and <see cref="SlaEscalationMetricsDto.CriticalEscalations"/>.
/// </summary>
public sealed class EscalationCandidateRule : RecommendationRuleBase
{
    public override string RuleName => "sla.active_escalations";
    public override RecommendationCategory Category => RecommendationCategory.Escalation;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.ExecutiveAdvisor, CopilotPersona.QualityManagerAssistant,
         CopilotPersona.DepartmentHeadAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var sla = context.SlaMetrics;
        if (sla is null || sla.EscalatedCount <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = sla.CriticalEscalations >= 2 ? RecommendationSeverity.Critical
                     : sla.CriticalEscalations >= 1 ? RecommendationSeverity.High
                     : RecommendationSeverity.Medium;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["EscalatedCount"]       = sla.EscalatedCount.ToString(),
                ["CriticalEscalations"]  = sla.CriticalEscalations.ToString(),
                ["EscalationRate"]       = $"{sla.EscalationRate:P1}",
                ["AvgResolutionHours"]   = $"{sla.AverageResolutionHours:F0}h",
            },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"{sla.EscalatedCount} escalated deficienc{(sla.EscalatedCount != 1 ? "ies" : "y")} require senior management attention",
                rationale: $"{sla.EscalatedCount} deficiencies have been escalated ({sla.CriticalEscalations} critical). Escalation rate: {sla.EscalationRate:P1}.",
                expectedOutcome: "Escalated items acknowledged and resolution plans communicated to stakeholders.",
                riskIfIgnored: "Unacknowledged escalations may trigger external reporting obligations.",
                rationaleData: rationale,
                sourceEntityType: "DeficiencyEscalation",
                deepLink: "/compliance/deficiency-command-center?filter=escalated",
                expiresAtUtc: DateTime.UtcNow.AddDays(1)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
