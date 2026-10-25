using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;
using Shared.Infrastructure.Auth;
using System.Security.Claims;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/identity/users")]
[Authorize]
public class IdentityUsersController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
    private Guid ActorUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    private Guid? ResolveOrganizationId()
    {
        if (User.IsInRole(ApplicationRoles.SUPER_ADMIN))
        {
            var queryOrg = Request.Query["organizationId"].FirstOrDefault();
            if (Guid.TryParse(queryOrg, out var routeId)) return routeId;
        }
        var claim = User.FindFirst("organization_id")?.Value;
        return Guid.TryParse(claim, out var orgId) ? orgId : null;
    }

    [HttpGet]
    [RequirePermission("Users.View")]
    public Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? membershipStatus = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Task.FromResult<IActionResult>(Forbid());
        return mediator.Send(new ListIdentityUsersAdminQuery(V(), orgId.Value, ActorUserId(), page, pageSize, search, role, departmentId, membershipStatus, includeInactive), ct);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Users.View")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Task.FromResult<IActionResult>(Forbid());
        return mediator.Send(new GetIdentityUserAdminQuery(V(), orgId.Value, id, ActorUserId()), ct);
    }

    [HttpPost]
    [RequirePermission("Users.Create")]
    public Task<IActionResult> Create([FromBody] DTOs.CreateIdentityUserAdminRequest body, CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Task.FromResult<IActionResult>(Forbid());
        return mediator.Send(new CreateIdentityUserAdminCommand(V(), orgId.Value, ActorUserId(), body), ct);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Users.Update")]
    public Task<IActionResult> Update(Guid id, [FromBody] DTOs.UpdateIdentityUserAdminRequest body, CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Task.FromResult<IActionResult>(Forbid());
        return mediator.Send(new UpdateIdentityUserAdminCommand(V(), orgId.Value, id, ActorUserId(), body), ct);
    }

    [HttpPost("{id:guid}/suspend")]
    [RequirePermission("Users.Disable")]
    public Task<IActionResult> Suspend(Guid id, CancellationToken ct)
        => mediator.Send(new SuspendIdentityUserAdminCommand(V(), id, ActorUserId()), ct);

    [HttpPost("{id:guid}/activate")]
    [RequirePermission("Users.Update")]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => mediator.Send(new ActivateIdentityUserAdminCommand(V(), id, ActorUserId()), ct);

    [HttpPost("{id:guid}/disable")]
    [RequirePermission("Users.Disable")]
    public Task<IActionResult> Disable(Guid id, CancellationToken ct)
        => mediator.Send(new DisableIdentityUserAdminCommand(V(), id, ActorUserId()), ct);

    [HttpDelete("{id:guid}")]
    [RequirePermission("Users.Delete")]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Task.FromResult<IActionResult>(Forbid());
        return mediator.Send(new DeleteIdentityUserAdminCommand(V(), orgId.Value, id, ActorUserId()), ct);
    }

    [HttpPost("{id:guid}/unlock")]
    [RequirePermission("Users.Update")]
    public Task<IActionResult> Unlock(Guid id, CancellationToken ct)
        => mediator.Send(new UnlockIdentityUserAdminCommand(V(), id, ActorUserId()), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/identity/password-management")]
[Authorize]
public class IdentityPasswordManagementController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
    private Guid ActorUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpPost("{userId:guid}/reset")]
    [RequirePermission("Users.ResetPassword")]
    public Task<IActionResult> ResetPassword(Guid userId, CancellationToken ct)
        => mediator.Send(new AdminResetPasswordCommand(V(), userId, ActorUserId()), ct);

    [HttpPost("{userId:guid}/force-change")]
    [RequirePermission("PasswordManagement.Manage")]
    public Task<IActionResult> ForcePasswordChange(Guid userId, CancellationToken ct)
        => mediator.Send(new ForcePasswordChangeIdentityCommand(V(), userId, ActorUserId()), ct);

    [HttpPost("{userId:guid}/unlock")]
    [RequirePermission("PasswordManagement.Manage")]
    public Task<IActionResult> Unlock(Guid userId, CancellationToken ct)
        => mediator.Send(new UnlockIdentityUserAdminCommand(V(), userId, ActorUserId()), ct);

    [HttpPost("expire")]
    public Task<IActionResult> Expire(CancellationToken ct)
        => mediator.Send(new PlaceholderPasswordManagementCommand(V(), "Expire Password"), ct);

    [HttpPost("bulk-reset")]
    public Task<IActionResult> BulkReset(CancellationToken ct)
        => mediator.Send(new PlaceholderPasswordManagementCommand(V(), "Bulk Password Reset"), ct);
}
