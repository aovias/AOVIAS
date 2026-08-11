using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IFindingQueryService
{
    Task<IReadOnlyList<FindingListItemDto>> GetFindingsAsync(
        Guid organizationId,
        ManagedFindingListFilters filters,
        CancellationToken cancellationToken = default);

    Task<FindingDetailDto?> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FindingDetailDto> CreateAsync(
        Guid organizationId,
        Guid userId,
        CreateManagedFindingRequest request,
        CancellationToken cancellationToken = default);
}
