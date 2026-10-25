using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;
using Shared.Infrastructure.Execution;

namespace Gateway.Controllers;

/// <summary>
/// Fifteen endpoints here accept <c>organizationId</c> from the query string, previously gated only
/// by a permission check — so a caller holding the permission in one tenant could read or refresh
/// another's dashboards. <see cref="ValidateTenantScopeAttribute"/> pins that argument to the
/// authenticated organization (Wave 2.2A risk R-02).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/intelligence")]
[Authorize]
[ValidateTenantScope]
public class WorkforceIntelligenceController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("dashboard")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Dashboard([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
        => mediator.Send(new GetIntelligenceDashboardQuery(V(), organizationId, hospitalId, departmentId, asOf), ct);

    [HttpGet("headcount")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Headcount([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
        => mediator.Send(new GetIntelligenceHeadcountQuery(V(), organizationId, hospitalId, departmentId, asOf), ct);

    [HttpGet("skills")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Skills([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
        => mediator.Send(new GetIntelligenceSkillsQuery(V(), organizationId, hospitalId, departmentId, asOf), ct);

    [HttpGet("competencies")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Competencies([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
        => mediator.Send(new GetIntelligenceCompetenciesQuery(V(), organizationId, hospitalId, departmentId, asOf), ct);

    [HttpGet("training")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Training([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
        => mediator.Send(new GetIntelligenceTrainingQuery(V(), organizationId, hospitalId, departmentId, asOf), ct);

    [HttpGet("licenses")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Licenses([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
        => mediator.Send(new GetIntelligenceLicensesQuery(V(), organizationId, hospitalId, departmentId, asOf), ct);

    [HttpGet("certifications")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Certifications([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
        => mediator.Send(new GetIntelligenceCertificationsQuery(V(), organizationId, hospitalId, departmentId, asOf), ct);

    [HttpGet("forecast")]
    [RequirePermission("Intelligence.Forecast.Read")]
    public Task<IActionResult> Forecast([FromQuery] Guid organizationId, [FromQuery] string forecastType = "CertificationExpiry", CancellationToken ct = default)
        => mediator.Send(new GetIntelligenceForecastQuery(V(), organizationId, forecastType), ct);

    [HttpGet("recommendations")]
    [RequirePermission("Intelligence.Recommendations.Read")]
    public Task<IActionResult> Recommendations([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new GetIntelligenceRecommendationsQuery(V(), organizationId), ct);

    [HttpGet("search")]
    [RequirePermission("Intelligence.Search.Read")]
    public Task<IActionResult> Search([FromQuery] Guid organizationId, [FromQuery] string query = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => mediator.Send(new SearchIntelligenceWorkforceQuery(V(), organizationId, query, page, pageSize), ct);

    [HttpGet("organization")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Organization([FromQuery] Guid organizationId, [FromQuery] DateTime? asOf, CancellationToken ct)
        => mediator.Send(new GetIntelligenceOrganizationQuery(V(), organizationId, asOf), ct);

    [HttpGet("responsibilities")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Responsibilities([FromQuery] Guid organizationId, [FromQuery] DateTime? asOf, CancellationToken ct)
        => mediator.Send(new GetIntelligenceResponsibilitiesQuery(V(), organizationId, asOf), ct);

    [HttpGet("committees")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Committees([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new GetIntelligenceCommitteesQuery(V(), organizationId), ct);

    [HttpGet("availability")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Availability([FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, CancellationToken ct)
        => mediator.Send(new GetIntelligenceAvailabilityQuery(V(), organizationId, hospitalId), ct);

    [HttpPost("refresh")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public Task<IActionResult> Refresh([FromQuery] Guid organizationId, CancellationToken ct)
        => mediator.Send(new RefreshIntelligenceDashboardQuery(V(), organizationId), ct);
}
