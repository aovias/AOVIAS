namespace Aovias.Orchestration.Contracts.Enums;

/// <summary>
/// Supported workflow types for the orchestration engine.
/// </summary>
public enum WorkflowType
{
    Deployment = 0,
    Rollback = 1,
    ServiceStart = 2,
    ServiceStop = 3,
    ServiceRestart = 4,
    HealthVerification = 5,
    Maintenance = 6,
    Recovery = 7
}
