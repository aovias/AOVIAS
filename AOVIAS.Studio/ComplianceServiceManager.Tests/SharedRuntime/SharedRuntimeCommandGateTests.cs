using ComplianceServiceManager.Models.SharedRuntime;
using ComplianceServiceManager.Services.SharedRuntime;

namespace ComplianceServiceManager.Tests.SharedRuntime;

public sealed class SharedRuntimeCommandGateTests
{
    [Fact]
    public void Start_Disabled_WhenRuntimeBlocked()
    {
        Assert.False(SharedRuntimeCommandGate.CanStart(
            profileSelected: true,
            profileValid: true,
            planCanStart: false,
            status: SharedRuntimeStatus.Blocked,
            isStarting: false,
            isBusy: false));
    }

    [Fact]
    public void Start_Disabled_WhenProfileInvalid()
    {
        Assert.False(SharedRuntimeCommandGate.CanStart(
            profileSelected: true,
            profileValid: false,
            planCanStart: false,
            status: SharedRuntimeStatus.Blocked,
            isStarting: false,
            isBusy: false));
    }

    [Fact]
    public void Start_Disabled_WhenValidatingOrFailed()
    {
        Assert.False(SharedRuntimeCommandGate.CanStart(
            true, true, true, SharedRuntimeStatus.Validating, false, false));
        Assert.False(SharedRuntimeCommandGate.CanStart(
            true, true, true, SharedRuntimeStatus.Failed, false, false));
        Assert.False(SharedRuntimeCommandGate.CanStart(
            true, true, true, SharedRuntimeStatus.Starting, false, false));
    }

    [Fact]
    public void Start_Enabled_WhenValidPlanAndNotBusy()
    {
        Assert.True(SharedRuntimeCommandGate.CanStart(
            profileSelected: true,
            profileValid: true,
            planCanStart: true,
            status: SharedRuntimeStatus.Planned,
            isStarting: false,
            isBusy: false));
        Assert.True(SharedRuntimeCommandGate.CanStart(
            true, true, true, SharedRuntimeStatus.Stopped, false, false));
    }

    [Fact]
    public void Stop_Disabled_WhenOwnedCountIsZero()
    {
        Assert.False(SharedRuntimeCommandGate.CanStop(ownedServiceCount: 0, isBusy: false, isStarting: false));
        Assert.True(SharedRuntimeCommandGate.CanStop(ownedServiceCount: 2, isBusy: false, isStarting: false));
    }

    [Fact]
    public void Cancel_OnlyWhenStarting()
    {
        Assert.False(SharedRuntimeCommandGate.CanCancel(isStarting: false));
        Assert.True(SharedRuntimeCommandGate.CanCancel(isStarting: true));
    }

    [Fact]
    public void Restart_Disabled_WhenNoOwnedOrInvalidPlan()
    {
        Assert.False(SharedRuntimeCommandGate.CanRestart(
            true, true, true, SharedRuntimeStatus.Planned, ownedServiceCount: 0, false, false));
        Assert.False(SharedRuntimeCommandGate.CanRestart(
            true, false, false, SharedRuntimeStatus.Blocked, ownedServiceCount: 3, false, false));
        Assert.True(SharedRuntimeCommandGate.CanRestart(
            true, true, true, SharedRuntimeStatus.Planned, ownedServiceCount: 1, false, false));
    }
}
