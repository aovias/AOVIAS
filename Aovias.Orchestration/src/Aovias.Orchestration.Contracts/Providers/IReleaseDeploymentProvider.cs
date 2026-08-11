namespace Aovias.Orchestration.Contracts.Providers;

/// <summary>
/// Release deployment operations executed by the orchestration pipeline.
/// </summary>
public interface IReleaseDeploymentProvider
{
    Task<DeploymentStepResult> ValidatePackageAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> ValidateDependenciesAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> ExtractReleaseAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> StageReleaseAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> LoadDockerImageAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> BackupCurrentReleaseAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> ActivateReleaseAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> RollbackReleaseAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> EnsureRuntimeFoldersAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> ConfigureNginxAsync(string releaseId, bool restorePrevious, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> RegisterDeploymentAsync(string releaseId, string deploymentId, string actor, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> RegisterRuntimeAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentStepResult> VerifyDeploymentAcceptanceAsync(string releaseId, string deploymentId, CancellationToken cancellationToken = default);

    Task RecordAuditAsync(string releaseId, string eventType, string message, string actor, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a deployment pipeline step.
/// </summary>
public sealed class DeploymentStepResult
{
    public required bool IsSuccess { get; init; }

    public required string Message { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public static DeploymentStepResult Success(string message, IReadOnlyDictionary<string, string>? metadata = null)
        => new() { IsSuccess = true, Message = message, Metadata = metadata ?? new Dictionary<string, string>() };

    public static DeploymentStepResult Failure(string message)
        => new() { IsSuccess = false, Message = message };
}
