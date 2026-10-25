namespace Aovias.Runtime.Contracts.Observability;

/// <summary>
/// Immutable runtime timeline entry.
/// </summary>
public sealed class RuntimeTimelineEntryDto
{
    public required string EntryId { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public required string Category { get; init; }

    public required string EventType { get; init; }

    public required string Message { get; init; }

    public string? CorrelationId { get; init; }

    public string? OperationId { get; init; }

    public string? ContainerName { get; init; }

    public string? WorkflowId { get; init; }

    public string? ReleaseId { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
