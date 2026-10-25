using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Phase 11 Gateway endpoints for journey obligation state (obligation-aware journey).
/// All endpoints proxy to ComplianceService via IProxyService.
/// Route prefix: api/v{version}/journey
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/journey")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class JourneyObligationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<JourneyObligationsController> _logger;

    public JourneyObligationsController(
        IMediator mediator,
        ILogger<JourneyObligationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Returns obligation overview for a hospital × framework journey.
    /// GET /api/v1/journey/overview?hospitalId=&amp;frameworkId=
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "GET /api/v{Version}/journey/overview - hospitalId={HospitalId}, frameworkId={FrameworkId}",
            apiVersion, hospitalId, frameworkId);

        return await _mediator.Send(
            new GetJourneyObligationOverviewQuery(hospitalId, frameworkId, apiVersion),
            cancellationToken);
    }

    /// <summary>
    /// Returns all obligations for a hospital × framework.
    /// GET /api/v1/journey/obligations?hospitalId=&amp;frameworkId=
    /// </summary>
    [HttpGet("obligations")]
    public async Task<IActionResult> GetObligations(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "GET /api/v{Version}/journey/obligations - hospitalId={HospitalId}, frameworkId={FrameworkId}",
            apiVersion, hospitalId, frameworkId);

        return await _mediator.Send(
            new GetJourneyObligationsQuery(hospitalId, frameworkId, apiVersion),
            cancellationToken);
    }

    /// <summary>
    /// Returns a single obligation by its ID.
    /// GET /api/v1/journey/obligations/{id}
    /// </summary>
    [HttpGet("obligations/{id:guid}")]
    public async Task<IActionResult> GetObligationDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "GET /api/v{Version}/journey/obligations/{Id}",
            apiVersion, id);

        return await _mediator.Send(
            new GetJourneyObligationDetailQuery(id, apiVersion),
            cancellationToken);
    }

    /// <summary>
    /// Returns per-level readiness breakdown for a hospital × framework.
    /// GET /api/v1/journey/readiness?hospitalId=&amp;frameworkId=
    /// </summary>
    [HttpGet("readiness")]
    public async Task<IActionResult> GetReadiness(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "GET /api/v{Version}/journey/readiness - hospitalId={HospitalId}, frameworkId={FrameworkId}",
            apiVersion, hospitalId, frameworkId);

        return await _mediator.Send(
            new GetJourneyReadinessQuery(hospitalId, frameworkId, apiVersion),
            cancellationToken);
    }
}
