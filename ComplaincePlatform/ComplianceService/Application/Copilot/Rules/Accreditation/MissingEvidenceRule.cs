using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Rules.Accreditation;

/// <summary>
/// Generates a recommendation when obligatory evidence is missing.
/// Data source: <see cref="DTOs.EvidenceSummaryDto.Missing"/> from IEvidenceLibraryService.
/// </summary>
public sealed class MissingEvidenceRule : RecommendationRuleBase
{
    private const int CriticalThreshold = 10;
    private const int HighThreshold = 5;

    public override string RuleName => "accreditation.missing_evidence";
    public override RecommendationCategory Category => RecommendationCategory.Evidence;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.AccreditationCoach, CopilotPersona.EvidenceAdvisor, CopilotPersona.QualityManagerAssistant];

    public override Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        var evidence = context.EvidenceSummary;
        if (evidence is null || evidence.Missing <= 0) return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);

        var severity = evidence.Missing >= CriticalThreshold
            ? RecommendationSeverity.Critical
            : evidence.Missing >= HighThreshold
                ? RecommendationSeverity.High
                : RecommendationSeverity.Medium;

        var stageMetrics = context.StageEvaluations?
            .Where(s => s.Metrics is not null)
            .Select(s => s.Metrics!)
            .OrderByDescending(m => m.MissingEvidenceCount)
            .FirstOrDefault();

        var rationale = MakeRationale(
            RuleName,
            metrics: new()
            {
                ["MissingEvidence"]  = evidence.Missing.ToString(),
                ["PendingReview"]    = evidence.PendingReview.ToString(),
                ["TotalEvidence"]    = evidence.Total.ToString(),
                ["ApprovedEvidence"] = evidence.Approved.ToString(),
            },
            thresholds: new()
            {
                ["CriticalThreshold"] = CriticalThreshold.ToString(),
                ["HighThreshold"]     = HighThreshold.ToString(),
            },
            scope: context.Scope);

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: severity,
                title: $"Upload {evidence.Missing} missing evidence item{(evidence.Missing != 1 ? "s" : "")} to advance accreditation",
                rationale: $"{evidence.Missing} obligations are missing required evidence. {evidence.PendingReview} items are pending review.",
                expectedOutcome: "Evidence coverage improves, unblocking stage progression criteria.",
                riskIfIgnored: "Missing evidence will prevent stage unlock and may lead to non-conformance findings during audit.",
                rationaleData: rationale,
                deepLink: "/compliance/evidence",
                expiresAtUtc: DateTime.UtcNow.AddDays(3)));
        }

        return Task.FromResult<IReadOnlyList<RecommendationCandidate>>(results);
    }
}
