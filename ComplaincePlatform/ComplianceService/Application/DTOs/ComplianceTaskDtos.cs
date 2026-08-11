using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.DTOs;

public class ComplianceTaskListItemDto
{
    public Guid Id { get; init; }
    public Guid HospitalId { get; init; }
    public Guid? ProgramId { get; init; }
    public Guid ComplianceStandardId { get; init; }
    public Guid ObjectiveElementId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string AssignedToUserId { get; init; } = string.Empty;
    public string? AssignedToDisplayName { get; init; }
    public DateTime? DueDate { get; init; }
    public ComplianceTaskStatus Status { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public TaskCriticalityLevel Criticality { get; init; }
    public ComplianceTaskPriority Priority { get; init; }
    public TaskCategory Category { get; init; }
    public TaskKind TaskKind { get; init; }
    public Guid? BlockedByTaskId { get; init; }
    public decimal Weight { get; init; }
    public string ObjectiveElementCode { get; init; } = string.Empty;
    public string ObjectiveElementName { get; init; } = string.Empty;
    public string StandardCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class ComplianceTaskDetailDto : ComplianceTaskListItemDto
{
    public string? MaturityState { get; init; }
}

public sealed class CreateComplianceTaskRequest
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

    /// <summary>Omitted JSON uses <see cref="ComplianceTaskPriority.Medium"/> at creation.</summary>
    public ComplianceTaskPriority? Priority { get; init; }

    public Guid? ProgramId { get; init; }
    public TaskCategory? Category { get; init; }
    public TaskKind? TaskKind { get; init; }
    public Guid? BlockedByTaskId { get; init; }
}

public sealed class PatchComplianceTaskRequest
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
