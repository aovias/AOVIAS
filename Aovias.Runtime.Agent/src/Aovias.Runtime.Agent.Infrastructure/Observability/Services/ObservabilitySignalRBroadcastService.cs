using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Observability;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Aovias.Runtime.Agent.Infrastructure.SignalR;

namespace Aovias.Runtime.Agent.Infrastructure.Observability.Services;

/// <summary>
/// Broadcasts observability platform events over SignalR without client polling.
/// </summary>
public sealed class ObservabilitySignalRBroadcastService : IHostedService
{
    private readonly IRuntimeEventBus eventBus;
    private readonly IRuntimeStateStore stateStore;
    private readonly IRuntimeMetricsStore metricsStore;
    private readonly IHubContext<RuntimeHub> hubContext;
    private readonly IOptionsMonitor<SignalRSettings> signalRSettings;
    private readonly IOptionsMonitor<ObservabilitySettings> observabilitySettings;

    public ObservabilitySignalRBroadcastService(
        IRuntimeEventBus eventBus,
        IRuntimeStateStore stateStore,
        IRuntimeMetricsStore metricsStore,
        IHubContext<RuntimeHub> hubContext,
        IOptionsMonitor<SignalRSettings> signalRSettings,
        IOptionsMonitor<ObservabilitySettings> observabilitySettings)
    {
        this.eventBus = eventBus;
        this.stateStore = stateStore;
        this.metricsStore = metricsStore;
        this.hubContext = hubContext;
        this.signalRSettings = signalRSettings;
        this.observabilitySettings = observabilitySettings;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventBus.Subscribe(BroadcastAsync);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task BroadcastAsync(RuntimePlatformEvent platformEvent, CancellationToken cancellationToken)
    {
        if (!signalRSettings.CurrentValue.Enabled || !observabilitySettings.CurrentValue.Enabled)
        {
            return;
        }

        var timelineEntry = new RuntimeTimelineEntryDto
        {
            EntryId = platformEvent.EventId,
            Timestamp = platformEvent.Timestamp,
            Category = platformEvent.Category,
            EventType = platformEvent.EventType,
            Message = platformEvent.Message,
            CorrelationId = platformEvent.CorrelationId,
            OperationId = platformEvent.OperationId,
            ContainerName = platformEvent.ContainerName,
            WorkflowId = platformEvent.WorkflowId,
            ReleaseId = platformEvent.ReleaseId,
            Metadata = platformEvent.Metadata
        };

        await hubContext.Clients.All.SendAsync("RuntimeTimeline", timelineEntry, cancellationToken);

        if (platformEvent.EventType is "MetricsSnapshotCaptured" or "HealthChanged" or "RuntimeStarted")
        {
            await hubContext.Clients.All.SendAsync("RuntimeState", stateStore.GetSnapshot(), cancellationToken);
            var latestMetrics = metricsStore.GetLatest();
            if (latestMetrics is not null)
            {
                await hubContext.Clients.All.SendAsync("RuntimeMetrics", latestMetrics, cancellationToken);
            }
        }

        if (platformEvent.Category == RuntimeEventCategory.Docker)
        {
            await hubContext.Clients.All.SendAsync("ContainerEvent", timelineEntry, cancellationToken);
            await hubContext.Clients.All.SendAsync("RuntimeState", stateStore.GetSnapshot(), cancellationToken);
        }

        if (platformEvent.Category is RuntimeEventCategory.Workflow or RuntimeEventCategory.Deployment)
        {
            await hubContext.Clients.All.SendAsync("WorkflowUpdate", timelineEntry, cancellationToken);
            await hubContext.Clients.All.SendAsync("DeploymentUpdate", timelineEntry, cancellationToken);
            await hubContext.Clients.All.SendAsync("RuntimeState", stateStore.GetSnapshot(), cancellationToken);
        }

        if (platformEvent.Category == RuntimeEventCategory.Health)
        {
            await hubContext.Clients.All.SendAsync(
                "RuntimeHealth",
                new
                {
                    Status = platformEvent.Metadata.GetValueOrDefault("status") ?? "Unknown",
                    Message = platformEvent.Message,
                    Timestamp = platformEvent.Timestamp
                },
                cancellationToken);
        }
    }
}
