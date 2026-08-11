using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IFindingPromotionService
{
    Task<FindingPromotionEligibilityDto> GetEligibilityAsync(
        Guid organizationId,
        Guid findingId,
        CancellationToken ct = default);

    Task<FindingPromotionStatusDto> GetPromotionStatusAsync(
        Guid organizationId,
        Guid findingId,
        Guid hospitalId,
        CancellationToken ct = default);

    Task<PromoteFindingResultDto> PromoteAsync(
        Guid organizationId,
        Guid findingId,
        Guid hospitalId,
        Guid? userId,
        CancellationToken ct = default,
        bool allowAdditional = true);
}
