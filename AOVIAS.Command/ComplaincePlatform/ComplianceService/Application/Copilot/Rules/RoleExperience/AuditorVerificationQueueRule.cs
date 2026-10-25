using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.RoleExperience;

/// <summary>Phase 3 Sprint 1 stub — auditor verification / evidence queue.</summary>
public sealed class AuditorVerificationQueueRule : RecommendationRuleBase
{
    public override string RuleName => "role.auditor_verification_queue";
    public override RecommendationCategory Category => RecommendationCategory.Verification;
    public override IReadOnlyList<CopilotPersona> TargetPersonas => [CopilotPersona.AuditorAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var pendingVerification = context.EvidenceSummary?.PendingReview ?? 0;

        if (pendingVerification <= 0)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var rationale = MakeRationale(
            RuleName,
            metrics: new() { ["PendingVerification"] = pendingVerification.ToString() },
            scope: context.Scope);

        results.AddRange(BuildForAllPersonas(
            severity: pendingVerification >= 10 ? RecommendationSeverity.High : RecommendationSeverity.Medium,
            title: "Auditor: clear verification and evidence review queue",
            rationale: $"{pendingVerification} items await verification before findings can close.",
            expectedOutcome: "Verification backlog clears and audit sessions stay on schedule.",
            riskIfIgnored: "Audit sessions stall on incomplete evidence trails.",
            rationaleData: rationale,
            deepLink: "/app/auditor/evidence-review",
            expiresAtUtc: DateTime.UtcNow.AddDays(2)));

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
