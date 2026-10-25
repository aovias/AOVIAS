namespace ComplianceServiceManager.Models.RuntimeProfiles;

/// <summary>Lifecycle state of a runtime profile orchestration session.</summary>
public enum RuntimeProfileStatus
{
    Stopped = 0,
    Starting = 1,
    Running = 2,
    Stopping = 3,
    Failed = 4,
    PartiallyRunning = 5,
    Invalid = 6
}
