using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.Orchestration.Contracts.Providers;
using Aovias.Orchestration.Contracts.Workflows;
using Aovias.Platform.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Orchestration.Steps;

internal abstract class NamedWorkflowStep : IWorkflowStep
{
    protected NamedWorkflowStep(ILogger logger)
    {
        Logger = logger;
    }

    public abstract string Name { get; }

    protected ILogger Logger { get; }

    public abstract Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default);

    protected static bool TryGetReleaseId(IWorkflowContext context, out string releaseId)
    {
        if (context.Parameters.TryGetValue("releaseId", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            releaseId = value;
            return true;
        }

        releaseId = string.Empty;
        return false;
    }
}

internal sealed class ValidatePackageStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public ValidatePackageStep(IReleaseDeploymentProvider deploymentProvider, ILogger<ValidatePackageStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Validate Package";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var result = await deploymentProvider.ValidatePackageAsync(releaseId, cancellationToken);
        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class VerifyRuntimeStep : NamedWorkflowStep
{
    private readonly ISystemProvider systemProvider;

    public VerifyRuntimeStep(ISystemProvider systemProvider, ILogger<VerifyRuntimeStep> logger) : base(logger)
    {
        this.systemProvider = systemProvider;
    }

    public override string Name => "Verify Runtime";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!systemProvider.IsAvailable)
        {
            return ExecutionResult.Failure("System provider is unavailable.");
        }

        var info = await systemProvider.GetSystemInfoAsync(cancellationToken);
        context.SetParameter("hostname", info.Hostname);
        return ExecutionResult.Success($"Runtime verified on {info.Hostname}.");
    }
}

internal sealed class VerifyDependenciesStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public VerifyDependenciesStep(IReleaseDeploymentProvider deploymentProvider, ILogger<VerifyDependenciesStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Verify Dependencies";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var result = await deploymentProvider.ValidateDependenciesAsync(releaseId, cancellationToken);
        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class BackupStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public BackupStep(IReleaseDeploymentProvider deploymentProvider, ILogger<BackupStep> logger) : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Backup Current Release";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var result = await deploymentProvider.BackupCurrentReleaseAsync(releaseId, cancellationToken);
        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class ComposeStopStep : NamedWorkflowStep
{
    private readonly IDockerProvider dockerProvider;
    private readonly IOptionsMonitor<DeploymentSettings> deploymentSettings;

    public ComposeStopStep(
        IDockerProvider dockerProvider,
        IOptionsMonitor<DeploymentSettings> deploymentSettings,
        ILogger<ComposeStopStep> logger) : base(logger)
    {
        this.dockerProvider = dockerProvider;
        this.deploymentSettings = deploymentSettings;
    }

    public override string Name => "Docker Compose Stop";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetCompose(context, out var composeFilePath, out var projectName))
        {
            return ExecutionResult.Success("Compose file not configured; stop step skipped.");
        }

        if (!File.Exists(composeFilePath))
        {
            return ExecutionResult.Success("Compose project not found; stop step skipped.");
        }

        if (!dockerProvider.IsAvailable)
        {
            return ExecutionResult.Failure("Docker provider is unavailable.");
        }

        await dockerProvider.ComposeDownAsync(composeFilePath, projectName, cancellationToken);
        return ExecutionResult.Success($"Docker Compose project '{projectName}' stopped.");
    }

    internal static bool TryGetCompose(IWorkflowContext context, out string composeFilePath, out string projectName)
    {
        composeFilePath = context.Parameters.GetValueOrDefault("composeFilePath") ?? string.Empty;
        projectName = context.Parameters.GetValueOrDefault("composeProjectName") ?? string.Empty;
        return !string.IsNullOrWhiteSpace(composeFilePath) && !string.IsNullOrWhiteSpace(projectName);
    }
}

