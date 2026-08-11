using IdentityService.Api;
using IdentityService.Application.Security.Commands;
using IdentityService.Application.Security.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using System.Security.Claims;

namespace IdentityService.Controllers.V1;

[ApiController]
[Route("api/v1/security")]
[Authorize]
public class SecurityController(IMediator mediator) : ControllerBase
{
    private Guid ActorUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    private bool IsAdmin() => User.IsInRole(ApplicationRoles.SUPER_ADMIN)
        || User.IsInRole(ApplicationRoles.ORG_ADMIN)
        || User.IsInRole(ApplicationRoles.GROUP_ADMIN)
        || User.IsInRole(ApplicationRoles.REGULATORY_ADMIN);

    private string RequestId() =>
        Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? HttpContext.TraceIdentifier;

    [HttpGet("events")]
    public async Task<IActionResult> ListEvents([FromQuery] Guid? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var data = await mediator.Send(new ListSecurityEventsQuery(ActorUserId(), IsAdmin(), userId, page, pageSize), ct);
            return Ok(AuthApiResponses.Success(data, RequestId()));
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.FORBIDDEN, ex.Message, RequestId());
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct = default)
    {
        try
        {
            var data = await mediator.Send(new GetSecurityDashboardQuery(ActorUserId(), IsAdmin()), ct);
            return Ok(AuthApiResponses.Success(data, RequestId()));
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.FORBIDDEN, ex.Message, RequestId());
        }
    }

    [HttpGet("risk")]
    public async Task<IActionResult> Risk([FromQuery] Guid? userId, CancellationToken ct = default)
    {
        try
        {
            var data = await mediator.Send(new GetUserRiskQuery(ActorUserId(), IsAdmin(), userId), ct);
            if (data == null)
                return AuthApiResponses.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, "Risk score not found", RequestId());
            return Ok(AuthApiResponses.Success(data, RequestId()));
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.FORBIDDEN, ex.Message, RequestId());
        }
    }

    [HttpGet("devices")]
    public async Task<IActionResult> Devices([FromQuery] Guid? userId, CancellationToken ct = default)
    {
        try
        {
            var data = await mediator.Send(new ListTrustedDevicesQuery(ActorUserId(), IsAdmin(), userId), ct);
            return Ok(AuthApiResponses.Success(data, RequestId()));
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.FORBIDDEN, ex.Message, RequestId());
        }
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> Timeline([FromQuery] Guid? userId, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        try
        {
            var data = await mediator.Send(new GetSecurityTimelineQuery(ActorUserId(), IsAdmin(), userId, take), ct);
            return Ok(AuthApiResponses.Success(data, RequestId()));
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.FORBIDDEN, ex.Message, RequestId());
        }
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int take = 200, CancellationToken ct = default)
    {
        try
        {
            var data = await mediator.Send(new ListIdentityAuditLogsQuery(ActorUserId(), IsAdmin(), from, to, take), ct);
            return Ok(AuthApiResponses.Success(data, RequestId()));
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.FORBIDDEN, ex.Message, RequestId());
        }
    }

    [HttpPost("policies")]
    public async Task<IActionResult> UpdatePolicy([FromBody] Application.Contracts.Security.UpdateSecurityPolicyRequest request, CancellationToken ct = default)
    {
        try
        {
            var data = await mediator.Send(new UpdateSecurityPolicyCommand(ActorUserId(), IsAdmin(), request), ct);
            return Ok(AuthApiResponses.Success(data, RequestId()));
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.FORBIDDEN, ex.Message, RequestId());
        }
    }

    [HttpPost("device/revoke")]
    public async Task<IActionResult> RevokeDevice([FromBody] Application.Contracts.Security.RevokeDeviceRequest request, [FromQuery] Guid? userId, CancellationToken ct = default)
    {
        try
        {
            await mediator.Send(new RevokeTrustedDeviceCommand(ActorUserId(), IsAdmin(), userId, request), ct);
            return Ok(AuthApiResponses.Success(new { message = "Device revoked" }, RequestId()));
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.FORBIDDEN, ex.Message, RequestId());
        }
        catch (InvalidOperationException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, ex.Message, RequestId());
        }
    }

    [HttpPost("session/revoke")]
    public async Task<IActionResult> RevokeSession([FromBody] Application.Contracts.Security.RevokeSecuritySessionRequest request, [FromQuery] Guid? userId, CancellationToken ct = default)
    {
        try
        {
            await mediator.Send(new RevokeSecuritySessionCommand(ActorUserId(), IsAdmin(), userId, request), ct);
            return Ok(AuthApiResponses.Success(new { message = "Session revoked" }, RequestId()));
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.FORBIDDEN, ex.Message, RequestId());
        }
        catch (InvalidOperationException ex)
        {
            return AuthApiResponses.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, ex.Message, RequestId());
        }
    }
}
