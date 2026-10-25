using Aovias.Orchestration.Contracts.Enums;
using Aovias.Orchestration.Contracts.Workflows;

namespace Aovias.Orchestration.Contracts;

/// <summary>
/// Coordinates release record state with workflow execution outcomes.
/// </summary>
public interface IDeploymentWorkflowCoordinator
{
    Task OnWorkflowStartedAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);

    Task OnWorkflowCompletedAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);

    Task OnWorkflowFailedAsync(WorkflowInstance instance, string reason, CancellationToken cancellationToken = default);

    Task OnRollbackTriggeredAsync(WorkflowInstance instance, string reason, CancellationToken cancellationToken = default);
}
