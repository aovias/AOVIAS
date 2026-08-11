using Aovias.Orchestration.Contracts.Events;

namespace Aovias.Orchestration.Contracts;

/// <summary>
/// Publishes workflow events to real-time subscribers.
/// </summary>
public interface IWorkflowEventBroadcaster
{
    Task PublishAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default);
}
