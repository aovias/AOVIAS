namespace Aovias.Orchestration.Contracts.Enums;

/// <summary>
/// Workflow lifecycle states.
/// </summary>
public enum WorkflowState
{
    Created = 0,
    Queued = 1,
    Validating = 2,
    Executing = 3,
    Waiting = 4,
    Succeeded = 5,
    Failed = 6,
    RollingBack = 7,
    Cancelled = 8,
    Completed = 9
}
