using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain.Enums;

namespace Gateway.Controllers;

/// <summary>
/// Proxies OE-based scoring reads (aggregation + enriched scores) to ComplianceService v1.1.
/// </summary>
[ApiController]
[ApiVersion("1.1")]
[Route("api/v{version:apiVersion}/scoring")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class ComplianceScoringV11Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ComplianceScoringV11Controller> _logger;

    public ComplianceScoringV11Controller(
        IMediator mediator,
        ILogger<ComplianceScoringV11Controller> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("hospitals/{hospitalId:guid}/frameworks/{frameworkId:guid}/journeys/{journeyId:guid}/aggregate")]
    public Task<IActionResult> GetAggregate(
        Guid hospitalId,
        Guid frameworkId,
        Guid journeyId,
        [FromQuery] OperationalMode operationalMode = OperationalMode.FinalAudit,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.1";
        _logger.LogInformation("GET aggregate — dispatching GetScoringAggregateV11Query");
        return _mediator.Send(
            new GetScoringAggregateV11Query(hospitalId, frameworkId, journeyId, apiVersion, operationalMode),
            cancellationToken);
    }

    [HttpGet("hospitals/{hospitalId:guid}/frameworks/{frameworkId:guid}/score-enriched")]
    public Task<IActionResult> GetEnrichedScore(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.1";
        _logger.LogInformation("GET score-enriched — dispatching GetScoringEnrichedV11Query");
        return _mediator.Send(
            new GetScoringEnrichedV11Query(hospitalId, frameworkId, apiVersion),
            cancellationToken);
    }
}
