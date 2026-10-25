using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Observability;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aovias.Runtime.Agent.Infrastructure.Observability.Services;

/// <summary>
/// Projects platform events into timeline and event store.
/// </summary>
public sealed class RuntimeStateProjectionHandler : IHostedService
{
    private readonly IRuntimeEventBus eventBus;
    private readonly IRuntimeTimelineStore timelineStore;
    private readonly IRuntimeEventStore eventStore;
    private readonly ILogger<RuntimeStateProjectionHandler> logger;

    public RuntimeStateProjectionHandler(
        IRuntimeEventBus eventBus,
        IRuntimeTimelineStore timelineStore,
        IRuntimeEventStore eventStore,
        ILogger<RuntimeStateProjectionHandler> logger)
    {
        this.eventBus = eventBus;
        this.timelineStore = timelineStore;
        this.eventStore = eventStore;
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventBus.Subscribe(HandleAsync);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private Task HandleAsync(RuntimePlatformEvent platformEvent, CancellationToken cancellationToken)
    {
        try
        {
            eventStore.Append(platformEvent);
            timelineStore.Append(new RuntimeTimelineEntryDto
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
            });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to project observability event {EventType}", platformEvent.EventType);
        }

        return Task.CompletedTask;
    }
}
