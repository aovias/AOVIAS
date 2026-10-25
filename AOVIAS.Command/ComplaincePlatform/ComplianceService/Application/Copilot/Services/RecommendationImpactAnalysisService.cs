using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Sprint 13F — correlates recommendations → proposals → execution using existing entity links.
/// Read-only; does not modify recommendation or proposal engines.
/// </summary>
public sealed class RecommendationImpactAnalysisService : IRecommendationImpactAnalysisService
{
    private readonly ICopilotAnalyticsRepository _analytics;

    public RecommendationImpactAnalysisService(ICopilotAnalyticsRepository analytics) =>
        _analytics = analytics;

    public async Task<RecommendationImpactSummaryDto> AnalyzeAsync(
        Guid organizationId,
        int limit = 20,
        CancellationToken ct = default)
    {
        var rows = await _analytics.GetRecommendationImpactRowsAsync(organizationId, limit, ct);
        var withProposal = rows.Count(r => r.HasProposal);
        var withExecution = rows.Count(r => r.ProposalExecuted);

        return new RecommendationImpactSummaryDto
        {
            RecommendationsWithProposal   = withProposal,
            RecommendationsWithExecution  = withExecution,
            ExecutionConversionRate       = withProposal == 0 ? 0 :
                Math.Round((double)withExecution / withProposal * 100, 1),
            TopImpacts = rows.Select(r => new RecommendationImpactDto
            {
                RecommendationId = r.RecommendationId,
                Title            = r.Title,
                Category         = r.Category,
                Persona          = r.Persona,
                HasProposal      = r.HasProposal,
                ProposalExecuted = r.ProposalExecuted,
                ResultEntityType = r.ResultEntityType,
                EstimatedImpact  = TruncateImpact(r.ImpactJson),
                ActualOutcome    = r.ExecutionNotes,
            }).ToList(),
        };
    }

    private static string? TruncateImpact(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return json.Length <= 200 ? json : json[..200] + "…";
    }
}
