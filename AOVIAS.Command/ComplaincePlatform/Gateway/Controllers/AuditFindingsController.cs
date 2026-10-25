using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Gateway controller for compliance journey audit findings (objective element–based).
/// Proxies to ComplianceService via MediatR.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/journeys")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class AuditFindingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuditFindingsController> _logger;

    public AuditFindingsController(
        IMediator mediator,
        ILogger<AuditFindingsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Lists audit findings for a compliance journey.
    /// </summary>
    [HttpGet("{journeyId:guid}/audit-findings")]
    public async Task<IActionResult> GetAuditFindings(
        Guid journeyId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "GET /api/v{Version}/journeys/{JourneyId}/audit-findings — dispatching GetAuditFindingsQuery",
            apiVersion, journeyId);

        return await _mediator.Send(new GetAuditFindingsQuery(journeyId, apiVersion), cancellationToken);
    }

    /// <summary>
    /// Creates an audit finding for a journey (references <see cref="CreateComplianceAuditFindingRequest.ObjectiveElementId"/>).
    /// </summary>
    [HttpPost("{journeyId:guid}/audit-findings")]
    public async Task<IActionResult> CreateAuditFinding(
        Guid journeyId,
        [FromBody] CreateComplianceAuditFindingRequest requestBody,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "POST /api/v{Version}/journeys/{JourneyId}/audit-findings — dispatching CreateComplianceAuditFindingCommand",
            apiVersion, journeyId);

        return await _mediator.Send(
            new CreateComplianceAuditFindingCommand(journeyId, requestBody, apiVersion),
            cancellationToken);
    }
}
