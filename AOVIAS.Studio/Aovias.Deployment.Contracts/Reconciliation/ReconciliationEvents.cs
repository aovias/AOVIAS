namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>The structured event vocabulary the reconciliation lifecycle emits (Sprint M2.6 D10) — mirrors Sprint M2.5's <c>ExecutionEventType</c> discipline: there is no unstructured logging path anywhere in this library.</summary>
public enum ReconciliationEventType
{
    ReconciliationStarted,
    DriftDetected,
    DriftResolved,
    RepairStarted,
    RepairCompleted,
    RepairRejected,
    OperatorApprovalRequired,
    ReconciliationCompleted
}

/// <summary>One structured reconciliation event (Sprint M2.6 D10).</summary>
public sealed class ReconciliationEvent
{
    public required ReconciliationEventType Type { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    public required string EnvironmentId { get; init; }

    public required string ServiceId { get; init; }

    public required string CorrelationId { get; init; }

    public string? TaskId { get; init; }

    public required string Message { get; init; }
}
