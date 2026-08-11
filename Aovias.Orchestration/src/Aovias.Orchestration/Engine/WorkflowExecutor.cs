using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.Orchestration.Contracts.Events;
using Aovias.Orchestration.Contracts.Workflows;
using Aovias.Platform.Core;
using PlatformExecutionContext = Aovias.Platform.Core.ExecutionContext;
using Microsoft.Extensions.Logging;

namespace Aovias.Orchestration.Engine;

/// <summary>
/// Executes workflow pipelines step-by-step.
/// </summary>
public sealed class WorkflowExecutor : IWorkflowExecutor
{
    private readonly IEnumerable<IWorkflowStep> steps;
    private readonly InMemoryWorkflowStore store;
    private readonly WorkflowStateMachine stateMachine;
    private readonly IWorkflowEventBroadcaster broadcaster;
    private readonly IDeploymentWorkflowCoordinator? coordinator;
    private readonly ILogger<WorkflowExecutor> logger;

    public WorkflowExecutor(
        IEnumerable<IWorkflowStep> steps,
        InMemoryWorkflowStore store,
        WorkflowStateMachine stateMachine,
        IWorkflowEventBroadcaster broadcaster,
        ILogger<WorkflowExecutor> logger,
        IDeploymentWorkflowCoordinator? coordinator = null)
    {
        this.steps = steps;
        this.store = store;
        this.stateMachine = stateMachine;
        this.broadcaster = broadcaster;
        this.coordinator = coordinator;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> ExecuteAsync(
        IWorkflow workflow,
        IReadOnlyDictionary<string, string> parameters,
        PlatformExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        var workflowId = parameters.TryGetValue("workflowId", out var configuredWorkflowId) &&
                         !string.IsNullOrWhiteSpace(configuredWorkflowId)
            ? configuredWorkflowId
            : $"WF-{Guid.NewGuid():N}";
        var startedAt = DateTimeOffset.UtcNow;

        var instance = new WorkflowInstance
        {
            WorkflowId = workflowId,
            WorkflowName = workflow.WorkflowName,
            WorkflowType = workflow.WorkflowType,
            WorkflowVersion = workflow.WorkflowVersion,
            Steps = workflow.Steps,
            Status = WorkflowState.Created,
            CorrelationId = executionContext.Correlation.CorrelationId,
            StartedAt = startedAt,
            Parameters = parameters
        };

        store.Save(instance);
        await PublishAsync(instance, WorkflowEventType.WorkflowCreated, "Workflow created.", null, cancellationToken);

        instance = UpdateState(instance, WorkflowState.Queued);
        await PublishAsync(instance, WorkflowEventType.WorkflowQueued, "Workflow queued.", null, cancellationToken);

        if (coordinator is not null)
        {
            await coordinator.OnWorkflowStartedAsync(instance, cancellationToken);
        }

        instance = UpdateState(instance, WorkflowState.Validating);
        instance = UpdateState(instance, WorkflowState.Executing);

        var context = new WorkflowContext(instance, executionContext, parameters);
        var orderedSteps = ResolveSteps(workflow);
        var completed = 0;

        foreach (var step in orderedSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.UpdateInstance(context.Instance with { CurrentStep = step.Name });
            store.Save(context.Instance);
            await PublishAsync(context.Instance, WorkflowEventType.StepStarted, $"Step '{step.Name}' started.", step.Name, cancellationToken);

            var result = await step.ExecuteAsync(context, cancellationToken);
            if (!result.IsSuccess)
            {
                instance = context.Instance with
                {
                    Status = WorkflowState.Failed,
                    FailureReason = result.Message,
                    FinishedAt = DateTimeOffset.UtcNow,
                    Duration = DateTimeOffset.UtcNow - startedAt
                };
                store.Save(instance);
                await PublishAsync(instance, WorkflowEventType.StepFailed, result.Message, step.Name, cancellationToken);
                await PublishAsync(instance, WorkflowEventType.WorkflowFailed, result.Message, step.Name, cancellationToken);
                logger.LogWarning("Workflow {WorkflowId} failed at step {StepName}: {Message}", workflowId, step.Name, result.Message);

                if (coordinator is not null)
                {
                    await coordinator.OnWorkflowFailedAsync(instance, result.Message, cancellationToken);
                }

                return instance;
            }

            completed++;
            context.UpdateInstance(context.Instance with { CompletedSteps = completed });
            store.Save(context.Instance);
            await PublishAsync(context.Instance, WorkflowEventType.StepCompleted, result.Message, step.Name, cancellationToken);
        }

        instance = context.Instance with
        {
            Status = WorkflowState.Succeeded,
            CurrentStep = null,
            CompletedSteps = completed,
            FinishedAt = DateTimeOffset.UtcNow,
            Duration = DateTimeOffset.UtcNow - startedAt
        };
        instance = UpdateState(instance, WorkflowState.Completed);
        await PublishAsync(instance, WorkflowEventType.WorkflowCompleted, "Workflow completed successfully.", null, cancellationToken);
        logger.LogInformation("Workflow {WorkflowId} completed.", workflowId);

        if (coordinator is not null)
        {
            await coordinator.OnWorkflowCompletedAsync(instance, cancellationToken);
        }

        return instance;
    }

    private IEnumerable<IWorkflowStep> ResolveSteps(IWorkflow workflow)
    {
        var map = steps.ToDictionary(step => step.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var definition in workflow.Steps.OrderBy(step => step.Order))
        {
            if (map.TryGetValue(definition.Name, out var step))
            {
                yield return step;
            }
        }
    }

    private WorkflowInstance UpdateState(WorkflowInstance instance, WorkflowState target)
    {
        var next = stateMachine.Transition(instance.Status, target);
        instance = instance with { Status = next };
        store.Save(instance);
        return instance;
    }

    private async Task PublishAsync(
        WorkflowInstance instance,
        WorkflowEventType eventType,
        string message,
        string? stepName,
        CancellationToken cancellationToken)
    {
        var workflowEvent = new WorkflowEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = eventType,
            WorkflowId = instance.WorkflowId,
            CorrelationId = instance.CorrelationId,
            Timestamp = DateTimeOffset.UtcNow,
            StepName = stepName,
            Message = message
        };

        store.AppendEvent(workflowEvent);
        await broadcaster.PublishAsync(workflowEvent, cancellationToken);
    }
}
