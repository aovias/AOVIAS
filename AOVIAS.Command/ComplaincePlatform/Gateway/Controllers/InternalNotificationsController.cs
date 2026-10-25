using Gateway.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

public sealed class NotificationBroadcastRequest
{
    public Guid PersonId { get; init; }
    public Guid NotificationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string ModuleCode { get; init; } = "Communication";
}

/// <summary>
/// Internal endpoint used by CommunicationService after creating an in-app notification
/// so Gateway can push to the person-scoped SignalR group.
/// </summary>
[ApiController]
[Route("internal/notifications")]
public sealed class InternalNotificationsController(IHubContext<NotificationHub> hub) : ControllerBase
{
    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] NotificationBroadcastRequest request, CancellationToken ct = default)
    {
        if (!InternalApiAuthorizationExtensions.ValidateInternalApiKey(Request, HttpContext.RequestServices))
            return Unauthorized();

        if (request.PersonId == Guid.Empty)
            return BadRequest(ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));

        var payload = new
        {
            id = request.NotificationId,
            title = request.Title,
            body = request.Body,
            moduleCode = request.ModuleCode,
            personId = request.PersonId,
        };

        await hub.Clients
            .Group(NotificationHub.PersonGroup(request.PersonId.ToString()))
            .SendAsync("notificationReceived", payload, ct);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { broadcast = true }, HttpContext.TraceIdentifier));
    }
}
