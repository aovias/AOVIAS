using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/assignment")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class AssignmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssignmentController(IMediator mediator) => _mediator = mediator;

    [HttpGet("deficiencies/{id:guid}/suggestion")]
    public Task<IActionResult> GetDeficiencySuggestion(Guid id, CancellationToken ct) =>
        _mediator.Send(new GetDeficiencyAssignmentSuggestionQuery(id, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpPost("deficiencies/{id:guid}/auto-assign")]
    public Task<IActionResult> AutoAssignDeficiency(Guid id, [FromQuery] bool force = false, CancellationToken ct = default) =>
        _mediator.Send(new AutoAssignDeficiencyCommand(id, force, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpGet("findings/{id:guid}/suggestion")]
    public Task<IActionResult> GetFindingSuggestion(
        Guid id,
        [FromQuery] Guid organizationId,
        [FromQuery] Guid hospitalId,
        CancellationToken ct) =>
        _mediator.Send(new GetFindingAssignmentSuggestionQuery(id, organizationId, hospitalId, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpGet("metrics")]
    public Task<IActionResult> GetMetrics([FromQuery] Guid organizationId, CancellationToken ct) =>
        _mediator.Send(new GetAssignmentRoutingMetricsQuery(organizationId, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpGet("department-label-mappings")]
    public Task<IActionResult> GetLabelMappings(CancellationToken ct) =>
        _mediator.Send(new GetDepartmentLabelMappingsQuery(HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpGet("department-ownership")]
    public Task<IActionResult> ListDepartmentOwnership(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? hospitalId,
        CancellationToken ct) =>
        _mediator.Send(new ListDepartmentOwnershipQuery(organizationId, hospitalId, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpPut("department-ownership")]
    public Task<IActionResult> UpsertDepartmentOwnership([FromBody] object body, CancellationToken ct) =>
        _mediator.Send(new UpsertDepartmentOwnershipCommand(body, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);
}
