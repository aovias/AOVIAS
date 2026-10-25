using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IDashboardAppService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        Guid hospitalId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<HospitalDashboardDto> GetHospitalDashboardAsync(
        Guid hospitalId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<ComplianceTrendDto> GetComplianceTrendAsync(
        Guid hospitalId,
        Guid tenantId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}

