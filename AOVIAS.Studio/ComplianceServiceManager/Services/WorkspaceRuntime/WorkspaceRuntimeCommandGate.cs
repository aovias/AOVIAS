using ComplianceServiceManager.Models.SharedRuntime;

namespace ComplianceServiceManager.Services.WorkspaceRuntime;

public static class WorkspaceRuntimeCommandGate
{
    public static bool CanStart(
        bool workspaceSelected,
        bool planCanStart,
        SharedRuntimeStatus status,
        bool isStarting,
        bool isBusy)
    {
        if (!workspaceSelected || isBusy || isStarting)
        {
            return false;
        }

        if (!planCanStart)
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
}
