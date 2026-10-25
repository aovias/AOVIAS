namespace ComplianceServiceManager.Models;

public enum RepairStage
{
    None,
    Diagnosing,
    Fixing,
    Rebuilding,
    Restarting,
    Resolved,
    FailedRepair
}
