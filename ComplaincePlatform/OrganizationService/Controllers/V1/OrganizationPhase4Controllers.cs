using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Application.Commands;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.Queries;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace OrganizationService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/membership")]
[Authorize(Policy = "OrganizationScope")]
public class MembershipController(IMediator mediator) : ControllerBase
{
    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<MembershipDto>>>> List(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? membershipStatus = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListMembershipQuery(organizationId, userId, page, pageSize, search, role, departmentId, membershipStatus, includeInactive), ct), HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MembershipDto>>> Create([FromBody] CreateMembershipRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateMembershipCommand(request, Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MembershipDto>>> Update(Guid id, [FromBody] UpdateMembershipRequest request, CancellationToken ct = default)
    {
        var result = await mediator.Send(new UpdateMembershipCommand(id, request, Actor()), ct);
        if (result == null) return NotFound(ApiResponseFactory.Failure<MembershipDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, HttpContext.TraceIdentifier));
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MembershipHistoryDto>>>> History([FromQuery] Guid membershipId, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetMembershipHistoryQuery(membershipId), ct), HttpContext.TraceIdentifier));

    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<MembershipDto>>> Transfer([FromBody] TransferMembershipRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new TransferMembershipCommand(request, Actor()), ct), HttpContext.TraceIdentifier));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invitations")]
[Authorize(Policy = "OrganizationScope")]
public class InvitationsController(IMediator mediator) : ControllerBase
{
    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<InvitationDto>>>> List(
        [FromQuery] Guid organizationId,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            await mediator.Send(new ListInvitationsQuery(organizationId, status, page, pageSize), ct),
            HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InvitationDto>>> Create([FromBody] CreateInvitationRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateInvitationCommand(request, Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPost("bulk")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InvitationDto>>>> Bulk(
        [FromBody] BulkCreateInvitationsRequest request,
        CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            await mediator.Send(new BulkCreateInvitationsCommand(request, Actor()), ct),
            HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<ApiResponse<MembershipDto>>> Accept(Guid id, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new AcceptInvitationCommand(id, Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new RevokeInvitationCommand(id, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/resend")]
    public async Task<ActionResult<ApiResponse<InvitationDto>>> Resend(Guid id, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            await mediator.Send(new ResendInvitationCommand(id, Actor()), ct),
            HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new CancelInvitationCommand(id, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organizations")]
[Authorize(Policy = "OrganizationScope")]
public class OrganizationTreeController(IMediator mediator) : ControllerBase
{
    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<OrganizationTreeNodeDto>>> Tree([FromQuery] Guid organizationId, CancellationToken ct = default)
    {
        var tree = await mediator.Send(new GetOrganizationTreeQuery(organizationId), ct);
        if (tree == null) return NotFound(ApiResponseFactory.Failure<OrganizationTreeNodeDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, tree, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/departments")]
[Authorize(Policy = "OrganizationScope")]
public class DepartmentTreeController(IMediator mediator) : ControllerBase
{
    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentTreeNodeDto>>>> Tree([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetDepartmentTreeQuery(organizationId, hospitalId), ct), HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DepartmentTreeNodeDto>>> Create([FromBody] CreateOrgDepartmentRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateOrgDepartmentCommand(request, Actor()), ct), HttpContext.TraceIdentifier));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organizational-units")]
[Authorize(Policy = "OrganizationScope")]
public class OrganizationalUnitsController(IMediator mediator) : ControllerBase
{
    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrganizationalUnitTypeDto>>>> Types(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetOrganizationalUnitTypesQuery(), ct), HttpContext.TraceIdentifier));

    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrganizationalUnitDto>>>> Tree(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? hospitalId,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetOrganizationalUnitTreeQuery(organizationId, hospitalId, includeInactive), ct), HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrganizationalUnitDto>>> Create([FromBody] CreateOrganizationalUnitRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateOrganizationalUnitCommand(request, Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrganizationalUnitDto>>> Update(Guid id, [FromBody] UpdateOrganizationalUnitRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new UpdateOrganizationalUnitCommand(id, request, Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/move")]
    public async Task<ActionResult<ApiResponse<OrganizationalUnitDto>>> Move(Guid id, [FromBody] MoveOrganizationalUnitRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new MoveOrganizationalUnitCommand(id, request, Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<OrganizationalUnitDto>>> Activate(Guid id, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new SetOrganizationalUnitStatusCommand(id, "Active", Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<OrganizationalUnitDto>>> Deactivate(Guid id, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new SetOrganizationalUnitStatusCommand(id, "Inactive", Actor()), ct), HttpContext.TraceIdentifier));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new SoftDeleteOrganizationalUnitCommand(id, Actor()), ct);
        return Ok(ApiResponseFactory.Success<object>(ApiResponseCode.SUCCESS, new { id }, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organization")]
[Authorize(Policy = "OrganizationScope")]
public class OrganizationGraphController(IMediator mediator) : ControllerBase
{
    [HttpGet("graph")]
    public async Task<ActionResult<ApiResponse<OrganizationGraphDto>>> Graph([FromQuery] Guid organizationId, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetOrganizationGraphQuery(organizationId), ct), HttpContext.TraceIdentifier));
}
