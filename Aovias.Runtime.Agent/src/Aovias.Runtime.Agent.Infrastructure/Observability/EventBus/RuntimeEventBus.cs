using Aovias.Runtime.Agent.Application.Interfaces;

namespace Aovias.Runtime.Agent.Infrastructure.Observability.EventBus;

/// <summary>
/// In-process event bus for observability platform events.
/// </summary>
public sealed class RuntimeEventBus : IRuntimeEventBus
{
    private readonly object sync = new();
    private readonly List<Func<RuntimePlatformEvent, CancellationToken, Task>> handlers = [];

    /// <inheritdoc />
    public void Subscribe(Func<RuntimePlatformEvent, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (sync)
        {
            handlers.Add(handler);
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync(RuntimePlatformEvent platformEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(platformEvent);
        List<Func<RuntimePlatformEvent, CancellationToken, Task>> snapshot;
        lock (sync)
        {
            snapshot = handlers.ToList();
        }

        foreach (var handler in snapshot)
        {
            await handler(platformEvent, cancellationToken);
        }
    }
}
