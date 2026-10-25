namespace ComplianceServiceManager.Models.Runtime;

/// <summary>
/// Represents the health state of a runtime component or service.
/// </summary>
public enum RuntimeHealth
{
    Unknown = 0,
    Healthy = 1,
    Running = 2,
    Valid = 3,
    Degraded = 4,
    Warning = 5,
    Stopped = 6,
    NotInstalled = 7,
    Unreachable = 8
}
