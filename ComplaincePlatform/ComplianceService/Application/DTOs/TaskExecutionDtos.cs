using ComplianceService.Application.TaskExecution;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.DTOs;

public sealed class TaskQuerySpec
{
    public Guid FrameworkId { get; init; }
    /// <summary>Explicit hospital scope for background callers that have no authenticated user context.</summary>
    public Guid? HospitalId { get; init; }
    public TaskApiStatus? Status { get; init; }
    public ComplianceTaskPriority? Priority { get; init; }
    public string? OwnerId { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? ChapterId { get; init; }
    public TaskRiskLevelApi? RiskLevel { get; init; }
    public string? Search { get; init; }
    public bool? OverdueOnly { get; init; }
    public bool? DueThisWeek { get; init; }
    public bool? FocusCriticalOverdue { get; init; }
    public DeficiencySeverity? DeficiencySeverity { get; init; }
    public DeficiencyType? DeficiencyType { get; init; }
    public ObjectiveElementClassification? MaturityStage { get; init; }
    public EvidenceStatus? ObligationEvidenceStatus { get; init; }
    public VerificationStatus? ObligationVerificationStatus { get; init; }
    /// <summary>When true, only tasks linked to an open deficiency.</summary>
    public bool? DeficiencyLinkedOnly { get; init; }
    public TaskCategory? Category { get; init; }
    public TaskKind? TaskKind { get; init; }
    /// <summary>
    /// When true, exclude the current user's tasks (colleagues / team queue).
    /// </summary>
    public bool? TeamOnly { get; init; }
    /// <summary>Inclusive calendar window start (UTC date).</summary>
    public DateTime? DueFrom { get; init; }
    /// <summary>Inclusive calendar window end (UTC date).</summary>
    public DateTime? DueTo { get; init; }
    /// <summary>table | kanban | risk | calendar | department</summary>
    public string View { get; init; } = "table";
}

public sealed class TaskSummaryDto
{
    public int Total { get; init; }
    public int NotStarted { get; init; }
    public int InProgress { get; init; }
    public int Blocked { get; init; }
    public int Completed { get; init; }
    public int Overdue { get; init; }
}

public class TaskListItemResponseDto
{
    public Guid Id { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public Guid ObjectiveElementId { get; init; }
    public string ObjectiveElementCode { get; init; } = string.Empty;
    public Guid ChapterId { get; init; }
    public string AssignedToUserId { get; init; } = string.Empty;
    public string? AssignedToDisplayName { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public ComplianceTaskPriority Priority { get; init; }
    public TaskApiStatus Status { get; init; }
    public TaskRiskLevelApi RiskLevel { get; init; }
    public int RiskScore { get; init; }
    public bool IsOverdue { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public TaskCriticalityLevel Criticality { get; init; }
    /// <summary>Distinct merged evidence count (task-linked + OE-linked for this task's objective element).</summary>
    public int EvidenceTotalCount { get; init; }
    /// <summary>Subset of merged evidences with verified OE link for this objective element (matches drawer merged rules).</summary>
    public int EvidenceApprovedCount { get; init; }
    /// <summary>
    /// Merged evidences awaiting review: no OE link for this objective element, or link status is Pending. Rejected links are not counted.
    /// </summary>
    public int EvidencePendingReviewCount { get; init; }
    public Guid? SourceDeficiencyId { get; init; }
    public TaskRelatedDeficiencyDto? RelatedDeficiency { get; init; }
    public TaskRelatedObligationDto? RelatedObligation { get; init; }
    public string? MaturityStageLabel { get; init; }
    public string? ObjectiveElementTitle { get; init; }
    public TaskCategory Category { get; init; }
    public TaskKind TaskKind { get; init; }
    public Guid? BlockedByTaskId { get; init; }
    public bool IsBlockedByDependency { get; init; }
}

public sealed class TaskRelatedDeficiencyDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string DeficiencyType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed class TaskRelatedObligationDto
{
    public Guid Id { get; init; }
    public string EvidenceStatus { get; init; } = string.Empty;
    public string VerificationStatus { get; init; } = string.Empty;
    public string AssessmentStatus { get; init; } = string.Empty;
    public string ImplementationStatus { get; init; } = string.Empty;
    public decimal ReadinessScore { get; init; }
    public string? NextActionLabel { get; init; }
}

public class TaskDetailResponseDto : TaskListItemResponseDto
{
    public Guid ComplianceStandardId { get; init; }
    public string StandardCode { get; init; } = string.Empty;
    public string? MaturityState { get; init; }
}

public sealed class TaskKanbanResponseDto
{
    public IReadOnlyList<TaskListItemResponseDto> NotStarted { get; init; } = Array.Empty<TaskListItemResponseDto>();
    public IReadOnlyList<TaskListItemResponseDto> InProgress { get; init; } = Array.Empty<TaskListItemResponseDto>();
    public IReadOnlyList<TaskListItemResponseDto> Blocked { get; init; } = Array.Empty<TaskListItemResponseDto>();
    public IReadOnlyList<TaskListItemResponseDto> Completed { get; init; } = Array.Empty<TaskListItemResponseDto>();
    public bool Truncated { get; init; }
}

public sealed class TaskEvidenceItemDto
{
    public Guid EvidenceId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string UploadedBy { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool? IsApproved { get; init; }
    public string? ReviewComment { get; init; }
    public TaskEvidenceSourceApi Source { get; init; }
    public int Version { get; init; }
}

public sealed class TaskCommentDto
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public Guid AuthorUserId { get; init; }
    public string Body { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class TaskHistoryEntryDto
{
    public Guid Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public Guid PerformedBy { get; init; }
    public DateTime Timestamp { get; init; }
    public string MetadataJson { get; init; } = "{}";
}

public sealed class CreateTaskExecutionRequestDto
{
    public Guid ObjectiveElementId { get; init; }
    public Guid ComplianceStandardId { get; init; }
    public string AssignedToUserId { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public DateTime? DueDate { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public TaskCriticalityLevel Criticality { get; init; } = TaskCriticalityLevel.Medium;
    public ComplianceTaskPriority? Priority { get; init; }
    public Guid? ProgramId { get; init; }
    public TaskCategory? Category { get; init; }
    public TaskKind? TaskKind { get; init; }
    public Guid? BlockedByTaskId { get; init; }
}

public sealed class PatchTaskExecutionRequestDto
{
    public string? AssignedToUserId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public DateTime? DueDate { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public TaskCriticalityLevel? Criticality { get; init; }
    public ComplianceTaskPriority? Priority { get; init; }
    public ComplianceTaskStatus? Status { get; init; }
    public TaskCategory? Category { get; init; }
    public TaskKind? TaskKind { get; init; }
    public Guid? BlockedByTaskId { get; init; }
}

public sealed class ReassignTaskRequestDto
{
    public string AssignedToUserId { get; init; } = string.Empty;
    public string? Reason { get; init; }
}

public sealed class AddTaskCommentRequestDto
{
    public string Body { get; init; } = string.Empty;
}

public static class RowVersionCodec
{
    public static string ToBase64(byte[]? rowVersion) =>
        rowVersion == null || rowVersion.Length == 0 ? string.Empty : Convert.ToBase64String(rowVersion);

    public static byte[]? FromBase64(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return null;
        }

        var s = base64.Trim().Trim('"');

        try
        {
            return Convert.FromBase64String(s);
        }
        catch
        {
            return null;
        }
    }

    public static bool Matches(byte[]? current, byte[]? expected)
    {
        if (current == null || expected == null || current.Length == 0 || expected.Length == 0)
        {
            return false;
        }

        return current.AsSpan().SequenceEqual(expected);
    }
}
