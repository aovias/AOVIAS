namespace Aovias.Release.Contracts.Enums;

/// <summary>
/// Release lifecycle states for the AOVIAS release state machine.
/// </summary>
public enum ReleaseState
{
    Uploaded = 0,
    Validated = 1,
    Compatible = 2,
    Staged = 3,
    Deploying = 4,
    HealthCheck = 5,
    Activated = 6,
    Completed = 7,
    Failed = 8,
    RollingBack = 9,
    RolledBack = 10,
    Archived = 11
}
