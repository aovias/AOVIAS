using Aovias.Configuration.Profiles;
using Aovias.Deployment.Contracts.Reconciliation;
using Aovias.Orchestration.Contracts.Providers;
using Microsoft.Extensions.Logging;

namespace Aovias.Runtime.Agent.Infrastructure.Reconciliation;

/// <summary>Repairs a missing Docker network (Sprint M2.6 D6 — "recreate Docker network", allowed automatically).</summary>
public sealed class DockerNetworkRepairExecutor : IRepairExecutor
{
    private readonly IDockerProvider dockerProvider;

    public DockerNetworkRepairExecutor(IDockerProvider dockerProvider) => this.dockerProvider = dockerProvider;

    public bool CanHandle(RepairActionType actionType) => actionType == RepairActionType.RecreateDockerNetwork;

    public async Task<RepairExecutionOutcome> ExecuteAsync(RepairTask task, DriftFinding finding, CancellationToken cancellationToken = default)
    {
        if (!dockerProvider.IsAvailable)
        {
            return RepairExecutionOutcome.Failure("Docker provider is unavailable.");
        }

        await dockerProvider.EnsureNetworkExistsAsync(task.ResourceId, cancellationToken);
        return RepairExecutionOutcome.Success($"Docker network '{task.ResourceId}' now exists.");
    }
}

/// <summary>Repairs a missing directory (Sprint M2.6 D6 — "recreate missing directory", allowed automatically).</summary>
public sealed class DirectoryRepairExecutor : IRepairExecutor
{
    private readonly IFileSystemProvider fileSystemProvider;

    public DirectoryRepairExecutor(IFileSystemProvider fileSystemProvider) => this.fileSystemProvider = fileSystemProvider;

    public bool CanHandle(RepairActionType actionType) => actionType == RepairActionType.RecreateDirectory;

    public async Task<RepairExecutionOutcome> ExecuteAsync(RepairTask task, DriftFinding finding, CancellationToken cancellationToken = default)
    {
        await fileSystemProvider.CreateDirectoryAsync(task.ResourceId, cancellationToken);
        return RepairExecutionOutcome.Success($"Directory '{task.ResourceId}' now exists.");
    }
}

/// <summary>
/// Repairs a stopped or missing container by restarting it (Sprint M2.6 D6 — "restart container",
/// allowed automatically). <b>Technical Debt</b>: only handles a container that already exists but
/// is stopped; a container that has never been created cannot be recreated by this executor
/// today, since <see cref="IDockerProvider"/> has no "create container from image" method (compose
/// project recreation exists only at the whole-project level via
/// <see cref="IDockerProvider.ComposeUpAsync"/>, which this executor does not have a compose file
/// path to call safely). Such a finding is honestly reported as a failure, not silently ignored —
/// see the M2.6 report's Technical Debt section.
/// </summary>
public sealed class ContainerRestartRepairExecutor : IRepairExecutor
{
    private readonly IDockerProvider dockerProvider;
    private readonly ILogger<ContainerRestartRepairExecutor> logger;

    public ContainerRestartRepairExecutor(IDockerProvider dockerProvider, ILogger<ContainerRestartRepairExecutor> logger)
    {
        this.dockerProvider = dockerProvider;
        this.logger = logger;
    }

    public bool CanHandle(RepairActionType actionType) => actionType == RepairActionType.RestartContainer;

    public async Task<RepairExecutionOutcome> ExecuteAsync(RepairTask task, DriftFinding finding, CancellationToken cancellationToken = default)
    {
        if (!dockerProvider.IsAvailable)
        {
            return RepairExecutionOutcome.Failure("Docker provider is unavailable.");
        }

        var containerName = task.ResourceType == ReconciliationResourceTypes.HealthEndpoint
            ? finding.ResourceId // health-endpoint findings key by endpoint id, not container name — best-effort fallback below.
            : task.ResourceId;

        try
        {
            await dockerProvider.RestartContainerAsync(containerName, cancellationToken);
            return RepairExecutionOutcome.Success($"Container '{containerName}' restarted.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to restart container {ContainerName} during self-healing.", containerName);
            return RepairExecutionOutcome.Failure(
                $"Could not restart '{containerName}': {exception.Message}. If the container does not exist yet, recreating it is not supported by this executor (Sprint M2.6 Technical Debt) — redeploy the service instead.");
        }
    }
}

/// <summary>Restores a directory's expected permission (Sprint M2.6 D6 — "restore permissions", allowed automatically, elevated to Security-severity + always-approved-first when the path is sensitive per <see cref="DriftRepairMatrix"/>).</summary>
public sealed class PermissionsRepairExecutor : IRepairExecutor
{
    public bool CanHandle(RepairActionType actionType) => actionType == RepairActionType.RestorePermissions;

