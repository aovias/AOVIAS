using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Evidence;

/// <summary>
/// Generates a recommendation when evidence is queued for verification but not yet processed.
/// Primarily targets the AuditorAssistant persona whose workload bottleneck causes this.
/// Data source: <see cref="DTOs.EvidenceSummaryDto.PendingReview"/>.
/// </summary>
public sealed class PendingVerificationRule : RecommendationRuleBase
{
    private const int BottleneckThreshold = 5;

    public override string RuleName => "evidence.pending_verification_queue";
    public override RecommendationCategory Category => RecommendationCategory.Verification;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.AuditorAssistant, CopilotPersona.EvidenceAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var evidence = context.EvidenceSummary;
        if (evidence is null || evidence.PendingReview < BottleneckThreshold)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = evidence.PendingReview >= 15 ? RecommendationSeverity.High : RecommendationSeverity.Medium;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["PendingReview"]   = evidence.PendingReview.ToString(),
                ["Approved"]        = evidence.Approved.ToString(),
                ["Rejected"]        = evidence.Rejected.ToString(),
                ["Total"]           = evidence.Total.ToString(),
            },
            thresholds: new() { ["BottleneckThreshold"] = BottleneckThreshold.ToString() },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Process {evidence.PendingReview} pending evidence verification{(evidence.PendingReview != 1 ? "s" : "")} to unblock pipeline",
                rationale: $"{evidence.PendingReview} evidence items are awaiting verification, creating a bottleneck in the compliance pipeline.",
                expectedOutcome: "Verification backlog cleared, verified obligation percentage increases toward stage criteria.",
                riskIfIgnored: "Unverified evidence does not count toward accreditation metrics and delays stage unlock.",
                rationaleData: rationale,
                deepLink: "/compliance/evidence?filter=pending-review",
                expiresAtUtc: DateTime.UtcNow.AddDays(3)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
