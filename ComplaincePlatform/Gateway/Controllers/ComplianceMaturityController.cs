using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Gateway entry point for the Compliance Maturity Dashboard.
/// Proxies to ComplianceService; the Portal MUST route through this controller only.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compliance-maturity")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class ComplianceMaturityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ComplianceMaturityController> _logger;

    public ComplianceMaturityController(
        IMediator mediator,
        ILogger<ComplianceMaturityController> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>
    /// Returns the full Compliance Maturity Dashboard for the authenticated hospital.
    /// Includes CMI score, maturity stage breakdown, actual Core gaps, predicted upper-stage gaps,
    /// and rules-based recommended next actions.
    /// </summary>
    [HttpGet("dashboard")]
    public Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Compliance maturity dashboard requested for user {User}", User.Identity?.Name);

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
        return _mediator.Send(new GetComplianceMaturityDashboardQuery(apiVersion), cancellationToken);
    }
}
