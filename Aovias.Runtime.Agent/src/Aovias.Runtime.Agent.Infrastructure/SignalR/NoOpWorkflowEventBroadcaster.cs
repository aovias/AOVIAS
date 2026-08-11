using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Events;

namespace Aovias.Runtime.Agent.Infrastructure.SignalR;

/// <summary>
/// No-op workflow broadcaster used when SignalR is disabled but orchestration is enabled.
/// </summary>
public sealed class NoOpWorkflowEventBroadcaster : IWorkflowEventBroadcaster
{
    /// <inheritdoc />
    public Task PublishAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
