namespace Aovias.Platform.Core;

/// <summary>
/// Base domain event for platform operations.
/// </summary>
public abstract record DomainEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString("N");

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public string CorrelationId { get; init; } = string.Empty;
}
