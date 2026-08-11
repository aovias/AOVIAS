using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.Contracts.Releases;

namespace ComplianceServiceManager.Contracts.Deployment;

/// <summary>
/// Orchestrates remote platform deployments from AOVIAS Command to Runtime Agents.
/// </summary>
public interface IRemoteDeploymentOrchestrator
{
    event EventHandler<DeploymentUploadProgress>? UploadProgressChanged;

    Task<PlatformDeploymentManifest> LoadManifestAsync(
        string releaseLibraryRoot,
        CancellationToken cancellationToken = default);

    Task<PlatformDeploymentResponse> DeployPlatformAsync(
        RemoteEnvironment environment,
        string releaseLibraryRoot,
        string requestedBy,
        IReadOnlyList<string>? selectedServiceIds = null,
        CancellationToken cancellationToken = default);

    Task<DeploymentStatusResponse?> PollStatusAsync(
        RemoteEnvironment environment,
        string deploymentId,
        CancellationToken cancellationToken = default);

    Task<PlatformDeploymentResponse> RollbackAsync(
        RemoteEnvironment environment,
        string deploymentId,
        string requestedBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformDeploymentHistoryEntry>> GetHistoryAsync(
        RemoteEnvironment environment,
        CancellationToken cancellationToken = default);

    Task<DeploymentReliabilityMetrics> GetReliabilityMetricsAsync(
        RemoteEnvironment environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the authoritative runtime release transition:
    /// Uploaded → Validated → Compatible, then generates a deployment plan (→ Staged).
    /// </summary>
    Task<DeploymentPlan> ValidateAndPlanReleaseOnRuntimeAsync(
        RemoteEnvironment environment,
        string releaseId,
        string requestedBy,
        CancellationToken cancellationToken = default);
}
