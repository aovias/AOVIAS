using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.ReleaseManager.Abstractions;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Agent.Infrastructure.Observability.StateStore;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using Aovias.Runtime.Contracts.Observability;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Observability.Collectors;

/// <summary>
/// Collects Docker infrastructure observations for the runtime state store.
/// </summary>
public sealed class DockerStateCollector
{
    private readonly IOptionsMonitor<DockerSettings> dockerSettings;
    private readonly ILogger<DockerStateCollector> logger;

    public DockerStateCollector(IOptionsMonitor<DockerSettings> dockerSettings, ILogger<DockerStateCollector> logger)
    {
        this.dockerSettings = dockerSettings;
        this.logger = logger;
    }

    public async Task<DockerObservationBundle> CollectAsync(CancellationToken cancellationToken = default)
    {
        var settings = dockerSettings.CurrentValue;
        if (!settings.Enabled)
        {
            return new DockerObservationBundle
            {
                Status = "Disabled",
                ContainerCount = 0,
                RunningContainerCount = 0,
                NetworkCount = 0,
                VolumeCount = 0,
                ImageCount = 0,
                Containers = [],
                Networks = [],
                Volumes = []
            };
        }

        try
        {
            using var client = CreateClient(settings);
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true }, cancellationToken);
            var networks = await client.Networks.ListNetworksAsync(new NetworksListParameters(), cancellationToken);
            var volumes = await client.Volumes.ListAsync(cancellationToken);
            var images = await client.Images.ListImagesAsync(new ImagesListParameters(), cancellationToken);

            var observedContainers = new List<ContainerObservationDto>();
            foreach (var container in containers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ContainerInspectResponse? inspect = null;
                try
                {
                    inspect = await client.Containers.InspectContainerAsync(container.ID, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogDebug(exception, "Unable to inspect container {ContainerId}", container.ID);
                }

                observedContainers.Add(new ContainerObservationDto
                {
                    Id = container.ID,
                    Name = container.Names.FirstOrDefault()?.TrimStart('/') ?? container.ID,
                    Image = container.Image,
                    Status = container.Status,
                    State = container.State,
                    Health = inspect?.State.Health?.Status ?? container.Status,
                    RestartCount = (int)(inspect?.RestartCount ?? 0),
                    Network = container.NetworkSettings?.Networks.Keys.FirstOrDefault(),
                    Volume = inspect?.Mounts.FirstOrDefault()?.Name ?? inspect?.Mounts.FirstOrDefault()?.Source
                });
            }

            return new DockerObservationBundle
            {
                Status = "Running",
                ContainerCount = containers.Count,
                RunningContainerCount = containers.Count(item => string.Equals(item.State, "running", StringComparison.OrdinalIgnoreCase)),
                NetworkCount = networks.Count,
                VolumeCount = volumes.Volumes?.Count ?? 0,
                ImageCount = images.Count,
                Containers = observedContainers,
                Networks = networks.Select(network => new NetworkObservationDto
                {
                    Id = network.ID,
                    Name = network.Name,
                    Driver = network.Driver,
                    Scope = network.Scope,
                    ContainerCount = network.Containers?.Count ?? 0
                }).ToList(),
                Volumes = volumes.Volumes?.Select(volume => new VolumeObservationDto
                {
                    Name = volume.Name,
                    Driver = volume.Driver,
                    Mountpoint = volume.Mountpoint
                }).ToList() ?? []
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to collect Docker observability state.");
            return new DockerObservationBundle
            {
                Status = "Unavailable",
                ContainerCount = 0,
                RunningContainerCount = 0,
                NetworkCount = 0,
                VolumeCount = 0,
                ImageCount = 0,
                Containers = [],
                Networks = [],
                Volumes = []
            };
        }
    }

    private static DockerClient CreateClient(DockerSettings settings)
    {
        var endpoint = !string.IsNullOrWhiteSpace(settings.Socket) ? settings.Socket : settings.Host;
        return new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
    }
}

/// <summary>
/// Docker observation bundle used while projecting runtime state.
/// </summary>
public sealed class DockerObservationBundle
{
    public static DockerObservationBundle Empty { get; } = new()
    {
        Status = "Unknown",
        ContainerCount = 0,
        RunningContainerCount = 0,
        NetworkCount = 0,
        VolumeCount = 0,
        ImageCount = 0,
        Containers = [],
        Networks = [],
        Volumes = []
    };

    public required string Status { get; init; }

    public required int ContainerCount { get; init; }

    public required int RunningContainerCount { get; init; }

