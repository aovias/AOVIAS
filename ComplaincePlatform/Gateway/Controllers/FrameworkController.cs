using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Controller for Framework Management endpoints.
/// Proxies requests to ComplianceService via MediatR.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/frameworks")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class FrameworkController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FrameworkController> _logger;

    public FrameworkController(
        IMediator mediator,
        ILogger<FrameworkController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available compliance frameworks.
    /// </summary>
    [HttpGet("all-frameworks")]
    public Task<IActionResult> GetAllFrameworks(CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation("GET /api/v{Version}/frameworks/all-frameworks", apiVersion);
        return _mediator.Send(new GetAllFrameworksQuery(apiVersion), cancellationToken);
    }

    /// <summary>
    /// Framework catalog with lifecycle status (Phase 1).
    /// </summary>
    [HttpGet("catalog")]
    public Task<IActionResult> GetCatalog(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "GET /api/v{Version}/frameworks/catalog includeInactive={IncludeInactive}",
            apiVersion, includeInactive);
        return _mediator.Send(new GetComplianceFrameworkCatalogQuery(includeInactive, apiVersion), cancellationToken);
    }

    [HttpPost("{id:guid}/publish")]
    public Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new PublishFrameworkCommand(id, apiVersion), cancellationToken);
    }

    [HttpPost("{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new ActivateFrameworkCommand(id, apiVersion), cancellationToken);
    }

    [HttpPost("{id:guid}/retire")]
    public Task<IActionResult> Retire(Guid id, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new RetireFrameworkCommand(id, apiVersion), cancellationToken);
    }

    [HttpPost("{id:guid}/archive")]
    public Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new ArchiveFrameworkCommand(id, apiVersion), cancellationToken);
    }

    /// <summary>
    /// Gets a dynamically generated assessment questionnaire for a compliance framework.
    /// </summary>
    [HttpGet("{frameworkId:guid}/questionnaire")]
    public Task<IActionResult> GetFrameworkQuestionnaire(Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "GET /api/v{Version}/frameworks/{FrameworkId}/questionnaire",
            apiVersion,
            frameworkId);
        return _mediator.Send(new GetFrameworkQuestionnaireQuery(frameworkId, apiVersion), cancellationToken);
    }

    /// <summary>
    /// Gets frameworks where a hospital is actively enrolled.
    /// </summary>
    [HttpGet("/api/v{version:apiVersion}/hospitals/{hospitalId}/frameworks")]
    public Task<IActionResult> GetHospitalFrameworks(
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation("GET /api/v{Version}/hospitals/{HospitalId}/frameworks", apiVersion, hospitalId);
        return _mediator.Send(new GetHospitalFrameworksQuery(hospitalId, apiVersion), cancellationToken);
    }

    /// <summary>
    /// Enrolls a hospital in a compliance framework.
    /// </summary>
    [HttpPost("/api/v{version:apiVersion}/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll")]
    public Task<IActionResult> EnrollHospitalInFramework(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "POST /api/v{Version}/hospitals/{HospitalId}/frameworks/{FrameworkId}/enroll",
            apiVersion, hospitalId, frameworkId);
        return _mediator.Send(new EnrollHospitalInFrameworkCommand(hospitalId, frameworkId, apiVersion), cancellationToken);
    }
}
