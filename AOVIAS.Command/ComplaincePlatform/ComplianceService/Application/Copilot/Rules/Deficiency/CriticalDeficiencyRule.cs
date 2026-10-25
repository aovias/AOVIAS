using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Deficiency;

/// <summary>
/// Generates an urgent recommendation when critical deficiencies are open.
/// Critical deficiencies directly block stage unlock (MaxOpenCriticalDeficiencies = 0).
/// Data source: <see cref="DeficiencyMetricCountsDto.CriticalOpen"/>.
/// </summary>
public sealed class CriticalDeficiencyRule : RecommendationRuleBase
{
    public override string RuleName => "deficiency.critical_open";
    public override RecommendationCategory Category => RecommendationCategory.Deficiency;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.QualityManagerAssistant, CopilotPersona.CapaAdvisor,
         CopilotPersona.AccreditationCoach, CopilotPersona.ExecutiveAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var metrics = context.DeficiencyMetrics;
        if (metrics is null || metrics.CriticalOpen <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = metrics.CriticalOpen >= 5 ? RecommendationSeverity.Critical : RecommendationSeverity.High;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["CriticalOpen"]         = metrics.CriticalOpen.ToString(),
                ["TotalOpen"]            = metrics.Open.ToString(),
                ["BlockingOpen"]         = metrics.BlockingOpen.ToString(),
                ["CriticalEscalatedOpen"]= metrics.CriticalEscalatedOpen.ToString(),
            },
            thresholds: new() { ["MaxAllowed"] = "0" },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Resolve {metrics.CriticalOpen} critical deficiency{(metrics.CriticalOpen != 1 ? " findings" : "")} blocking accreditation",
                rationale: $"{metrics.CriticalOpen} critical deficiencies are open. Zero critical deficiencies are allowed for stage unlock.",
                expectedOutcome: "Resolving critical deficiencies unblocks stage progression and reduces compliance risk.",
                riskIfIgnored: "Open critical deficiencies will block all stage unlock evaluations and trigger mandatory escalation.",
                rationaleData: rationale,
                sourceEntityType: "ComplianceDeficiency",
                deepLink: "/compliance/deficiency-command-center?filter=critical",
                expiresAtUtc: DateTime.UtcNow.AddHours(24)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
