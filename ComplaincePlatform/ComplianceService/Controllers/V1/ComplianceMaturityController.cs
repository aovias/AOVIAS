using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Provides the Compliance Maturity Dashboard endpoint.
/// Consumed exclusively via the Gateway — portal never calls this directly.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compliance")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class ComplianceMaturityController : ControllerBase
{
    private readonly IComplianceMaturityAggregationService _aggregationService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ComplianceMaturityController> _logger;

    public ComplianceMaturityController(
        IComplianceMaturityAggregationService aggregationService,
        IUserContextService userContext,
        ILogger<ComplianceMaturityController> logger)
    {
        _aggregationService = aggregationService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Returns the Compliance Maturity Dashboard for the authenticated hospital.
    /// Includes CMI, stage progression, actual Core gaps, predicted upper-stage gaps,
    /// and rules-based recommended next actions.
    /// </summary>
    [HttpGet("maturity-dashboard")]
    public async Task<ActionResult<ApiResponse<ComplianceMaturityDashboardDto>>> GetMaturityDashboard(
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        var organizationId = _userContext.OrganizationId;
        if (organizationId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            var response = ApiResponseFactory.Failure<ComplianceMaturityDashboardDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId);
            return StatusCode(meta.HttpStatus, response);
        }

        var hospitalId = _userContext.HospitalId;
        if (hospitalId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
            var response = ApiResponseFactory.Failure<ComplianceMaturityDashboardDto>(
                ApiResponseCode.HOSPITAL_ID_REQUIRED, requestId);
            return StatusCode(meta.HttpStatus, response);
        }

        _logger.LogInformation(
            "Building compliance maturity dashboard for HospitalId={HospitalId} OrgId={OrgId}",
            hospitalId, organizationId);

        var dashboard = await _aggregationService.GetDashboardAsync(
            hospitalId, organizationId, cancellationToken);

        var success = ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, dashboard, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];

        return StatusCode(successMeta.HttpStatus, success);
    }
}
