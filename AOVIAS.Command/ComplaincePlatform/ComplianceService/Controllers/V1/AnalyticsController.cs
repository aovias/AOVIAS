using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.RoleExperience;
using ComplianceService.Application.Services;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Read-only analytics platform APIs (Sprint 12B-1).
/// Consumed exclusively via the Gateway — portal never calls this directly.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/analytics")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsQueryService _analyticsQuery;
    private readonly IRoleExperienceService _roleExperience;
    private readonly IGovernanceDomainKpiService _governanceKpis;
    private readonly IUserContextService _userContext;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsQueryService analyticsQuery,
        IRoleExperienceService roleExperience,
        IGovernanceDomainKpiService governanceKpis,
        IUserContextService userContext,
        ILogger<AnalyticsController> logger)
    {
        _analyticsQuery = analyticsQuery;
        _roleExperience = roleExperience;
        _governanceKpis = governanceKpis;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("overview")]
    public Task<ActionResult<ApiResponse<AnalyticsOverviewDto>>> GetOverview(
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(scope => _analyticsQuery.GetOverviewAsync(scope, cancellationToken), frameworkId, cancellationToken);

    [HttpGet("executive")]
    public Task<ActionResult<ApiResponse<ExecutiveAnalyticsDto>>> GetExecutive(
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(scope => _analyticsQuery.GetExecutiveAsync(scope, cancellationToken), frameworkId, cancellationToken);

    [HttpGet("operational")]
    public Task<ActionResult<ApiResponse<OperationalAnalyticsDto>>> GetOperational(
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(scope => _analyticsQuery.GetOperationalAsync(scope, cancellationToken), frameworkId, cancellationToken);

    [HttpGet("rca")]
    public Task<ActionResult<ApiResponse<RcaAnalyticsDto>>> GetRca(
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(scope => _analyticsQuery.GetRcaAsync(scope, cancellationToken), frameworkId, cancellationToken);

    [HttpGet("capa")]
    public Task<ActionResult<ApiResponse<CapaAnalyticsDto>>> GetCapa(
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(scope => _analyticsQuery.GetCapaAsync(scope, cancellationToken), frameworkId, cancellationToken);

    [HttpGet("sla")]
    public Task<ActionResult<ApiResponse<SlaAnalyticsDto>>> GetSla(
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(scope => _analyticsQuery.GetSlaAsync(scope, cancellationToken), frameworkId, cancellationToken);

    [HttpGet("departments")]
    public Task<ActionResult<ApiResponse<DepartmentAnalyticsDto>>> GetDepartments(
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(scope => _analyticsQuery.GetDepartmentsAsync(scope, cancellationToken), frameworkId, cancellationToken);

    [HttpGet("trends")]
    public Task<ActionResult<ApiResponse<AnalyticsTrendsDto>>> GetTrends(
        [FromQuery] Guid? frameworkId,
        [FromQuery] string? metric,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] AnalyticsTrendGranularity granularity = AnalyticsTrendGranularity.Daily,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            scope => _analyticsQuery.GetTrendsAsync(
                scope,
                new AnalyticsTrendQueryDto
                {
                    Metric = metric,
                    From = from,
                    To = to,
                    Granularity = granularity,
                },
                cancellationToken),
            frameworkId,
            cancellationToken);

    /// <summary>
    /// GET /api/v1/analytics/governance — domain enrichment for governance KPIs (Phase 4 Sprint 6).
    /// Committee/meeting counts come from GovernanceService; this supplies docs/incidents/risks.
    /// </summary>
    [HttpGet("governance")]
    public async Task<ActionResult<ApiResponse<GovernanceDomainKpisDto>>> GetGovernance(
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;
        if (organizationId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(
                meta.HttpStatus,
                ApiResponseFactory.Failure<GovernanceDomainKpisDto>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        var data = await _governanceKpis.GetAsync(organizationId, _userContext.HospitalId, cancellationToken);
        var success = ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, data, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(successMeta.HttpStatus, success);
    }

    /// <summary>
    /// GET /api/v1/analytics/role-engagement — pack engagement counters (Phase 3 Sprint 6).
    /// Best-effort from existing operational / CAPA analytics.
    /// </summary>
    [HttpGet("role-engagement")]
    public async Task<ActionResult<ApiResponse<RoleEngagementDto>>> GetRoleEngagement(
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;
        if (organizationId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(
                meta.HttpStatus,
                ApiResponseFactory.Failure<RoleEngagementDto>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        var hospitalId = _userContext.HospitalId;
        if (hospitalId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
            return StatusCode(
                meta.HttpStatus,
                ApiResponseFactory.Failure<RoleEngagementDto>(
                    ApiResponseCode.HOSPITAL_ID_REQUIRED, requestId));
        }

        var data = await _roleExperience.GetEngagementAsync(
            _userContext.Roles,
            organizationId,
            hospitalId,
            frameworkId,
            cancellationToken);

        var success = ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, data, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(successMeta.HttpStatus, success);
    }

    private async Task<ActionResult<ApiResponse<T>>> ExecuteAsync<T>(
        Func<AnalyticsScope, Task<T>> action,
        Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        var organizationId = _userContext.OrganizationId;
        if (organizationId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            var response = ApiResponseFactory.Failure<T>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId);
            return StatusCode(meta.HttpStatus, response);
        }

        var hospitalId = _userContext.HospitalId;
        if (hospitalId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
            var response = ApiResponseFactory.Failure<T>(ApiResponseCode.HOSPITAL_ID_REQUIRED, requestId);
            return StatusCode(meta.HttpStatus, response);
        }

        var scope = await _analyticsQuery.ResolveScopeAsync(organizationId, hospitalId, frameworkId, cancellationToken);

        _logger.LogInformation(
            "Analytics query {Action} for OrgId={OrgId} HospitalId={HospitalId} FrameworkId={FrameworkId}",
            typeof(T).Name,
            organizationId,
            hospitalId,
            scope.FrameworkId);

        var data = await action(scope);
        var success = ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, data, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(successMeta.HttpStatus, success);
    }
}
