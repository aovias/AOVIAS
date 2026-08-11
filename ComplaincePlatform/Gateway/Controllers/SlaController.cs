using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sla")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class SlaController : ControllerBase
{
    private readonly IMediator _mediator;

    public SlaController(IMediator mediator) => _mediator = mediator;

    [HttpGet("policies")]
    public Task<IActionResult> ListPolicies(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? hospitalId,
        CancellationToken ct) =>
        _mediator.Send(new ListSlaPoliciesQuery(organizationId, hospitalId, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpPost("policies")]
    public Task<IActionResult> CreatePolicy([FromBody] object body, CancellationToken ct) =>
        _mediator.Send(new CreateSlaPolicyCommand(body, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpGet("deficiencies/{id:guid}")]
    public Task<IActionResult> GetDeficiencySla(Guid id, CancellationToken ct) =>
        _mediator.Send(new GetDeficiencySlaQuery(id, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpGet("deficiencies/{id:guid}/escalations")]
    public Task<IActionResult> GetDeficiencyEscalations(Guid id, CancellationToken ct) =>
        _mediator.Send(new GetDeficiencyEscalationsQuery(id, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpPost("deficiencies/{id:guid}/acknowledge-escalation")]
    public Task<IActionResult> AcknowledgeEscalation(Guid id, [FromBody] object body, CancellationToken ct) =>
        _mediator.Send(new AcknowledgeEscalationCommand(id, body, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);

    [HttpGet("metrics")]
    public Task<IActionResult> GetMetrics([FromQuery] Guid organizationId, CancellationToken ct) =>
        _mediator.Send(new GetSlaMetricsQuery(organizationId, HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"), ct);
}
