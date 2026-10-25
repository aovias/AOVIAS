using Aovias.Runtime.Agent.Application.Interfaces;

namespace Aovias.Runtime.Agent.Infrastructure.Observability.EventStore;

/// <summary>
/// In-memory raw platform event store.
/// </summary>
public sealed class InMemoryRuntimeEventStore : IRuntimeEventStore
{
    private readonly object sync = new();
    private readonly LinkedList<RuntimePlatformEvent> events = new();

    /// <inheritdoc />
    public void Append(RuntimePlatformEvent platformEvent)
    {
        lock (sync)
        {
            events.AddFirst(platformEvent);
            while (events.Count > 5000)
            {
                events.RemoveLast();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RuntimePlatformEvent> GetRecent(int limit = 200)
    {
        lock (sync)
        {
            return events.Take(Math.Max(1, limit)).ToList();
        }
    }
}
