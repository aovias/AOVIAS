using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IRiskRegisterAppService
{
    Task<RiskListDto> ListAsync(
        Guid organizationId,
        Guid? hospitalId,
        string? status,
        string? category,
        string? search,
        Guid? ownerUserId,
        Guid? departmentId,
        bool? reviewDueOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<RiskDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct = default);

    Task<RiskDetailDto> CreateAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid userId,
        CreateRiskRequestDto request,
        CancellationToken ct = default);

    Task<RiskDetailDto> UpdateAsync(
        Guid organizationId,
        Guid id,
        Guid userId,
        UpdateRiskRequestDto request,
        CancellationToken ct = default);

    Task<RiskMatrixDto> GetMatrixAsync(
        Guid organizationId,
        Guid? hospitalId,
        bool useResidual = false,
        CancellationToken ct = default);
}
