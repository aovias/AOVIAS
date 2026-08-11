using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IComplianceTaskAppService
{
    Task<ComplianceTaskDetailDto> CreateAsync(CreateComplianceTaskRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ComplianceTaskListItemDto>> ListAsync(
        Guid? frameworkId,
        CancellationToken cancellationToken,
        Domain.Enums.TaskCategory? category = null,
        Domain.Enums.TaskKind? taskKind = null);

    Task<ComplianceTaskDetailDto?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken);

    Task<ComplianceTaskDetailDto?> PatchAsync(Guid taskId, PatchComplianceTaskRequest request, CancellationToken cancellationToken);

    /// <param name="ifMatchRowVersion">When set, must match current <see cref="Domain.ComplianceTask.RowVersion"/> or update fails.</param>
    Task<ComplianceTaskDetailDto?> PatchAsync(Guid taskId, PatchComplianceTaskRequest request, byte[]? ifMatchRowVersion, CancellationToken cancellationToken);
}
