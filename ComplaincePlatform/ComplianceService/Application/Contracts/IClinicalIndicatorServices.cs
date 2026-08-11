using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IClinicalIndicatorQueryService
{
    Task<QualityDashboardDto> GetDashboardAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct = default);
    Task<ClinicalIndicatorListDto> GetIndicatorsAsync(
        Guid organizationId, string? category, string? department, string? thresholdStatus,
        string? trend, string? search, string? sortBy, bool sortDesc,
        int page, int pageSize, CancellationToken ct = default);
    Task<ClinicalIndicatorDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DepartmentScorecardDto>> GetScorecardsAsync(Guid organizationId, CancellationToken ct = default);
    Task<QualityTrendsDto> GetTrendsAsync(Guid organizationId, string? period, Guid? indicatorId, CancellationToken ct = default);
    Task<QualityBenchmarkingDto> GetBenchmarkingAsync(Guid organizationId, string? comparisonPeriod, CancellationToken ct = default);
    Task<QualityExecutiveDto> GetExecutiveAsync(Guid organizationId, CancellationToken ct = default);
    Task<QualityAnalyticsDto> GetAnalyticsAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct = default);
}

public interface IClinicalIndicatorCommandService
{
    Task<ClinicalIndicatorDetailDto> PatchThresholdsAsync(
        Guid organizationId, Guid userId, PatchIndicatorThresholdsRequestDto request, CancellationToken ct = default);
    Task EnsureCatalogSeededAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct = default);
}
