using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IDeficiencyCommandCenterQueryService
{
    Task<DeficiencyCommandCenterDashboardDto> GetDashboardAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default);

    Task<DeficiencyWorkspaceListDto> GetWorkspaceListAsync(
        Guid hospitalId,
        Guid frameworkId,
        string? status,
        string? severity,
        string? maturityLevel,
        string? search,
        Guid? objectiveElementId,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<DeficiencyWorkspaceDetailDto?> GetWorkspaceDetailAsync(
        Guid id,
        CancellationToken ct = default);
}
