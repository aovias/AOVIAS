using Aovias.Orchestration.Contracts.Enums;

namespace Aovias.Orchestration.Contracts.Events;

/// <summary>
/// Immutable workflow event for the event stream.
/// </summary>
public sealed record WorkflowEvent
{
    public required string EventId { get; init; }

    public required WorkflowEventType EventType { get; init; }

    public required string WorkflowId { get; init; }

    public required string CorrelationId { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public string? StepName { get; init; }

    public required string Message { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
