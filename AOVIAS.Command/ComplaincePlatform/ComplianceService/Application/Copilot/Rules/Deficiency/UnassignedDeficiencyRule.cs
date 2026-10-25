using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Deficiency;

/// <summary>
/// Generates a recommendation when audit-origin deficiencies exist without assignment.
/// Audit-origin deficiencies are the highest-visibility items for external auditors.
/// Data source: <see cref="DeficiencyMetricCountsDto.AuditOriginOpen"/>.
/// </summary>
public sealed class UnassignedDeficiencyRule : RecommendationRuleBase
{
    public override string RuleName => "deficiency.audit_origin_unactioned";
    public override RecommendationCategory Category => RecommendationCategory.Deficiency;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.QualityManagerAssistant, CopilotPersona.AuditorAssistant,
         CopilotPersona.CapaAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var metrics = context.DeficiencyMetrics;
        if (metrics is null || metrics.AuditOriginOpen <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = metrics.AuditOriginOpen >= 3
            ? RecommendationSeverity.High
            : RecommendationSeverity.Medium;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["AuditOriginOpen"] = metrics.AuditOriginOpen.ToString(),
                ["TotalOpen"]       = metrics.Open.ToString(),
            },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Assign and action {metrics.AuditOriginOpen} audit-origin deficienc{(metrics.AuditOriginOpen != 1 ? "ies" : "y")}",
                rationale: $"{metrics.AuditOriginOpen} deficiencies originating from audit findings remain open and require assignment.",
                expectedOutcome: "All audit-origin deficiencies are assigned with corrective action timelines.",
                riskIfIgnored: "Unactioned audit findings are a significant adverse indicator during future external audits.",
                rationaleData: rationale,
                sourceEntityType: "ComplianceDeficiency",
                deepLink: "/compliance/deficiency-command-center?filter=audit",
                expiresAtUtc: DateTime.UtcNow.AddDays(3)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
