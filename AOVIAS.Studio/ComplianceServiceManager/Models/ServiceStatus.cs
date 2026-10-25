namespace ComplianceServiceManager.Models;

public enum ServiceStatus
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Error,
    Diagnosing,
    Fixing,
    Rebuilding,
    Restarting,
    Resolved,
    FailedRepair
}