    public Task<RepairExecutionOutcome> ExecuteAsync(RepairTask task, DriftFinding finding, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsLinux())
        {
            return Task.FromResult(RepairExecutionOutcome.Failure(
                $"Could not restore permission on '{task.ResourceId}': POSIX permission repair is only supported on Linux."));
        }

        try
        {
            var mode = Convert.ToInt32(finding.Expected, 8);
            File.SetUnixFileMode(task.ResourceId, (UnixFileMode)mode);
            return Task.FromResult(RepairExecutionOutcome.Success($"Permission on '{task.ResourceId}' restored to '{finding.Expected}'."));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException)
        {
            return Task.FromResult(RepairExecutionOutcome.Failure($"Could not restore permission on '{task.ResourceId}': {exception.Message}"));
        }
    }
}

/// <summary>Recreates a symbolic link (Sprint M2.6 D6 — "recreate symbolic link", allowed automatically).</summary>
public sealed class SymbolicLinkRepairExecutor : IRepairExecutor
{
    public bool CanHandle(RepairActionType actionType) => actionType == RepairActionType.RecreateSymbolicLink;

    public Task<RepairExecutionOutcome> ExecuteAsync(RepairTask task, DriftFinding finding, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(task.ResourceId) || Directory.Exists(task.ResourceId))
            {
                File.Delete(task.ResourceId);
            }

            File.CreateSymbolicLink(task.ResourceId, finding.Expected);
            return Task.FromResult(RepairExecutionOutcome.Success($"Symbolic link '{task.ResourceId}' now points to '{finding.Expected}'."));
        }
        catch (Exception exception)
        {
            return Task.FromResult(RepairExecutionOutcome.Failure($"Could not recreate symbolic link '{task.ResourceId}': {exception.Message}"));
        }
    }
}

/// <summary>Reloads runtime configuration by re-activating the active profile (Sprint M2.6 D6 — "reload configuration"; automatic-or-not is policy-driven per <see cref="DriftRepairMatrix"/>'s Configuration Drift row).</summary>
public sealed class ConfigurationReloadRepairExecutor : IRepairExecutor
{
    private readonly IProfileManager profileManager;

    public ConfigurationReloadRepairExecutor(IProfileManager profileManager) => this.profileManager = profileManager;

    public bool CanHandle(RepairActionType actionType) => actionType == RepairActionType.ReloadConfiguration;

    public async Task<RepairExecutionOutcome> ExecuteAsync(RepairTask task, DriftFinding finding, CancellationToken cancellationToken = default)
    {
        await profileManager.ActivateProfileAsync(profileManager.ActiveProfile, cancellationToken);
        return RepairExecutionOutcome.Success($"Configuration profile '{profileManager.ActiveProfile}' re-activated.");
    }
}

/// <summary>
/// Handles every repair action that always requires operator approval (delete a volume, database
/// changes, certificate/secret replacement, image downgrade, rollback) plus
/// <see cref="RepairActionType.Unknown"/> (Sprint M2.6 D6/D11). Even once an operator has approved
/// one of these, this sprint does not implement the actual destructive/high-risk action itself —
/// doing so safely would require deep integration with subsystems this sprint's Scope and Out of
/// Scope lists deliberately exclude (database migration tooling, certificate issuance, the
/// external release rollback workflow). This executor honestly reports failure with a directive
/// message rather than fabricating success — mirroring
/// <c>PendingExternalExecutionTaskProvisioner</c>'s honesty pattern from Sprint M2.5. See the M2.6
/// report's Technical Debt section.
/// </summary>
public sealed class ApprovalRequiredRepairExecutor : IRepairExecutor
{
    private static readonly HashSet<RepairActionType> HandledTypes =
    [
        RepairActionType.DeleteVolume,
        RepairActionType.DatabaseChange,
        RepairActionType.CertificateReplacement,
        RepairActionType.SecretReplacement,
        RepairActionType.ImageDowngrade,
        RepairActionType.RollbackDeployment,
        RepairActionType.Unknown
    ];

    public bool CanHandle(RepairActionType actionType) => HandledTypes.Contains(actionType);

    public Task<RepairExecutionOutcome> ExecuteAsync(RepairTask task, DriftFinding finding, CancellationToken cancellationToken = default)
        => Task.FromResult(RepairExecutionOutcome.Failure(
            $"'{task.ActionType}' for {task.ResourceType} '{task.ResourceId}' was approved but requires manual execution by an operator — this action is not automated in Sprint M2.6 (Technical Debt)."));
}
