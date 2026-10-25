using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface ITaskQueryService
{
    Task<TaskSummaryDto> GetSummaryAsync(TaskQuerySpec spec, CancellationToken cancellationToken = default);

    Task<PagedResultDto<TaskListItemResponseDto>> GetPagedAsync(
        TaskQuerySpec spec,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TaskKanbanResponseDto> GetKanbanAsync(TaskQuerySpec spec, CancellationToken cancellationToken = default);

    Task<TaskDetailResponseDto?> GetDetailAsync(Guid taskId, Guid frameworkId, CancellationToken cancellationToken = default);

    /// <summary>Count of merged evidences pending review for completion gating (0 if task not in scope).</summary>
    Task<int> GetEvidencePendingReviewCountAsync(
        Guid taskId,
        Guid frameworkId,
        CancellationToken cancellationToken = default);

    /// <summary>Count of merged evidences rejected or needing more evidence (0 if task not in scope).</summary>
    Task<int> GetEvidenceRejectedOrNeedsMoreCountAsync(
        Guid taskId,
        Guid frameworkId,
        CancellationToken cancellationToken = default);

    /// <summary>True when task-linked OE has no merged evidence at all.</summary>
    Task<bool> HasMissingRequiredEvidenceAsync(
        Guid taskId,
        Guid frameworkId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<TaskEvidenceItemDto>> GetMergedEvidencesAsync(
        Guid taskId,
        Guid frameworkId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<TaskCommentDto>> GetCommentsAsync(
        Guid taskId,
        Guid frameworkId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<TaskHistoryEntryDto>> GetHistoryAsync(
        Guid taskId,
        Guid frameworkId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Returns null if task is not in framework scope.</summary>
    Task<ComplianceTaskFrameworkContext?> ResolveTaskFrameworkAsync(
        Guid taskId,
        Guid frameworkId,
        CancellationToken cancellationToken = default);
}

/// <summary>Resolved identifiers for a task under a framework context.</summary>
public sealed class ComplianceTaskFrameworkContext
{
    public Guid TaskId { get; init; }
    public Guid ObjectiveElementId { get; init; }
    public Guid ResolvedFrameworkId { get; init; }
}
