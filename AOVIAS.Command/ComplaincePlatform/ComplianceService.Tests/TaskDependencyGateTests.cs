using ComplianceService.Application.TaskExecution;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class TaskDependencyGateTests
{
    [Fact]
    public void CanComplete_when_no_blocker_returns_true()
    {
        var task = new ComplianceTask { Id = Guid.NewGuid(), BlockedByTaskId = null };
        Assert.True(TaskDependencyGate.CanComplete(task, blocker: null));
        Assert.Null(TaskDependencyGate.GetBlockReason(task, null));
    }

    [Fact]
    public void CanComplete_blocked_when_blocker_not_completed()
    {
        var blockerId = Guid.NewGuid();
        var task = new ComplianceTask { Id = Guid.NewGuid(), BlockedByTaskId = blockerId };
        var blocker = new ComplianceTask
        {
            Id = blockerId,
            Status = ComplianceTaskStatus.InProgress
        };

        Assert.False(TaskDependencyGate.CanComplete(task, blocker));
        Assert.Equal(TaskDependencyGate.BlockedByIncomplete, TaskDependencyGate.GetBlockReason(task, blocker));
    }

    [Fact]
    public void CanComplete_allowed_when_blocker_completed()
    {
        var blockerId = Guid.NewGuid();
        var task = new ComplianceTask { Id = Guid.NewGuid(), BlockedByTaskId = blockerId };
        var blocker = new ComplianceTask
        {
            Id = blockerId,
            Status = ComplianceTaskStatus.Completed
        };

        Assert.True(TaskDependencyGate.CanComplete(task, blocker));
        Assert.Null(TaskDependencyGate.GetBlockReason(task, blocker));
    }

    [Fact]
    public void CanComplete_missing_blocker_row_does_not_hard_lock()
    {
        var task = new ComplianceTask { Id = Guid.NewGuid(), BlockedByTaskId = Guid.NewGuid() };
        Assert.True(TaskDependencyGate.CanComplete(task, blocker: null));
    }
}
