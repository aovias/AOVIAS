using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Builds live dashboard snapshot DTOs (request-time aggregation). Used as fallback when projection is unavailable.
/// </summary>
public interface IDashboardSnapshotBuilder
{
    Task<DashboardSnapshotDto> BuildLiveAsync(
        Guid tenantId,
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken = default);
}
