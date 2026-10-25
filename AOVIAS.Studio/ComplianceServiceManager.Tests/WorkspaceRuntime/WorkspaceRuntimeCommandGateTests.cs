using ComplianceServiceManager.Models.SharedRuntime;
using ComplianceServiceManager.Services.WorkspaceRuntime;

namespace ComplianceServiceManager.Tests.WorkspaceRuntime;

public sealed class WorkspaceRuntimeCommandGateTests
{
    [Fact]
    public void Start_Disabled_WhenBlockedOrBusy()
    {
        Assert.False(WorkspaceRuntimeCommandGate.CanStart(true, false, SharedRuntimeStatus.Blocked, false, false));
        Assert.False(WorkspaceRuntimeCommandGate.CanStart(true, true, SharedRuntimeStatus.Failed, false, false));
        Assert.False(WorkspaceRuntimeCommandGate.CanStart(true, true, SharedRuntimeStatus.Starting, false, false));
        Assert.False(WorkspaceRuntimeCommandGate.CanStart(false, true, SharedRuntimeStatus.Planned, false, false));
        Assert.False(WorkspaceRuntimeCommandGate.CanStart(true, true, SharedRuntimeStatus.Planned, true, false));
        Assert.False(WorkspaceRuntimeCommandGate.CanStart(true, true, SharedRuntimeStatus.Planned, false, true));
    }

    [Fact]
    public void Start_Enabled_WhenValidPlan()
    {
        Assert.True(WorkspaceRuntimeCommandGate.CanStart(true, true, SharedRuntimeStatus.Planned, false, false));
        Assert.True(WorkspaceRuntimeCommandGate.CanStart(true, true, SharedRuntimeStatus.Stopped, false, false));
    }

    [Fact]
    public void Stop_RequiresOwnedServices()
    {
        Assert.False(WorkspaceRuntimeCommandGate.CanStop(0, false, false));
        Assert.True(WorkspaceRuntimeCommandGate.CanStop(1, false, false));
        Assert.False(WorkspaceRuntimeCommandGate.CanStop(1, true, false));
        Assert.False(WorkspaceRuntimeCommandGate.CanStop(1, false, true));
    }

    [Fact]
    public void Cancel_OnlyWhenStarting()
    {
        Assert.False(WorkspaceRuntimeCommandGate.CanCancel(false));
        Assert.True(WorkspaceRuntimeCommandGate.CanCancel(true));
    }
}
