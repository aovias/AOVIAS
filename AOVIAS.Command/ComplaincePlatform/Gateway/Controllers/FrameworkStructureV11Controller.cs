using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain.Enums;

namespace Gateway.Controllers;

/// <summary>
/// Proxies NABH framework structure reads (Framework → Chapter → Standard → Objective Element) to ComplianceService v1.1.
/// </summary>
[ApiController]
[ApiVersion("1.1")]
[Route("api/v{version:apiVersion}/frameworks")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class FrameworkStructureV11Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FrameworkStructureV11Controller> _logger;

    public FrameworkStructureV11Controller(
        IMediator mediator,
        ILogger<FrameworkStructureV11Controller> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("{frameworkId:guid}/structure")]
    public Task<IActionResult> GetFrameworkStructure(
        Guid frameworkId,
        [FromQuery] OperationalMode? operationalMode = null,
        [FromQuery] string? classifications = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.1";

        _logger.LogInformation(
            "GET framework structure — dispatching GetFrameworkStructureV11Query (v{Version})",
            apiVersion);

        return _mediator.Send(
            new GetFrameworkStructureV11Query(frameworkId, apiVersion, operationalMode, classifications, page, pageSize),
            cancellationToken);
    }
}
