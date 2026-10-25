using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Thin proxy for hospital OE ownership matrix.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hospitals/{hospitalId:guid}/oe-ownership")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class OeOwnershipController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OeOwnershipController> _logger;

    public OeOwnershipController(IMediator mediator, ILogger<OeOwnershipController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> ListMatrix(
        Guid hospitalId,
        [FromQuery] Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "GET /api/v{Version}/hospitals/{HospitalId}/oe-ownership",
            apiVersion, hospitalId);
        return _mediator.Send(new ListOeOwnershipQuery(hospitalId, frameworkId, apiVersion), cancellationToken);
    }

    [HttpPut]
    public Task<IActionResult> Upsert(
        Guid hospitalId,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "PUT /api/v{Version}/hospitals/{HospitalId}/oe-ownership",
            apiVersion, hospitalId);
        return _mediator.Send(new UpsertOeOwnershipCommand(hospitalId, body, apiVersion), cancellationToken);
    }

    [HttpDelete("{objectiveElementId:guid}")]
    public Task<IActionResult> Clear(
        Guid hospitalId,
        Guid objectiveElementId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "DELETE /api/v{Version}/hospitals/{HospitalId}/oe-ownership/{ObjectiveElementId}",
            apiVersion, hospitalId, objectiveElementId);
        return _mediator.Send(
            new ClearOeOwnershipCommand(hospitalId, objectiveElementId, apiVersion),
            cancellationToken);
    }
}