internal sealed class ExtractPackageStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public ExtractPackageStep(IReleaseDeploymentProvider deploymentProvider, ILogger<ExtractPackageStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Release Extraction";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var result = await deploymentProvider.ExtractReleaseAsync(releaseId, cancellationToken);
        if (result.IsSuccess && result.Metadata.TryGetValue("extractionPath", out var extractionPath))
        {
            context.SetParameter("extractionPath", extractionPath);
        }

        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class StageReleaseStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;
    private readonly IOptionsMonitor<DeploymentSettings> deploymentSettings;

    public StageReleaseStep(
        IReleaseDeploymentProvider deploymentProvider,
        IOptionsMonitor<DeploymentSettings> deploymentSettings,
        ILogger<StageReleaseStep> logger) : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
        this.deploymentSettings = deploymentSettings;
    }

    public override string Name => "Stage Release";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var result = await deploymentProvider.StageReleaseAsync(releaseId, cancellationToken);
        if (!result.IsSuccess)
        {
            return ExecutionResult.Failure(result.Message);
        }

        foreach (var entry in result.Metadata)
        {
            context.SetParameter(entry.Key, entry.Value);
        }

        var serviceId = result.Metadata.GetValueOrDefault("serviceId") ?? "service";
        var prefix = deploymentSettings.CurrentValue.ComposeProjectNamePrefix;
        context.SetParameter("composeProjectName", $"{prefix}-{serviceId}");
        return ExecutionResult.Success(result.Message);
    }
}

