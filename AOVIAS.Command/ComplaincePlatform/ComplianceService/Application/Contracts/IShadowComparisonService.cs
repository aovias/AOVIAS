using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IShadowComparisonService
{
    Task CompareAndLogAsync(
        string entityType,
        Guid entityId,
        string action,
        object oldPayload,
        object newPayload,
        Guid performedBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShadowMismatchDto>> GetRecentMismatchesAsync(
        int take,
        CancellationToken cancellationToken = default);
}
