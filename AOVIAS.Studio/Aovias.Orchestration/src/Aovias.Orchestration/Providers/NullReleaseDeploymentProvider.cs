using Aovias.Orchestration.Contracts.Providers;

namespace Aovias.Orchestration.Providers;

/// <summary>
/// No-op release deployment provider used when the runtime agent is not hosting deployment services.
/// </summary>
internal sealed class NullReleaseDeploymentProvider : IReleaseDeploymentProvider
{
    public Task<DeploymentStepResult> ValidatePackageAsync(string releaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Failure("Release deployment provider is not configured."));

    public Task<DeploymentStepResult> ValidateDependenciesAsync(string releaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Failure("Release deployment provider is not configured."));

    public Task<DeploymentStepResult> ExtractReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Failure("Release deployment provider is not configured."));

    public Task<DeploymentStepResult> StageReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Failure("Release deployment provider is not configured."));

    public Task<DeploymentStepResult> LoadDockerImageAsync(string releaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Success("Docker image load skipped."));

    public Task<DeploymentStepResult> BackupCurrentReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Failure("Release deployment provider is not configured."));

    public Task<DeploymentStepResult> ActivateReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Failure("Release deployment provider is not configured."));

    public Task<DeploymentStepResult> RollbackReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Failure("Release deployment provider is not configured."));

    public Task<DeploymentStepResult> EnsureRuntimeFoldersAsync(string releaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Success("Runtime folders step skipped."));

    public Task<DeploymentStepResult> ConfigureNginxAsync(string releaseId, bool restorePrevious, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Success("Nginx configuration skipped."));

    public Task<DeploymentStepResult> RegisterDeploymentAsync(string releaseId, string deploymentId, string actor, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Success("Deployment registration skipped."));

    public Task<DeploymentStepResult> RegisterRuntimeAsync(string releaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Success("Runtime registration skipped."));

    public Task<DeploymentStepResult> VerifyDeploymentAcceptanceAsync(string releaseId, string deploymentId, CancellationToken cancellationToken = default)
        => Task.FromResult(DeploymentStepResult.Success("Deployment acceptance skipped."));

    public Task RecordAuditAsync(string releaseId, string eventType, string message, string actor, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
