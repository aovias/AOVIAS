using System.Net.Http.Json;

namespace CommunicationService.Infrastructure.Clients;

public interface IGatewayRealtimeClient
{
    Task PublishInAppCreatedAsync(
        Guid personId,
        Guid notificationId,
        string title,
        string body,
        string moduleCode,
        CancellationToken ct = default);
}

/// <summary>
/// Notifies Gateway NotificationHub so WebApp SignalR clients receive in-app pushes.
/// No-ops when Gateway:BaseUrl is not configured (local unit tests / isolated runs).
/// </summary>
public sealed class GatewayRealtimeClient(HttpClient http, IConfiguration configuration, ILogger<GatewayRealtimeClient> logger) : IGatewayRealtimeClient
{
    public async Task PublishInAppCreatedAsync(
        Guid personId,
        Guid notificationId,
        string title,
        string body,
        string moduleCode,
        CancellationToken ct = default)
    {
        var baseUrl = configuration["Gateway:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogDebug("Gateway:BaseUrl missing; skipping SignalR broadcast for {NotificationId}", notificationId);
            return;
        }

        try
        {
            using var response = await http.PostAsJsonAsync(
                "/internal/notifications/broadcast",
                new
                {
                    personId,
                    notificationId,
                    title,
                    body,
                    moduleCode,
                },
                ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Gateway SignalR broadcast failed status={Status} notificationId={NotificationId}",
                    (int)response.StatusCode,
                    notificationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Gateway SignalR broadcast error notificationId={NotificationId}", notificationId);
        }
    }
}