    public required int NetworkCount { get; init; }

    public required int VolumeCount { get; init; }

    public required int ImageCount { get; init; }

    public required IReadOnlyList<ContainerObservationDto> Containers { get; init; }

    public required IReadOnlyList<NetworkObservationDto> Networks { get; init; }

    public required IReadOnlyList<VolumeObservationDto> Volumes { get; init; }
}

/// <summary>
/// Collects host metrics and orchestration summaries for runtime state projection.
/// </summary>
public sealed class SystemMetricsCollector
{
    private readonly IStatisticsProvider statisticsProvider;
    private readonly IHealthProvider healthProvider;
    private readonly IExecutionEngine executionEngine;
    private readonly IReleaseRepository releaseRepository;
    private readonly RuntimeCapabilitiesProvider capabilitiesProvider;
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;

    public SystemMetricsCollector(
        IStatisticsProvider statisticsProvider,
        IHealthProvider healthProvider,
        IExecutionEngine executionEngine,
        IReleaseRepository releaseRepository,
        RuntimeCapabilitiesProvider capabilitiesProvider,
        IOptionsMonitor<RuntimeSettings> runtimeSettings)
    {
        this.statisticsProvider = statisticsProvider;
        this.healthProvider = healthProvider;
        this.executionEngine = executionEngine;
        this.releaseRepository = releaseRepository;
        this.capabilitiesProvider = capabilitiesProvider;
        this.runtimeSettings = runtimeSettings;
    }

    public async Task<RuntimeProjectionBundle> CollectAsync(CancellationToken cancellationToken = default)
    {
        var statistics = await statisticsProvider.GetStatisticsAsync(cancellationToken);
        var health = await healthProvider.GetHealthAsync(cancellationToken);
        var workflows = await executionEngine.ListAsync(cancellationToken);
        var releases = await releaseRepository.ListAsync(cancellationToken);
        var capabilities = capabilitiesProvider.GetCapabilities(runtimeSettings.CurrentValue.RuntimeVersion);

        var workflowObservations = workflows.Select(workflow => new WorkflowObservationDto
        {
            WorkflowId = workflow.WorkflowId,
            WorkflowName = workflow.WorkflowName,
            WorkflowType = workflow.WorkflowType.ToString(),
            Status = workflow.Status.ToString(),
            CurrentStep = workflow.CurrentStep,
            CompletedSteps = workflow.CompletedSteps,
            TotalSteps = workflow.Steps.Count,
            ReleaseId = workflow.Parameters.GetValueOrDefault("releaseId"),
            StartedAt = workflow.StartedAt
        }).ToList();

        var releaseObservations = releases.Select(release => new ReleaseObservationDto
        {
            ReleaseId = release.Identity.ReleaseId,
            PackageId = release.Identity.PackageId,
            PackageVersion = release.Identity.PackageVersion,
            State = release.State.ToString(),
            UpdatedAt = release.UpdatedAt
        }).ToList();

        var runningWorkflows = workflows.Count(workflow =>
            workflow.Status is WorkflowState.Executing or WorkflowState.Queued or WorkflowState.Validating);

        var queuedWorkflows = workflows.Count(workflow => workflow.Status == WorkflowState.Queued);

        return new RuntimeProjectionBundle
        {
            Statistics = statistics,
            Health = health,
            Capabilities = capabilities.Capabilities,
            Workflows = workflowObservations,
            Releases = releaseObservations,
            RunningWorkflowCount = runningWorkflows,
            DeploymentQueueCount = queuedWorkflows,
            ActiveReleaseCount = releases.Count(release =>
                release.State is Aovias.Release.Contracts.Enums.ReleaseState.Activated
                    or Aovias.Release.Contracts.Enums.ReleaseState.Completed)
        };
    }
}

/// <summary>
/// Bundle used when projecting observability state.
/// </summary>
public sealed class RuntimeProjectionBundle
{
    public required Aovias.Runtime.Contracts.Monitoring.RuntimeStatisticsDto Statistics { get; init; }

    public required Aovias.Runtime.Contracts.Health.RuntimeHealthDto Health { get; init; }

    public required IReadOnlyList<string> Capabilities { get; init; }

    public required IReadOnlyList<WorkflowObservationDto> Workflows { get; init; }

    public required IReadOnlyList<ReleaseObservationDto> Releases { get; init; }

    public required int RunningWorkflowCount { get; init; }

    public required int DeploymentQueueCount { get; init; }

    public required int ActiveReleaseCount { get; init; }
}
