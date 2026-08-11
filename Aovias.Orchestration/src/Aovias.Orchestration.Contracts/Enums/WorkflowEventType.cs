namespace Aovias.Orchestration.Contracts.Enums;

/// <summary>
/// Immutable workflow event types for the event stream.
/// </summary>
public enum WorkflowEventType
{
    WorkflowCreated = 0,
    WorkflowQueued = 1,
    StepStarted = 2,
    StepCompleted = 3,
    StepFailed = 4,
    WorkflowCompleted = 5,
    WorkflowFailed = 6,
    WorkflowCancelled = 7
}
