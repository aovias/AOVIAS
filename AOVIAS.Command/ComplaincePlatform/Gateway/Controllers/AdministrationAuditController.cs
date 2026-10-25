using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/administration/audit")]
[Authorize]
public class AdministrationAuditController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    private Guid? ResolveOrganizationId()
    {
        if (User.IsInRole(ApplicationRoles.SUPER_ADMIN))
        {
            var queryOrg = Request.Query["organizationId"].FirstOrDefault();
            if (Guid.TryParse(queryOrg, out var orgId)) return orgId;
        }

        var claim = User.FindFirst("organization_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet("activity")]
    [RequirePermission("Audit.View")]
    public Task<IActionResult> Activity(
        [FromQuery] string? search,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? module,
        [FromQuery] string? category,
        [FromQuery] string? action,
        [FromQuery] string? userId,
        [FromQuery] string? status,
        [FromQuery] string? service,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Task.FromResult<IActionResult>(Forbid());

        return mediator.Send(new GetPlatformActivityQuery(
            V(), orgId.Value, search, from, to, module, category, action, userId, status, service, sort, page, pageSize), ct);
    }
}
