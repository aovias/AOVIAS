namespace Aovias.Runtime.Contracts.Deployment;

/// <summary>
/// Operator-facing deployment session lifecycle (Sprint 12.1 D17).
/// </summary>
public enum DeploymentSessionState
{
    NotStarted,
    Analyzing,
    ReadyToDeploy,
    Uploading,
    Provisioning,
    Deploying,
    HealthChecking,
    Certifying,
    Completed,
    Failed,
    Cancelled
}
