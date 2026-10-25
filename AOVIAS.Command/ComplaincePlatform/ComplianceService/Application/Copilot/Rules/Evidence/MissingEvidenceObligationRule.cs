using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Evidence;

/// <summary>
/// Generates a recommendation for obligations with missing evidence.
/// Focused specifically on the EvidenceAdvisor persona — more granular than the accreditation-level rule.
/// Data source: <see cref="DTOs.EvidenceSummaryDto"/> from IEvidenceLibraryService.
/// </summary>
public sealed class MissingEvidenceObligationRule : RecommendationRuleBase
{
    private const int TriggerThreshold = 1;

    public override string RuleName => "evidence.missing_obligation_coverage";
    public override RecommendationCategory Category => RecommendationCategory.Evidence;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.EvidenceAdvisor, CopilotPersona.AuditorAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var evidence = context.EvidenceSummary;
        if (evidence is null || evidence.Missing < TriggerThreshold)
            return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var coveragePercent = evidence.Total > 0
            ? (decimal)(evidence.Total - evidence.Missing) / evidence.Total * 100m
            : 0m;

        var severity = evidence.Missing >= 15 ? RecommendationSeverity.Critical
                     : evidence.Missing >= 8  ? RecommendationSeverity.High
                     : evidence.Missing >= 3  ? RecommendationSeverity.Medium
                     : RecommendationSeverity.Low;

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["MissingEvidence"]   = evidence.Missing.ToString(),
                ["Total"]             = evidence.Total.ToString(),
                ["Approved"]          = evidence.Approved.ToString(),
                ["Rejected"]          = evidence.Rejected.ToString(),
                ["CoveragePercent"]   = $"{coveragePercent:F1}%",
            },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Provide evidence for {evidence.Missing} uncovered obligation{(evidence.Missing != 1 ? "s" : "")} ({coveragePercent:F0}% covered)",
                rationale: $"{evidence.Missing} obligations lack required evidence. Current coverage: {coveragePercent:F1}%.",
                expectedOutcome: "Evidence coverage improves, unblocking verification pipeline.",
                riskIfIgnored: "Uncovered obligations will fail stage unlock evidence criteria and may result in audit findings.",
                rationaleData: rationale,
                deepLink: "/compliance/evidence?filter=missing",
                expiresAtUtc: DateTime.UtcNow.AddDays(5)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
