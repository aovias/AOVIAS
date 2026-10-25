using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize]
public class RolesController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => mediator.Send(new ListRolesQuery(V(), page, pageSize), ct);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct = default)
        => mediator.Send(new GetRoleByIdQuery(V(), id), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateRoleCommand(V(), body), ct);

    [HttpPost("{roleId:guid}/permissions/{permissionId:guid}")]
    public Task<IActionResult> GrantPermission(Guid roleId, Guid permissionId, CancellationToken ct = default)
        => mediator.Send(new GrantPermissionToRoleCommand(V(), roleId, permissionId), ct);

    [HttpPost("users/{userId:guid}/assign/{roleId:guid}")]
    public Task<IActionResult> Assign(Guid userId, Guid roleId, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new AssignRoleCommand(V(), userId, roleId, body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/permissions")]
[Authorize]
public class PermissionsController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct = default) => mediator.Send(new ListPermissionsQuery(V()), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreatePermissionCommand(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/policies")]
[Authorize]
public class PoliciesController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct = default) => mediator.Send(new ListPoliciesQuery(V()), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreatePolicyCommand(V(), body), ct);

    [HttpPost("{id:guid}/publish")]
    public Task<IActionResult> Publish(Guid id, CancellationToken ct = default)
        => mediator.Send(new PublishPolicyCommand(V(), id), ct);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct = default)
        => mediator.Send(new GetPolicyByIdQuery(V(), id), ct);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new UpdatePolicyCommand(V(), id, body), ct);

    [HttpPost("{id:guid}/archive")]
    public Task<IActionResult> Archive(Guid id, CancellationToken ct = default)
        => mediator.Send(new ArchivePolicyCommand(V(), id), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/role-assignments")]
[Authorize]
public class RoleAssignmentsController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List([FromQuery] Guid? userId, CancellationToken ct = default)
        => mediator.Send(new ListRoleAssignmentsQuery(V(), userId), ct);

    [HttpPost]
    public Task<IActionResult> Assign([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new AssignRoleAssignmentCommand(V(), body), ct);

    [HttpPost("{id:guid}/revoke")]
    public Task<IActionResult> Revoke(Guid id, CancellationToken ct = default)
        => mediator.Send(new RevokeRoleAssignmentCommand(V(), id), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/delegations")]
[Authorize]
public class DelegationsController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct = default) => mediator.Send(new ListDelegationsQuery(V()), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateDelegationCommand(V(), body), ct);

    [HttpPost("{id:guid}/revoke")]
    public Task<IActionResult> Revoke(Guid id, CancellationToken ct = default)
        => mediator.Send(new RevokeDelegationCommand(V(), id), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/responsibilities")]
[Authorize]
public class ResponsibilitiesController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => mediator.Send(new ListGovernanceResponsibilitiesQuery(V(), page, pageSize), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateGovernanceResponsibilityCommand(V(), body), ct);

    [HttpPost("{id:guid}/assign")]
    public Task<IActionResult> Assign(Guid id, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new AssignGovernanceResponsibilityCommand(V(), id, body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/approval-authorities")]
[Authorize]
public class ApprovalAuthoritiesController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct = default) => mediator.Send(new ListApprovalAuthoritiesQuery(V()), ct);

    [HttpPost]
    public Task<IActionResult> Set([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new SetApprovalAuthorityCommand(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/authorization")]
[Authorize]
public class AuthorizationController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpPost("evaluate")]
    public Task<IActionResult> Evaluate([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new EvaluateAuthorizationCommand(V(), body), ct);

    [HttpGet("effective-permissions")]
    public Task<IActionResult> EffectivePermissions([FromQuery] Guid userId, CancellationToken ct = default)
        => mediator.Send(new GetEffectivePermissionsQuery(V(), userId), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/access-governance")]
[Authorize]
public class AccessGovernanceLookupsController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("lookups/{category}")]
    public Task<IActionResult> Lookups(string category, CancellationToken ct = default)
        => mediator.Send(new GetAccessGovernanceLookupsQuery(V(), category), ct);
}
