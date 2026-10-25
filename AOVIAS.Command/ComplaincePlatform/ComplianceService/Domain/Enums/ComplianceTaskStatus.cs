namespace ComplianceService.Domain.Enums;

/// <summary>
/// Lifecycle status for a compliance execution task. Completion must use the aggregate complete-task endpoint, not PATCH.
/// API documentation refers to this enum conceptually as TaskStatus.
/// </summary>
public enum ComplianceTaskStatus
{
    Pending = 0,
    InProgress = 1,
    OnHold = 2,
    Blocked = 3,
    Completed = 4
}
