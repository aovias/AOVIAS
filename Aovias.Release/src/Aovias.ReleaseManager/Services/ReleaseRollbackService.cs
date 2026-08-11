using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.PackageBuilder.Abstractions;
using Aovias.Platform.Core;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Releases;
using Microsoft.Extensions.Options;
using PlatformExecutionContext = Aovias.Platform.Core.ExecutionContext;

namespace Aovias.ReleaseManager.Services;

/// <summary>
/// Queues rollback workflows through the orchestration engine.
/// </summary>
public sealed class ReleaseRollbackService : Abstractions.IReleaseRollbackService
{
    private readonly IExecutionEngine executionEngine;
    private readonly IPackageReader packageReader;
    private readonly IOptionsMonitor<DeploymentSettings> deploymentSettings;
    private readonly IOptionsMonitor<DockerSettings> dockerSettings;

    public ReleaseRollbackService(
        IExecutionEngine executionEngine,
        IPackageReader packageReader,
        IOptionsMonitor<DeploymentSettings> deploymentSettings,
        IOptionsMonitor<DockerSettings> dockerSettings)
    {
        this.executionEngine = executionEngine;
        this.packageReader = packageReader;
        this.deploymentSettings = deploymentSettings;
        this.dockerSettings = dockerSettings;
    }

    /// <inheritdoc />
    public async Task<RollbackReleaseResult> RollbackAsync(
        RollbackReleaseRequest request,
        ReleaseRecord release,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
        var deployment = deploymentSettings.CurrentValue;
        var composeFilePath = Path.Combine(dockerSettings.CurrentValue.ComposeRoot, contents.Service.ServiceId, "docker-compose.yml");

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["releaseId"] = release.Identity.ReleaseId,
            ["requestedBy"] = request.RequestedBy,
            ["rollbackReason"] = "Operator requested rollback.",
            ["serviceId"] = contents.Service.ServiceId,
            ["containerName"] = contents.Service.ContainerName,
            ["composeFilePath"] = composeFilePath,
            ["composeProjectName"] = $"{deployment.ComposeProjectNamePrefix}-{contents.Service.ServiceId}",
            ["healthEndpoint"] = contents.Health.HealthEndpoint,
            ["expectedStatusCode"] = contents.Health.ExpectedStatusCode.ToString(),
            ["healthTimeoutSeconds"] = deployment.HealthCheckTimeoutSeconds.ToString(),
            ["healthRetryCount"] = deployment.HealthCheckRetryCount.ToString()
        };

        var context = PlatformExecutionContext.Create(
            request.RequestedBy,
            release.Identity.Runtime,
            release.Identity.Environment,
            cancellationToken,
            release.Identity.ReleaseId);

        var instance = await executionEngine.EnqueueAsync(WorkflowType.Rollback, parameters, context, cancellationToken);

        return new RollbackReleaseResult
        {
            ReleaseId = release.Identity.ReleaseId,
            State = ReleaseState.RollingBack.ToString(),
            Message = $"Rollback workflow {instance.WorkflowId} queued.",
            QueuedForExecution = true
        };
    }
}
