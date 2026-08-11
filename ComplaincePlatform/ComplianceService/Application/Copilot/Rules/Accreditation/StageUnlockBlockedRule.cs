using ComplianceService.Application.Copilot.Models;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Copilot.Rules.Accreditation;

/// <summary>
/// Emits a recommendation for each maturity stage that is blocked or at-risk.
/// Data source: <see cref="IStageUnlockingService.GetStageEvaluationsAsync"/>.
/// </summary>
public sealed class StageUnlockBlockedRule : RecommendationRuleBase
{
    public override string RuleName => "accreditation.stage_unlock_blocked";
    public override RecommendationCategory Category => RecommendationCategory.Accreditation;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.AccreditationCoach, CopilotPersona.QualityManagerAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        if (context.StageEvaluations is null) return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        foreach (var eval in context.StageEvaluations)
        {
            if (eval.CriteriaMet) continue;
            if (eval.BlockingReasons.Count == 0 && eval.ProgressPercent >= 80m) continue;

            var severity = eval.BlockingReasons.Count > 0
                ? RecommendationSeverity.High
                : RecommendationSeverity.Medium;

            var blockingText = eval.BlockingReasons.Any()
                ? $" Blocking issues: {string.Join("; ", eval.BlockingReasons.Take(3))}."
                : string.Empty;

            var rationale = MakeRationale(
                RuleName,
                metrics: new()
                {
                    ["Stage"]           = eval.Stage.ToString(),
                    ["State"]           = eval.State.ToString(),
                    ["ProgressPercent"] = $"{eval.ProgressPercent:F1}%",
                    ["ReadinessPercent"]= $"{eval.ReadinessPercent:F1}%",
                    ["BlockingCount"]   = eval.BlockingReasons.Count.ToString(),
                },
                thresholds: new()
                {
                    ["RequiredReadiness"] = $"{StageUnlockCriteriaDefaults.ForStage(eval.Stage).MinimumReadinessPercent}%",
                },
                scope: context.Scope);

            foreach (var persona in TargetPersonas)
            {
                results.Add(Build(
                    persona: persona.ToString(),
                    severity: severity,
                    title: $"{eval.Stage} stage progression blocked — {eval.ProgressPercent:F0}% complete",
                    rationale: $"Stage {eval.Stage} is at {eval.ProgressPercent:F1}% progress (readiness: {eval.ReadinessPercent:F1}%).{blockingText}",
                    expectedOutcome: $"Resolve blocking issues to advance to {eval.Stage} stage.",
                    riskIfIgnored: "Accreditation milestone will be delayed.",
                    rationaleData: rationale,
                    deepLink: "/compliance/maturity",
                    expiresAtUtc: DateTime.UtcNow.AddDays(7)));
            }
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