internal sealed class LoadDockerImageStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public LoadDockerImageStep(IReleaseDeploymentProvider deploymentProvider, ILogger<LoadDockerImageStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Load Docker Image";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var result = await deploymentProvider.LoadDockerImageAsync(releaseId, cancellationToken);
        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class EnsureRuntimeFoldersStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public EnsureRuntimeFoldersStep(IReleaseDeploymentProvider deploymentProvider, ILogger<EnsureRuntimeFoldersStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Ensure Runtime Folders";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var result = await deploymentProvider.EnsureRuntimeFoldersAsync(releaseId, cancellationToken);
        if (result.IsSuccess && result.Metadata.TryGetValue("deploymentId", out var deploymentId))
        {
            context.SetParameter("deploymentId", deploymentId);
        }

        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class ConfigureNginxStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public ConfigureNginxStep(IReleaseDeploymentProvider deploymentProvider, ILogger<ConfigureNginxStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Configure Nginx";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var restorePrevious = context.Instance.WorkflowType == Aovias.Orchestration.Contracts.Enums.WorkflowType.Rollback;
        var result = await deploymentProvider.ConfigureNginxAsync(releaseId, restorePrevious, cancellationToken);
        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class RegisterDeploymentStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public RegisterDeploymentStep(IReleaseDeploymentProvider deploymentProvider, ILogger<RegisterDeploymentStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Register Deployment";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var deploymentId = context.Parameters.GetValueOrDefault("deploymentId") ?? releaseId;
        var actor = context.Parameters.GetValueOrDefault("requestedBy") ?? "system";
        var result = await deploymentProvider.RegisterDeploymentAsync(releaseId, deploymentId, actor, cancellationToken);
        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class RegisterRuntimeStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public RegisterRuntimeStep(IReleaseDeploymentProvider deploymentProvider, ILogger<RegisterRuntimeStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Register Runtime";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var result = await deploymentProvider.RegisterRuntimeAsync(releaseId, cancellationToken);
        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class DeploymentAcceptanceStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public DeploymentAcceptanceStep(IReleaseDeploymentProvider deploymentProvider, ILogger<DeploymentAcceptanceStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Deployment Acceptance";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var deploymentId = context.Parameters.GetValueOrDefault("deploymentId") ?? releaseId;
        var result = await deploymentProvider.VerifyDeploymentAcceptanceAsync(releaseId, deploymentId, cancellationToken);
        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class ComposeUpStep : NamedWorkflowStep
{
    private readonly IDockerProvider dockerProvider;

    public ComposeUpStep(IDockerProvider dockerProvider, ILogger<ComposeUpStep> logger) : base(logger)
    {
        this.dockerProvider = dockerProvider;
    }

    public override string Name => "Docker Compose Up";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!ComposeStopStep.TryGetCompose(context, out var composeFilePath, out var projectName))
        {
            return ExecutionResult.Failure("Compose file path and project name are required.");
        }

        if (!dockerProvider.IsAvailable)
        {
            return ExecutionResult.Failure("Docker provider is unavailable.");
        }

        await dockerProvider.ComposeUpAsync(composeFilePath, projectName, cancellationToken);
        return ExecutionResult.Success($"Docker Compose project '{projectName}' started.");
    }
}

internal sealed class RestorePreviousReleaseStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public RestorePreviousReleaseStep(IReleaseDeploymentProvider deploymentProvider, ILogger<RestorePreviousReleaseStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Restore Previous Release";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var result = await deploymentProvider.RollbackReleaseAsync(releaseId, cancellationToken);
        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class StopContainerStep : NamedWorkflowStep
{
    private readonly IDockerProvider dockerProvider;

    public StopContainerStep(IDockerProvider dockerProvider, ILogger<StopContainerStep> logger) : base(logger)
    {
        this.dockerProvider = dockerProvider;
    }

    public override string Name => "Stop";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Parameters.TryGetValue("containerName", out var containerName) || string.IsNullOrWhiteSpace(containerName))
        {
            return ExecutionResult.Success("No container specified; stop step skipped.");
        }

        if (!dockerProvider.IsAvailable)
        {
            return ExecutionResult.Failure("Docker provider is unavailable.");
        }

        await dockerProvider.StopContainerAsync(containerName, cancellationToken);
        return ExecutionResult.Success($"Container '{containerName}' stopped.");
    }
}

internal sealed class StartContainerStep : NamedWorkflowStep
{
    private readonly IDockerProvider dockerProvider;

    public StartContainerStep(IDockerProvider dockerProvider, ILogger<StartContainerStep> logger) : base(logger)
    {
        this.dockerProvider = dockerProvider;
    }

    public override string Name => "Start";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Parameters.TryGetValue("containerName", out var containerName) || string.IsNullOrWhiteSpace(containerName))
        {
            return ExecutionResult.Success("No container specified; start step skipped.");
        }

        if (!dockerProvider.IsAvailable)
        {
            return ExecutionResult.Failure("Docker provider is unavailable.");
        }

        await dockerProvider.StartContainerAsync(containerName, cancellationToken);
        return ExecutionResult.Success($"Container '{containerName}' started.");
    }
}

internal sealed class HealthVerificationStep : NamedWorkflowStep
{
    private readonly IHealthProvider healthProvider;
    private readonly IDockerProvider dockerProvider;
    private readonly IOptionsMonitor<DeploymentSettings> deploymentSettings;

    public HealthVerificationStep(
        IHealthProvider healthProvider,
        IDockerProvider dockerProvider,
        IOptionsMonitor<DeploymentSettings> deploymentSettings,
        ILogger<HealthVerificationStep> logger) : base(logger)
    {
        this.healthProvider = healthProvider;
        this.dockerProvider = dockerProvider;
        this.deploymentSettings = deploymentSettings;
    }

    public override string Name => "Health Verification";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!healthProvider.IsAvailable)
        {
            return ExecutionResult.Failure("Health provider is unavailable.");
        }

        var settings = deploymentSettings.CurrentValue;
        var endpoint = context.Parameters.GetValueOrDefault("healthEndpoint") ?? "/health";
        var expectedStatusCode = int.TryParse(context.Parameters.GetValueOrDefault("expectedStatusCode"), out var statusCode)
            ? statusCode
            : 200;
        var timeoutSeconds = int.TryParse(context.Parameters.GetValueOrDefault("healthTimeoutSeconds"), out var configuredTimeout)
            ? configuredTimeout
            : settings.HealthCheckTimeoutSeconds;
        var retryCount = int.TryParse(context.Parameters.GetValueOrDefault("healthRetryCount"), out var configuredRetry)
            ? configuredRetry
            : settings.HealthCheckRetryCount;

        if (context.Parameters.TryGetValue("containerName", out var containerName) &&
            !string.IsNullOrWhiteSpace(containerName) &&
            dockerProvider.IsAvailable)
        {
            var container = await dockerProvider.WaitForContainerHealthAsync(
                containerName,
                TimeSpan.FromSeconds(settings.ContainerHealthTimeoutSeconds),
                TimeSpan.FromMilliseconds(settings.ContainerHealthPollIntervalMilliseconds),
                cancellationToken);

            if (container is null || !string.Equals(container.Health, "healthy", StringComparison.OrdinalIgnoreCase))
            {
                return ExecutionResult.Failure($"Container '{containerName}' did not become healthy.");
            }
        }

        var result = await healthProvider.WaitForHealthAsync(
            endpoint,
            expectedStatusCode,
            TimeSpan.FromSeconds(timeoutSeconds),
            retryCount,
            TimeSpan.FromMilliseconds(settings.HealthCheckRetryDelayMilliseconds),
            cancellationToken);

        return result.IsHealthy
            ? ExecutionResult.Success(result.Message)
            : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class ActivateReleaseStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public ActivateReleaseStep(IReleaseDeploymentProvider deploymentProvider, ILogger<ActivateReleaseStep> logger)
        : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Activate Release";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Failure("Release ID is required.");
        }

        var result = await deploymentProvider.ActivateReleaseAsync(releaseId, cancellationToken);
        return result.IsSuccess ? ExecutionResult.Success(result.Message) : ExecutionResult.Failure(result.Message);
    }
}

