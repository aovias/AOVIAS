using ComplianceServiceManager.Models.SharedRuntime;

namespace ComplianceServiceManager.Services.SharedRuntime;

/// <summary>
/// Pure command enablement for Shared Runtime / Runtime Profile controls.
/// Issue count and validity come from the same plan/validation result shown to the user.
/// </summary>
public static class SharedRuntimeCommandGate
{
    public static bool CanStart(
        bool profileSelected,
        bool profileValid,
        bool planCanStart,
        SharedRuntimeStatus status,
        bool isStarting,
        bool isBusy)
    {
        if (!profileSelected || isBusy || isStarting)
        {
            return false;
        }

        if (!profileValid || !planCanStart)
        {
            return false;
        }

        return status is not (
            SharedRuntimeStatus.Blocked or
            SharedRuntimeStatus.Failed or
            SharedRuntimeStatus.Validating or
            SharedRuntimeStatus.Starting);
    }

    public static bool CanStop(int ownedServiceCount, bool isBusy, bool isStarting)
        => ownedServiceCount > 0 && !isBusy && !isStarting;

    public static bool CanCancel(bool isStarting) => isStarting;

    public static bool CanRestart(
        bool profileSelected,
        bool profileValid,
        bool planCanStart,
        SharedRuntimeStatus status,
        int ownedServiceCount,
        bool isStarting,
        bool isBusy)
        => ownedServiceCount > 0
           && CanStart(profileSelected, profileValid, planCanStart, status, isStarting, isBusy);
}
