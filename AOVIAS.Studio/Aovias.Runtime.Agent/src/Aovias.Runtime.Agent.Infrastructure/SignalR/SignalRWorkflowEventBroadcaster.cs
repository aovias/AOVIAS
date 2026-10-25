using Aovias.Orchestration.Contracts.Events;
using Aovias.Orchestration.Contracts.Workflows;
using Aovias.Orchestration.Engine;
using Aovias.Runtime.Agent.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Aovias.Runtime.Agent.Infrastructure.SignalR;

/// <summary>
/// SignalR workflow event broadcaster.
/// </summary>
public sealed class SignalRWorkflowEventBroadcaster : Aovias.Orchestration.Contracts.IWorkflowEventBroadcaster
{
    private readonly IHubContext<RuntimeHub> hubContext;
    private readonly InMemoryWorkflowStore workflowStore;
    private readonly IRuntimeEventBus eventBus;

    public SignalRWorkflowEventBroadcaster(
        IHubContext<RuntimeHub> hubContext,
        InMemoryWorkflowStore workflowStore,
        IRuntimeEventBus eventBus)
    {
        this.hubContext = hubContext;
        this.workflowStore = workflowStore;
        this.eventBus = eventBus;
    }

    /// <inheritdoc />
    public async Task PublishAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default)
    {
        var instance = workflowStore.Get(workflowEvent.WorkflowId);
        var releaseId = instance?.Parameters.GetValueOrDefault("releaseId");

        await hubContext.Clients.All.SendAsync("WorkflowEvent", workflowEvent, cancellationToken);

        var progress = new WorkflowProgressMessage
        {
            WorkflowId = workflowEvent.WorkflowId,
            ReleaseId = releaseId,
            EventType = workflowEvent.EventType.ToString(),
            StepName = workflowEvent.StepName,
            Message = workflowEvent.Message,
            Timestamp = workflowEvent.Timestamp,
            CurrentStep = instance?.CurrentStep,
            CompletedSteps = instance?.CompletedSteps ?? 0,
            TotalSteps = instance?.Steps.Count ?? 0,
            RollbackStatus = instance?.WorkflowType.ToString() == "Rollback"
                ? workflowEvent.EventType.ToString()
                : null
        };

        await hubContext.Clients.All.SendAsync("WorkflowProgress", progress, cancellationToken);

        if (!string.IsNullOrWhiteSpace(releaseId))
        {
            await hubContext.Clients.All.SendAsync("DeploymentProgress", progress, cancellationToken);
        }

        if (instance?.WorkflowType.ToString() == "Rollback")
        {
            await hubContext.Clients.All.SendAsync("RollbackProgress", progress, cancellationToken);
        }

        var category = !string.IsNullOrWhiteSpace(releaseId)
            ? RuntimeEventCategory.Deployment
            : RuntimeEventCategory.Workflow;

        await eventBus.PublishAsync(new RuntimePlatformEvent
        {
            EventId = workflowEvent.EventId,
            Timestamp = workflowEvent.Timestamp,
            Category = category,
            EventType = MapWorkflowEventType(workflowEvent.EventType.ToString()),
            Message = workflowEvent.Message,
            CorrelationId = workflowEvent.CorrelationId,
            WorkflowId = workflowEvent.WorkflowId,
            ReleaseId = releaseId,
            Metadata = new Dictionary<string, string>
            {
                ["stepName"] = workflowEvent.StepName ?? string.Empty
            }
        }, cancellationToken);
    }

    private static string MapWorkflowEventType(string eventType)
        => eventType switch
        {
            "WorkflowCreated" or "WorkflowQueued" => "WorkflowStarted",
            "WorkflowCompleted" => "DeploymentCompleted",
            "WorkflowFailed" => "PipelineStepFailed",
            "StepStarted" => "PipelineStepStarted",
            "StepCompleted" => "PipelineStepCompleted",
            "StepFailed" => "PipelineStepFailed",
            _ => eventType
        };
}

/// <summary>
/// Workflow progress payload for SignalR clients.
/// </summary>
public sealed class WorkflowProgressMessage
{
    public required string WorkflowId { get; init; }

    public string? ReleaseId { get; init; }

    public required string EventType { get; init; }

    public string? StepName { get; init; }

    public required string Message { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public string? CurrentStep { get; init; }

    public int CompletedSteps { get; init; }

    public int TotalSteps { get; init; }

    public string? RollbackStatus { get; init; }
}
