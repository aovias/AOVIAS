using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Analytics;

public interface IDeficiencyMetricsService
{
    Task<DeficiencyMetricCountsDto> GetCountsAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<IReadOnlyList<ComplianceDeficiency>> GetScopedOpenAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<int> CountOpenFindingsAsync(Guid organizationId, CancellationToken ct = default);

    Task<CapaMetricsDto> GetCapaMetricsAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<SlaEscalationMetricsDto> GetSlaMetricsAsync(AnalyticsScope scope, CancellationToken ct = default);
}

public interface IAnalyticsAggregationService
{
    Task<DeficiencyMetricCountsDto> GetDeficiencyCountsAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<CapaMetricsDto> GetCapaMetricsAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<SlaEscalationMetricsDto> GetSlaMetricsAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<AnalyticsReadinessDto> GetReadinessAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<AnalyticsGovernanceDto> GetGovernanceMetricsAsync(AnalyticsScope scope, CancellationToken ct = default);
}

public sealed class AnalyticsReadinessDto
{
    public decimal JourneyAccreditationReadiness { get; init; }
    public decimal FrameworkAssessmentReadiness { get; init; }
    public string JourneyReadinessSource { get; init; } = ReadinessMetricDefinitions.JourneyAccreditationReadiness;
    public string FrameworkReadinessSource { get; init; } = ReadinessMetricDefinitions.FrameworkAssessmentReadiness;
}

public sealed class AnalyticsGovernanceDto
{
    public CapaGovernanceSummaryDto Capa { get; init; } = new();
    public SlaGovernanceSummaryDto Sla { get; init; } = new();
}