internal sealed class AuditStep : NamedWorkflowStep
{
    private readonly IReleaseDeploymentProvider deploymentProvider;

    public AuditStep(IReleaseDeploymentProvider deploymentProvider, ILogger<AuditStep> logger) : base(logger)
    {
        this.deploymentProvider = deploymentProvider;
    }

    public override string Name => "Audit";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(context, out var releaseId))
        {
            return ExecutionResult.Success("Audit step completed.");
        }

        var actor = context.Parameters.GetValueOrDefault("requestedBy") ?? "system";
        await deploymentProvider.RecordAuditAsync(
            releaseId,
            "PipelineStepCompleted",
            "Deployment pipeline audit recorded.",
            actor,
            cancellationToken);

        return ExecutionResult.Success("Audit event recorded.");
    }
}

internal sealed class DockerReadyStep : NamedWorkflowStep
{
    private readonly IDockerProvider dockerProvider;

    public DockerReadyStep(IDockerProvider dockerProvider, ILogger<DockerReadyStep> logger) : base(logger)
    {
        this.dockerProvider = dockerProvider;
    }

    public override string Name => "Docker Ready";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!dockerProvider.IsAvailable)
        {
            return ExecutionResult.Failure("Docker provider is unavailable.");
        }

        var containers = await dockerProvider.ListContainersAsync(cancellationToken);
        return ExecutionResult.Success($"Docker ready. {containers.Count} containers visible.");
    }
}

internal sealed class RestartContainerStep : NamedWorkflowStep
{
    private readonly IDockerProvider dockerProvider;

    public RestartContainerStep(IDockerProvider dockerProvider, ILogger<RestartContainerStep> logger) : base(logger)
    {
        this.dockerProvider = dockerProvider;
    }

    public override string Name => "Restart Container";

    public override async Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Parameters.TryGetValue("containerName", out var containerName) || string.IsNullOrWhiteSpace(containerName))
        {
            return ExecutionResult.Failure("Container name is required.");
        }

        await dockerProvider.RestartContainerAsync(containerName, cancellationToken);
        return ExecutionResult.Success($"Container '{containerName}' restarted.");
    }
}

internal sealed class MaintenanceStep : NamedWorkflowStep
{
    public MaintenanceStep(ILogger<MaintenanceStep> logger) : base(logger) { }

    public override string Name => "Maintenance";

    public override Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(ExecutionResult.Success("Maintenance workflow step completed."));
}

internal sealed class RecoveryStep : NamedWorkflowStep
{
    public RecoveryStep(ILogger<RecoveryStep> logger) : base(logger) { }

    public override string Name => "Recovery";

    public override Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(ExecutionResult.Success("Recovery workflow step completed."));
}
