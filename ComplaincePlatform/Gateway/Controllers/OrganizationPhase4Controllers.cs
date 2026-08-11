using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/membership")]
[Authorize]
public class MembershipController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(
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
        => mediator.Send(
            new ListMembershipProxyQuery(
                V(),
                organizationId,
                userId,
                page,
                pageSize,
                search,
                role,
                departmentId,
                membershipStatus,
                includeInactive),
            ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateMembershipProxyCommand(V(), body), ct);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new UpdateMembershipProxyCommand(V(), id, body), ct);

    [HttpGet("history")]
    public Task<IActionResult> History([FromQuery] Guid membershipId, CancellationToken ct = default)
        => mediator.Send(new GetMembershipHistoryProxyQuery(V(), membershipId), ct);

    [HttpPost("transfer")]
    public Task<IActionResult> Transfer([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new TransferMembershipProxyCommand(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organizations")]
[Authorize]
public class OrganizationStructureController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("tree")]
    public Task<IActionResult> Tree([FromQuery] Guid organizationId, CancellationToken ct = default)
        => mediator.Send(new GetOrganizationTreeProxyQuery(V(), organizationId), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/departments")]
[Authorize]
public class OrganizationDepartmentsController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("tree")]
    public Task<IActionResult> Tree([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, CancellationToken ct = default)
        => mediator.Send(new GetDepartmentTreeProxyQuery(V(), organizationId, hospitalId), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateOrgDepartmentProxyCommand(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organizational-units")]
[Authorize]
public class OrganizationalUnitsGatewayController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("types")]
    public Task<IActionResult> Types(CancellationToken ct = default)
        => mediator.Send(new GetOrganizationalUnitTypesProxyQuery(V()), ct);

    [HttpGet("tree")]
    public Task<IActionResult> Tree(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? hospitalId,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
        => mediator.Send(new GetOrganizationalUnitTreeProxyQuery(V(), organizationId, hospitalId, includeInactive), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateOrganizationalUnitProxyCommand(V(), body), ct);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new UpdateOrganizationalUnitProxyCommand(V(), id, body), ct);

    [HttpPost("{id:guid}/move")]
    public Task<IActionResult> Move(Guid id, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new MoveOrganizationalUnitProxyCommand(V(), id, body), ct);

    [HttpPost("{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct = default)
        => mediator.Send(new SetOrganizationalUnitStatusProxyCommand(V(), id, "activate"), ct);

    [HttpPost("{id:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
        => mediator.Send(new SetOrganizationalUnitStatusProxyCommand(V(), id, "deactivate"), ct);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        => mediator.Send(new DeleteOrganizationalUnitProxyCommand(V(), id), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organization")]
[Authorize]
public class OrganizationGraphController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("graph")]
    public Task<IActionResult> Graph([FromQuery] Guid organizationId, CancellationToken ct = default)
        => mediator.Send(new GetOrganizationGraphProxyQuery(V(), organizationId), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invitations")]
public class OrganizationInvitationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly Application.Services.InvitationRedeemOrchestrator _orchestrator;
    private readonly Services.IProxyService _proxy;
    private readonly ILogger<OrganizationInvitationsController> _logger;

    public OrganizationInvitationsController(
        IMediator mediator,
        Application.Services.InvitationRedeemOrchestrator orchestrator,
        Services.IProxyService proxy,
        ILogger<OrganizationInvitationsController> logger)
    {
        _mediator = mediator;
        _orchestrator = orchestrator;
        _proxy = proxy;
        _logger = logger;
    }

    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpPost]
    [Authorize]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => _mediator.Send(new CreateInvitationProxyCommand(V(), body), ct);

    [HttpPost("{id:guid}/accept")]
    [Authorize]
    public Task<IActionResult> Accept(Guid id, CancellationToken ct = default)
        => _mediator.Send(new AcceptInvitationProxyCommand(V(), id), ct);

    [HttpGet("redeem/preview")]
    [AllowAnonymous]
    public Task<IActionResult> Preview([FromQuery] string token, CancellationToken ct)
        => _orchestrator.PreviewAsync(token ?? string.Empty, ct);

    public record RedeemRequest(string Token, string? Password, string? FirstName, string? LastName);

    [HttpPost("redeem")]
    [AllowAnonymous]
    public async Task<IActionResult> Redeem([FromBody] RedeemRequest body, CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;
        try
        {
            if (string.IsNullOrWhiteSpace(body.Token))
            {
                return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, requestId);
            }

            var result = await _orchestrator.RedeemAsync(
                body.Token,
                body.Password,
                body.FirstName,
                body.LastName,
                User,
                ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, requestId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invitation redeem rejected");
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, requestId);
            if (failure.Error != null)
            {
                failure.Error.Message = ex.Message;
            }

            return BadRequest(failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invitation redeem failed");
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.INTERNAL_ERROR, requestId);
        }
    }

    [HttpGet]
    [Authorize]
    public Task<IActionResult> List(
        [FromQuery] Guid organizationId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var q = $"/api/v{V()}/invitations?organizationId={organizationId}&page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status))
        {
            q += $"&status={Uri.EscapeDataString(status)}";
        }

        return _proxy.ProxyToOrganizationServiceAsync(q, HttpMethod.Get, cancellationToken: ct);
    }

    [HttpPost("bulk")]
    [Authorize]
    public Task<IActionResult> Bulk([FromBody] object body, CancellationToken ct)
        => _proxy.ProxyToOrganizationServiceAsync($"/api/v{V()}/invitations/bulk", HttpMethod.Post, body, cancellationToken: ct);

    [HttpPost("{id:guid}/resend")]
    [Authorize]
    public Task<IActionResult> Resend(Guid id, CancellationToken ct)
        => _proxy.ProxyToOrganizationServiceAsync($"/api/v{V()}/invitations/{id}/resend", HttpMethod.Post, cancellationToken: ct);

    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    public Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => _proxy.ProxyToOrganizationServiceAsync($"/api/v{V()}/invitations/{id}/cancel", HttpMethod.Post, cancellationToken: ct);

    [HttpPost("{id:guid}/revoke")]
    [Authorize]
    public Task<IActionResult> Revoke(Guid id, CancellationToken ct)
        => _proxy.ProxyToOrganizationServiceAsync($"/api/v{V()}/invitations/{id}/revoke", HttpMethod.Post, cancellationToken: ct);
}
