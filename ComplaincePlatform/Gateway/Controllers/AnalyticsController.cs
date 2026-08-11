using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Gateway entry point for the Analytics Platform (Sprint 12B-1).
/// Proxies read-only analytics queries to ComplianceService.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/analytics")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator) => _mediator = mediator;

    private string ApiVersion => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";

    [HttpGet("overview")]
    public Task<IActionResult> GetOverview(CancellationToken cancellationToken) =>
        _mediator.Send(new GetAnalyticsOverviewQuery(ApiVersion), cancellationToken);

    [HttpGet("executive")]
    public Task<IActionResult> GetExecutive(CancellationToken cancellationToken) =>
        _mediator.Send(new GetAnalyticsExecutiveQuery(ApiVersion), cancellationToken);

    [HttpGet("operational")]
    public Task<IActionResult> GetOperational(CancellationToken cancellationToken) =>
        _mediator.Send(new GetAnalyticsOperationalQuery(ApiVersion), cancellationToken);

    [HttpGet("rca")]
    public Task<IActionResult> GetRca(CancellationToken cancellationToken) =>
        _mediator.Send(new GetAnalyticsRcaQuery(ApiVersion), cancellationToken);

    [HttpGet("capa")]
    public Task<IActionResult> GetCapa(CancellationToken cancellationToken) =>
        _mediator.Send(new GetAnalyticsCapaQuery(ApiVersion), cancellationToken);

    [HttpGet("sla")]
    public Task<IActionResult> GetSla(CancellationToken cancellationToken) =>
        _mediator.Send(new GetAnalyticsSlaQuery(ApiVersion), cancellationToken);

    [HttpGet("departments")]
    public Task<IActionResult> GetDepartments(CancellationToken cancellationToken) =>
        _mediator.Send(new GetAnalyticsDepartmentsQuery(ApiVersion), cancellationToken);

    [HttpGet("trends")]
    public Task<IActionResult> GetTrends(CancellationToken cancellationToken)
    {
        var queryString = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value
            : null;
        return _mediator.Send(new GetAnalyticsTrendsQuery(ApiVersion, queryString), cancellationToken);
    }

    /// <summary>Phase 3 Sprint 6 — role pack engagement counters (proxied).</summary>
    [HttpGet("role-engagement")]
    public Task<IActionResult> GetRoleEngagement(CancellationToken cancellationToken)
    {
        var queryString = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value
            : null;
        return _mediator.Send(new GetAnalyticsRoleEngagementQuery(ApiVersion, queryString), cancellationToken);
    }

    /// <summary>Phase 4 Sprint 6 — governance KPIs (Governance + Compliance enrichment).</summary>
    [HttpGet("governance")]
    public Task<IActionResult> GetGovernance(CancellationToken cancellationToken) =>
        _mediator.Send(new GetAnalyticsGovernanceQuery(ApiVersion), cancellationToken);
}
