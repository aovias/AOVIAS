namespace ComplianceService.Application.TaskExecution;

/// <summary>Public API lifecycle status (maps from <see cref="Domain.Enums.ComplianceTaskStatus"/>).</summary>
public enum TaskApiStatus
{
    NotStarted = 0,
    InProgress = 1,
    Blocked = 2,
    Completed = 3
}
