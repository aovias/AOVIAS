using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Agent.Infrastructure.Observability.Collectors;
using Aovias.Runtime.Agent.Infrastructure.Observability.StateStore;
using Aovias.Runtime.Contracts.Observability;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Observability.Snapshots;

/// <summary>
/// Captures runtime snapshots on a configured interval and publishes observability events.
/// </summary>
public sealed class RuntimeSnapshotHostedService : BackgroundService
{
    private readonly IRuntimeEventBus eventBus;
    private readonly IRuntimeStateStore stateStore;
    private readonly IRuntimeMetricsStore metricsStore;
    private readonly SystemMetricsCollector systemMetricsCollector;
    private readonly DockerStateCollector dockerStateCollector;
    private readonly IOptionsMonitor<ObservabilitySettings> observabilitySettings;
    private readonly ILogger<RuntimeSnapshotHostedService> logger;

    public RuntimeSnapshotHostedService(
        IRuntimeEventBus eventBus,
        IRuntimeStateStore stateStore,
        IRuntimeMetricsStore metricsStore,
        SystemMetricsCollector systemMetricsCollector,
        DockerStateCollector dockerStateCollector,
        IOptionsMonitor<ObservabilitySettings> observabilitySettings,
        ILogger<RuntimeSnapshotHostedService> logger)
    {
        this.eventBus = eventBus;
        this.stateStore = stateStore;
        this.metricsStore = metricsStore;
        this.systemMetricsCollector = systemMetricsCollector;
        this.dockerStateCollector = dockerStateCollector;
        this.observabilitySettings = observabilitySettings;
        this.logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (observabilitySettings.CurrentValue.Enabled)
            {
                try
                {
                    await CaptureSnapshotAsync(stoppingToken);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Runtime snapshot capture failed.");
                }
            }

            var interval = Math.Max(1, observabilitySettings.CurrentValue.MetricIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }

    internal async Task CaptureSnapshotAsync(CancellationToken cancellationToken)
    {
        var projection = await systemMetricsCollector.CollectAsync(cancellationToken);
        var docker = await dockerStateCollector.CollectAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var snapshot = new RuntimeStateDto
        {
            UpdatedAt = now,
            RuntimeStatus = "Running",
            HealthStatus = projection.Health.Status,
            HealthMessage = projection.Health.Message,
            CpuUsagePercent = projection.Statistics.CpuUsagePercent,
            MemoryUsagePercent = projection.Statistics.MemoryUsagePercent,
            DiskUsagePercent = projection.Statistics.DiskUsagePercent,
            DockerStatus = docker.Status,
            ContainerCount = docker.ContainerCount,
            RunningContainerCount = docker.RunningContainerCount,
            NetworkCount = docker.NetworkCount,
            VolumeCount = docker.VolumeCount,
            ImageCount = docker.ImageCount,
            RunningWorkflowCount = projection.RunningWorkflowCount,
            DeploymentQueueCount = projection.DeploymentQueueCount,
            ActiveReleaseCount = projection.ActiveReleaseCount,
            Capabilities = projection.Capabilities,
            Containers = docker.Containers,
            Networks = docker.Networks,
            Volumes = docker.Volumes,
            Releases = projection.Releases,
            Workflows = projection.Workflows
        };

        stateStore.UpdateSnapshot(snapshot);

        var metricsSnapshot = new RuntimeMetricsSnapshotDto
        {
            SnapshotId = Guid.NewGuid().ToString("N"),
            CapturedAt = now,
            CpuUsagePercent = snapshot.CpuUsagePercent,
            MemoryUsagePercent = snapshot.MemoryUsagePercent,
            DiskUsagePercent = snapshot.DiskUsagePercent,
            ContainerCount = snapshot.ContainerCount,
            RunningContainerCount = snapshot.RunningContainerCount,
            NetworkCount = snapshot.NetworkCount,
            VolumeCount = snapshot.VolumeCount,
            ImageCount = snapshot.ImageCount,
            RunningWorkflowCount = snapshot.RunningWorkflowCount,
            DeploymentQueueCount = snapshot.DeploymentQueueCount
        };

        metricsStore.Add(metricsSnapshot);

        await eventBus.PublishAsync(new RuntimePlatformEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            Timestamp = now,
            Category = RuntimeEventCategory.Runtime,
            EventType = "MetricsSnapshotCaptured",
            Message = "Runtime metrics snapshot captured.",
            Metadata = new Dictionary<string, string>
            {
                ["snapshotId"] = metricsSnapshot.SnapshotId
            }
        }, cancellationToken);

        await eventBus.PublishAsync(new RuntimePlatformEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            Timestamp = now,
            Category = RuntimeEventCategory.Health,
            EventType = "HealthChanged",
            Message = projection.Health.Message,
            Metadata = new Dictionary<string, string>
            {
                ["status"] = projection.Health.Status
            }
        }, cancellationToken);
    }
}

/// <summary>
/// Publishes runtime startup events when the observability platform boots.
/// </summary>
public sealed class RuntimeObservabilityBootstrapHostedService : IHostedService
{
    private readonly IRuntimeEventBus eventBus;
    private readonly IOptionsMonitor<ObservabilitySettings> observabilitySettings;

    public RuntimeObservabilityBootstrapHostedService(
        IRuntimeEventBus eventBus,
        IOptionsMonitor<ObservabilitySettings> observabilitySettings)
    {
        this.eventBus = eventBus;
        this.observabilitySettings = observabilitySettings;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!observabilitySettings.CurrentValue.Enabled)
        {
            return Task.CompletedTask;
        }

        return eventBus.PublishAsync(new RuntimePlatformEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            Category = RuntimeEventCategory.Runtime,
            EventType = "RuntimeStarted",
            Message = "AOVIAS observability platform started."
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
