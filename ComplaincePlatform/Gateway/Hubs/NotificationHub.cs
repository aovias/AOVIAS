using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.Hubs;

/// <summary>
/// SignalR hub for real-time in-app notifications. Clients join a person-scoped group after connect.
/// CommunicationService calls Gateway <c>POST /internal/notifications/broadcast</c> after InApp create.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public const string HubPath = "/hubs/notifications";
    public const string ClientMethod = "notificationReceived";

    public override async Task OnConnectedAsync()
    {
        var personId = Context.User?.FindFirst(Shared.Infrastructure.Auth.AuthConstants.Claims.PersonId)?.Value;
        if (!string.IsNullOrWhiteSpace(personId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PersonGroup(personId));
        }

        await base.OnConnectedAsync();
    }

    public static string PersonGroup(string personId) => $"person:{personId}";
}
