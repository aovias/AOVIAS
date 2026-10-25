using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IFindingsWorkspaceQueryService
{
    Task<FindingsWorkspaceDashboardDto> GetDashboardAsync(Guid organizationId, CancellationToken ct = default);

    Task<FindingsWorkspaceListDto> GetWorkspaceListAsync(
        Guid organizationId,
        string? status,
        string? severity,
        string? classification,
        string? source,
        string? department,
        string? promotionReadiness,
        string? chapterCode,
        string? standardCode,
        Guid? objectiveElementId,
        string? quickFilter,
        string? search,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<FindingsWorkspaceDetailDto?> GetWorkspaceDetailAsync(Guid organizationId, Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<FindingComplianceContextOptionDto>> SearchComplianceContextAsync(
        string? search,
        string? chapterCode,
        string? standardCode,
        int limit,
        CancellationToken ct = default);
}
