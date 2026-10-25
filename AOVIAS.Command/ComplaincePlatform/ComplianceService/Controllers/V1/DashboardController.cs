using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Services;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardAppService _dashboardAppService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardAppService dashboardAppService,
        IUserContextService userContext,
        ILogger<DashboardController> logger)
    {
        _dashboardAppService = dashboardAppService;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(
        [FromQuery] Guid hospitalId,
        CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;

        if (hospitalId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
            var response = ApiResponseFactory.Failure<DashboardSummaryDto>(ApiResponseCode.HOSPITAL_ID_REQUIRED, requestId);

            return StatusCode(meta.HttpStatus, response);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            var response = ApiResponseFactory.Failure<DashboardSummaryDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId);

            return StatusCode(meta.HttpStatus, response);
        }

        var summary = await _dashboardAppService.GetDashboardSummaryAsync(hospitalId, tenantId, ct);

        var success = ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, summary, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];

        return StatusCode(successMeta.HttpStatus, success);
    }

    [HttpGet("hospital")]
    public async Task<ActionResult<ApiResponse<HospitalDashboardDto>>> GetHospitalDashboard(
        [FromQuery] Guid hospitalId,
        CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;

        if (hospitalId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
            var response = ApiResponseFactory.Failure<HospitalDashboardDto>(ApiResponseCode.HOSPITAL_ID_REQUIRED, requestId);

            return StatusCode(meta.HttpStatus, response);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            var response = ApiResponseFactory.Failure<HospitalDashboardDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId);

            return StatusCode(meta.HttpStatus, response);
        }

        var dashboard = await _dashboardAppService.GetHospitalDashboardAsync(hospitalId, tenantId, ct);

        var success = ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, dashboard, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];

        return StatusCode(successMeta.HttpStatus, success);
    }

    [HttpGet("compliance-trend")]
    public async Task<ActionResult<ApiResponse<ComplianceTrendDto>>> GetComplianceTrend(
        [FromQuery] Guid hospitalId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;

        if (hospitalId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
            var response = ApiResponseFactory.Failure<ComplianceTrendDto>(ApiResponseCode.HOSPITAL_ID_REQUIRED, requestId);

            return StatusCode(meta.HttpStatus, response);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            var response = ApiResponseFactory.Failure<ComplianceTrendDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId);

            return StatusCode(meta.HttpStatus, response);
        }

        var trend = await _dashboardAppService.GetComplianceTrendAsync(hospitalId, tenantId, from, to, ct);

        var success = ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, trend, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];

        return StatusCode(successMeta.HttpStatus, success);
    }
}

