using Aovias.Orchestration.Contracts.Enums;
using Aovias.Platform.Core.StateMachine;

namespace Aovias.Orchestration.Engine;

/// <summary>
/// Workflow lifecycle state machine.
/// </summary>
public sealed class WorkflowStateMachine
{
    private static readonly StateMachine<WorkflowState> Machine = new(
        new Dictionary<WorkflowState, IReadOnlyCollection<WorkflowState>>
        {
            [WorkflowState.Created] = [WorkflowState.Queued, WorkflowState.Cancelled],
            [WorkflowState.Queued] = [WorkflowState.Validating, WorkflowState.Cancelled],
            [WorkflowState.Validating] = [WorkflowState.Executing, WorkflowState.Failed, WorkflowState.Cancelled],
            [WorkflowState.Executing] = [WorkflowState.Waiting, WorkflowState.Succeeded, WorkflowState.Failed, WorkflowState.RollingBack, WorkflowState.Cancelled],
            [WorkflowState.Waiting] = [WorkflowState.Executing, WorkflowState.Succeeded, WorkflowState.Failed, WorkflowState.Cancelled],
            [WorkflowState.Succeeded] = [WorkflowState.Completed],
            [WorkflowState.Failed] = [WorkflowState.RollingBack, WorkflowState.Completed],
            [WorkflowState.RollingBack] = [WorkflowState.Completed, WorkflowState.Failed],
            [WorkflowState.Cancelled] = [WorkflowState.Completed],
            [WorkflowState.Completed] = []
        });

    public WorkflowState Transition(WorkflowState current, WorkflowState target) => Machine.Transition(current, target);

    public bool CanTransition(WorkflowState current, WorkflowState target) => Machine.CanTransition(current, target);
}
