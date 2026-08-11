using Aovias.Runtime.Contracts.Observability;

namespace Aovias.Runtime.Agent.Application.Interfaces;

/// <summary>
/// Single source of truth for current runtime observability state.
/// </summary>
public interface IRuntimeStateStore
{
    RuntimeStateDto GetSnapshot();

    void UpdateSnapshot(RuntimeStateDto snapshot);
}

/// <summary>
/// Internal event bus for observability subsystems.
/// </summary>
public interface IRuntimeEventBus
{
    void Subscribe(Func<RuntimePlatformEvent, CancellationToken, Task> handler);

    Task PublishAsync(RuntimePlatformEvent platformEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Immutable runtime timeline store.
/// </summary>
public interface IRuntimeTimelineStore
{
    void Append(RuntimeTimelineEntryDto entry);

    IReadOnlyList<RuntimeTimelineEntryDto> Query(
        string? searchTerm = null,
        string? category = null,
        int limit = 100);
}

/// <summary>
/// In-memory metrics snapshot history.
/// </summary>
public interface IRuntimeMetricsStore
{
    void Add(RuntimeMetricsSnapshotDto snapshot);

    IReadOnlyList<RuntimeMetricsSnapshotDto> GetRecent(int limit = 60);

    RuntimeMetricsSnapshotDto? GetLatest();
}

/// <summary>
/// Raw platform event retention store.
/// </summary>
public interface IRuntimeEventStore
{
    void Append(RuntimePlatformEvent platformEvent);

    IReadOnlyList<RuntimePlatformEvent> GetRecent(int limit = 200);
}

/// <summary>
/// Platform event published on the internal observability bus.
/// </summary>
public sealed class RuntimePlatformEvent
{
    public required string EventId { get; init; }

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

/// <summary>
/// Well-known observability event categories.
/// </summary>
public static class RuntimeEventCategory
{
    public const string Runtime = "Runtime";
    public const string Docker = "Docker";
    public const string Workflow = "Workflow";
    public const string Deployment = "Deployment";
    public const string Health = "Health";
    public const string Configuration = "Configuration";
    public const string SignalR = "SignalR";
}
