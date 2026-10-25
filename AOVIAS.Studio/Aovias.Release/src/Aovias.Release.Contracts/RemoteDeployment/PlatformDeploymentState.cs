namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Lifecycle states for a platform deployment executed by the Runtime Agent queue.
/// </summary>
public enum PlatformDeploymentState
{
    Queued,
    Preparing,
    Uploading,
    Provisioning,
    Deploying,
    Verifying,
    Completed,
    Failed,
    Cancelled,
    RollingBack
}
