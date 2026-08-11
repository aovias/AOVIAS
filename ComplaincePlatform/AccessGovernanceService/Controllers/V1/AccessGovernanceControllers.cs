using AccessGovernanceService.Application.Commands;
using AccessGovernanceService.Application.Contracts;
using AccessGovernanceService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace AccessGovernanceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize]
public class RolesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<RoleListItemDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var data = await mediator.Send(new ListRolesQuery(page, pageSize), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Get(Guid id, CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetRoleByIdQuery(id), ct);
        if (data == null) return NotFound(ApiResponseFactory.Failure<RoleDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create([FromBody] CreateRoleRequest request, CancellationToken ct = default)
    {
        var data = await mediator.Send(new CreateRoleCommand(request, GetUserId()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct = default)
    {
        await mediator.Send(new UpdateRoleCommand(id, request, GetUserId()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> GrantPermission(Guid id, Guid permissionId, CancellationToken ct = default)
    {
        await mediator.Send(new GrantPermissionToRoleCommand(id, permissionId, GetUserId()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{userId:guid}/assign/{roleId:guid}")]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId, [FromBody] AssignRoleRequest body, CancellationToken ct = default)
    {
        var request = new AssignRoleRequest
        {
            UserId = userId,
            OrganizationId = body.OrganizationId,
            HospitalId = body.HospitalId,
            DepartmentId = body.DepartmentId,
            EmploymentId = body.EmploymentId,
            EffectiveFrom = body.EffectiveFrom,
            EffectiveTo = body.EffectiveTo
        };
        await mediator.Send(new AssignRoleCommand(roleId, request, GetUserId()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? Guid.Empty.ToString());
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/permissions")]
[Authorize]
public class PermissionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> List([FromQuery] Guid? permissionGroupValueId = null, CancellationToken ct = default)
    {
        var data = await mediator.Send(new ListPermissionsQuery(permissionGroupValueId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> Create([FromBody] CreatePermissionRequest request, CancellationToken ct = default)
    {
        var data = await mediator.Send(new CreatePermissionCommand(request), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/policies")]
[Authorize]
public class PoliciesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PolicyDto>>>> List(CancellationToken ct = default)
    {
        var data = await mediator.Send(new ListPoliciesQuery(), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PolicyDetailDto>>> Get(Guid id, CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetPolicyByIdQuery(id), ct);
        if (data == null) return NotFound(ApiResponseFactory.Failure<PolicyDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PolicyDto>>> Create([FromBody] CreatePolicyRequest request, CancellationToken ct = default)
    {
        var data = await mediator.Send(new CreatePolicyCommand(request), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePolicyRequest request, CancellationToken ct = default)
    {
        await mediator.Send(new UpdatePolicyCommand(id, request), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new PublishPolicyCommand(id), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new ArchivePolicyCommand(id), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/delegations")]
[Authorize]
public class DelegationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DelegationDto>>>> List(CancellationToken ct = default)
    {
        var data = await mediator.Send(new ListDelegationsQuery(), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DelegationDto>>> Create([FromBody] CreateDelegationRequest request, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var data = await mediator.Send(new CreateDelegationCommand(request, userId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        await mediator.Send(new RevokeDelegationCommand(id, userId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/responsibilities")]
[Authorize]
public class ResponsibilitiesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ResponsibilityDto>>>> List(CancellationToken ct = default)
    {
        var data = await mediator.Send(new ListResponsibilitiesQuery(), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ResponsibilityDto>>> Assign([FromBody] AssignResponsibilityRequest request, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var data = await mediator.Send(new AssignResponsibilityCommand(request, userId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/approval-authorities")]
[Authorize]
public class ApprovalAuthoritiesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApprovalAuthorityDto>>>> List(CancellationToken ct = default)
    {
        var data = await mediator.Send(new ListApprovalAuthoritiesQuery(), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ApprovalAuthorityDto>>> Set([FromBody] SetApprovalAuthorityRequest request, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var data = await mediator.Send(new SetApprovalAuthorityCommand(request, userId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/authorization")]
[Authorize]
public class AuthorizationController(IMediator mediator) : ControllerBase
{
    [HttpPost("evaluate")]
    public async Task<ActionResult<ApiResponse<AuthorizationDecisionDto>>> Evaluate([FromBody] EvaluateAuthorizationRequest request, CancellationToken ct = default)
    {
        var data = await mediator.Send(new EvaluateAuthorizationCommand(request), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpGet("effective-permissions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EffectivePermissionDto>>>> EffectivePermissions([FromQuery] Guid userId, CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetEffectivePermissionsQuery(userId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/role-assignments")]
[Authorize]
public class RoleAssignmentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleAssignmentDto>>>> List([FromQuery] Guid? userId, CancellationToken ct = default)
    {
        var data = await mediator.Send(new ListRoleAssignmentsQuery(userId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignRoleAssignmentRequest request, CancellationToken ct = default)
    {
        var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? Guid.Empty.ToString());
        var assignRequest = new AssignRoleRequest
        {
            UserId = request.UserId,
            OrganizationId = request.OrganizationId,
            HospitalId = request.HospitalId,
            DepartmentId = request.DepartmentId,
            EmploymentId = request.EmploymentId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo
        };
        await mediator.Send(new AssignRoleCommand(request.RoleId, assignRequest, actorId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct = default)
    {
        var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? Guid.Empty.ToString());
        await mediator.Send(new RevokeRoleAssignmentCommand(id, actorId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}

public sealed class AssignRoleAssignmentRequest
{
    public Guid RoleId { get; init; }
    public Guid UserId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? EmploymentId { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/access-governance")]
[Authorize]
public class AccessGovernanceLookupsController(IMediator mediator) : ControllerBase
{
    [HttpGet("lookups/{category}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupValueDto>>>> Lookups(string category, CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetLookupsQuery(category), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }
}
