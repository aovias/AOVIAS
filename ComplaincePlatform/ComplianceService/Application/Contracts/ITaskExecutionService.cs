using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace ComplianceService.Application.Contracts;

public interface ITaskExecutionService
{
    Task<TaskDetailResponseDto> CreateAsync(
        Guid frameworkId,
        CreateTaskExecutionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<TaskDetailResponseDto?> PatchAsync(
        Guid frameworkId,
        Guid taskId,
        PatchTaskExecutionRequestDto request,
        byte[]? ifMatchRowVersion,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid frameworkId,
        Guid taskId,
        byte[]? ifMatchRowVersion,
        CancellationToken cancellationToken = default);

    Task<TaskDetailResponseDto?> ReassignAsync(
        Guid frameworkId,
        Guid taskId,
        ReassignTaskRequestDto request,
        byte[]? ifMatchRowVersion,
        CancellationToken cancellationToken = default);

    Task<TaskCommentDto> AddCommentAsync(
        Guid frameworkId,
        Guid taskId,
        AddTaskCommentRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Guid> UploadEvidenceAsync(
        Guid frameworkId,
        Guid taskId,
        HttpRequest request,
        CancellationToken cancellationToken = default);
}
