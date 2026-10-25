using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Accreditation;

/// <summary>
/// Generates a recommendation when there is a significant evidence verification backlog.
/// Data source: <see cref="DTOs.EvidenceSummaryDto.PendingReview"/>.
/// </summary>
public sealed class VerificationBacklogRule : RecommendationRuleBase
{
    private const int HighBacklogThreshold = 8;
    private const int MediumBacklogThreshold = 3;

    public override string RuleName => "accreditation.verification_backlog";
    public override RecommendationCategory Category => RecommendationCategory.Verification;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.AccreditationCoach, CopilotPersona.AuditorAssistant, CopilotPersona.EvidenceAdvisor];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var evidence = context.EvidenceSummary;
        if (evidence is null || evidence.PendingReview < MediumBacklogThreshold)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = evidence.PendingReview >= HighBacklogThreshold
            ? RecommendationSeverity.High
            : RecommendationSeverity.Medium;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["PendingVerification"] = evidence.PendingReview.ToString(),
                ["Rejected"]            = evidence.Rejected.ToString(),
                ["Total"]               = evidence.Total.ToString(),
            },
            thresholds: new() { ["HighBacklogThreshold"] = HighBacklogThreshold.ToString() },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Clear {evidence.PendingReview} pending evidence verification request{(evidence.PendingReview != 1 ? "s" : "")}",
                rationale: $"{evidence.PendingReview} evidence items are awaiting auditor verification, creating a bottleneck in the accreditation pipeline.",
                expectedOutcome: "Verification bottleneck cleared, verified obligation percentage increases.",
                riskIfIgnored: "Pending evidence does not count toward stage unlock criteria and will delay accreditation.",
                rationaleData: rationale,
                deepLink: "/compliance/evidence?filter=pending",
                expiresAtUtc: DateTime.UtcNow.AddDays(5)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
