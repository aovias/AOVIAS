using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Analytics;

/// <summary>
/// Read-only analytics query orchestrator (Sprint 12B-1).
/// Delegates KPI computation to SSOT services — no duplicate formulas.
/// </summary>
public interface IAnalyticsQueryService
{
    Task<AnalyticsOverviewDto> GetOverviewAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<ExecutiveAnalyticsDto> GetExecutiveAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<OperationalAnalyticsDto> GetOperationalAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<RcaAnalyticsDto> GetRcaAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<CapaAnalyticsDto> GetCapaAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<SlaAnalyticsDto> GetSlaAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<DepartmentAnalyticsDto> GetDepartmentsAsync(AnalyticsScope scope, CancellationToken ct = default);

    Task<AnalyticsTrendsDto> GetTrendsAsync(
        AnalyticsScope scope,
        AnalyticsTrendQueryDto query,
        CancellationToken ct = default);

    Task<AnalyticsScope> ResolveScopeAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid? frameworkId,
        CancellationToken ct = default);
}

public interface IAnalyticsSnapshotService
{
    Task GenerateDailySnapshotAsync(AnalyticsScope scope, DateOnly? date = null, CancellationToken ct = default);

    Task GenerateAllOrganizationsDailyAsync(DateOnly? date = null, CancellationToken ct = default);

    Task<IReadOnlyList<Domain.AnalyticsDailySnapshot>> GetDailySnapshotsAsync(
        AnalyticsScope scope,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);
}
