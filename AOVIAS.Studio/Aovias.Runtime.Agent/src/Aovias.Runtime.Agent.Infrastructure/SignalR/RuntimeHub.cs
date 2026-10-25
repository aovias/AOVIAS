using Microsoft.AspNetCore.SignalR;

namespace Aovias.Runtime.Agent.Infrastructure.SignalR;

/// <summary>
/// Real-time runtime and workflow event hub.
/// </summary>
public sealed class RuntimeHub : Hub
{
    /// <summary>
    /// Subscribes the caller to runtime event streams.
    /// </summary>
    public Task SubscribeAsync()
        => Groups.AddToGroupAsync(Context.ConnectionId, "runtime-clients");

    /// <summary>
    /// Unsubscribes the caller from runtime event streams.
    /// </summary>
    public Task UnsubscribeAsync()
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, "runtime-clients");
}
