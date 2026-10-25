using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IDeficiencyQueryService
{
    Task<DeficiencyListDto> GetListAsync(
        Guid hospitalId,
        Guid frameworkId,
        string? status,
        string? severity,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<DeficiencyDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DeficiencySummaryDto> GetSummaryAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default);

    Task<IReadOnlyList<DeficiencyByStageDto>> GetByStageAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default);
}
