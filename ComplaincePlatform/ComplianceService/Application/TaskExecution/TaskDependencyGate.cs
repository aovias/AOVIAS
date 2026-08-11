using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.TaskExecution;

/// <summary>
/// Pure rules for task dependency completion gating (unit-tested).
/// </summary>
public static class TaskDependencyGate
{
    public const string BlockedByIncomplete = "TASK_DEPENDENCY_BLOCKED";

    /// <summary>
    /// Returns true when <paramref name="task"/> may be completed given optional blocker status.
    /// </summary>
    public static bool CanComplete(ComplianceTask task, ComplianceTask? blocker)
    {
        if (task.BlockedByTaskId is null || task.BlockedByTaskId == Guid.Empty)
        {
            return true;
        }

        if (blocker is null)
        {
            // Missing blocker row: treat as unblockable to avoid orphan lockouts.
            return true;
        }

        return blocker.Status == ComplianceTaskStatus.Completed;
    }

    public static string? GetBlockReason(ComplianceTask task, ComplianceTask? blocker)
    {
        if (CanComplete(task, blocker))
        {
            return null;
        }

        return BlockedByIncomplete;
    }
}
