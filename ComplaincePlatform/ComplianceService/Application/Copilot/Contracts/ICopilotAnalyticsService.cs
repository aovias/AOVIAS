using ComplianceService.Application.Copilot.DTOs;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>Sprint 13F — read-only Copilot observability and effectiveness analytics.</summary>
public interface ICopilotAnalyticsService
{
    Task<CopilotGovernanceDashboardDto> GetGovernanceDashboardAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<RecommendationEffectivenessMetricsDto> GetRecommendationMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<DraftEffectivenessMetricsDto> GetDraftMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<ProposalConversionMetricsDto> GetProposalMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<PersonaAdoptionMetricsDto> GetPersonaMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<CopilotActivityAnalyticsDto> GetActivityAnalyticsAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<CopilotTrendsDto> GetTrendsAsync(
        Guid organizationId,
        int days = 30,
        CancellationToken ct = default);

    Task<CopilotEffectivenessSnapshotDto> CaptureDailySnapshotAsync(
        Guid organizationId,
        CancellationToken ct = default);
}

public interface IRecommendationImpactAnalysisService
{
    Task<RecommendationImpactSummaryDto> AnalyzeAsync(
        Guid organizationId,
        int limit = 20,
        CancellationToken ct = default);
}
