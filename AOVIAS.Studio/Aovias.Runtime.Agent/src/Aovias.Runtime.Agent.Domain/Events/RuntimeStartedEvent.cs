namespace Aovias.Runtime.Agent.Domain.Events;

/// <summary>
/// Domain event raised when the runtime control plane starts.
/// </summary>
public sealed class RuntimeStartedEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeStartedEvent"/> class.
    /// </summary>
    /// <param name="serverId">The server identifier.</param>
    /// <param name="startedAt">The start timestamp.</param>
    public RuntimeStartedEvent(string serverId, DateTimeOffset startedAt)
    {
        ServerId = serverId;
        StartedAt = startedAt;
    }

    /// <summary>
    /// Gets the server identifier.
    /// </summary>
    public string ServerId { get; }

    /// <summary>
    /// Gets the start timestamp.
    /// </summary>
    public DateTimeOffset StartedAt { get; }
}
