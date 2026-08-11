using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/security")]
[Authorize]
public class SecurityController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("events")]
    public Task<IActionResult> ListEvents([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? userId = null, CancellationToken ct = default)
        => mediator.Send(new ListSecurityEventsQuery(V(), page, pageSize, userId), ct);

    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard(CancellationToken ct = default)
        => mediator.Send(new GetSecurityDashboardQuery(V()), ct);

    [HttpGet("risk")]
    public Task<IActionResult> Risk([FromQuery] Guid? userId = null, CancellationToken ct = default)
        => mediator.Send(new GetUserRiskQuery(V(), userId), ct);

    [HttpGet("devices")]
    public Task<IActionResult> Devices([FromQuery] Guid? userId = null, CancellationToken ct = default)
        => mediator.Send(new ListTrustedDevicesQuery(V(), userId), ct);

    [HttpGet("timeline")]
    public Task<IActionResult> Timeline([FromQuery] Guid? userId = null, [FromQuery] int take = 100, CancellationToken ct = default)
        => mediator.Send(new GetSecurityTimelineQuery(V(), userId, take), ct);

    [HttpPost("policies")]
    public Task<IActionResult> UpdatePolicy([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new UpdateSecurityPolicyCommand(V(), body), ct);

    [HttpPost("device/revoke")]
    public Task<IActionResult> RevokeDevice([FromBody] object body, [FromQuery] Guid? userId = null, CancellationToken ct = default)
        => mediator.Send(new RevokeTrustedDeviceCommand(V(), body, userId), ct);

    [HttpPost("session/revoke")]
    public Task<IActionResult> RevokeSession([FromBody] object body, [FromQuery] Guid? userId = null, CancellationToken ct = default)
        => mediator.Send(new RevokeSecuritySessionCommand(V(), body, userId), ct);
}
