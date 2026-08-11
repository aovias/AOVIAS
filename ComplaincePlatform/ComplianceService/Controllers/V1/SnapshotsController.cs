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
/// Controller for retrieving and managing compliance snapshots.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compliance/snapshots")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public class SnapshotsController : ControllerBase
{
    private readonly IDashboardSnapshotService _dashboardSnapshotService;
    private readonly IComplianceSnapshotRecalculationQueue _snapshotRecalculationQueue;
    private readonly ILogger<SnapshotsController> _logger;

    public SnapshotsController(
        IDashboardSnapshotService dashboardSnapshotService,
        IComplianceSnapshotRecalculationQueue snapshotRecalculationQueue,
        ILogger<SnapshotsController> logger)
    {
        _dashboardSnapshotService = dashboardSnapshotService;
        _snapshotRecalculationQueue = snapshotRecalculationQueue;
        _logger = logger;
    }

    /// <summary>
    /// Gets program overview snapshot: prefers persisted ComplianceSnapshots; otherwise computes from active ComplianceJourney + OE data.
    /// </summary>
    [HttpGet("hospital/{hospitalId}")]
    [ProducesResponseType(typeof(DashboardSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DashboardSnapshotDto>>> GetSnapshotByHospitalId(
        Guid hospitalId,
        [FromQuery] Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation(
                "Retrieving program overview snapshot for hospital {HospitalId}, framework {FrameworkId}",
                hospitalId,
                frameworkId);

            if (hospitalId == Guid.Empty)
            {
                _logger.LogWarning("Invalid hospital ID provided");

                var result = ApiResponseFactory.Failure<DashboardSnapshotDto>(
                    ApiResponseCode.HOSPITAL_ID_REQUIRED,
                    traceId);

                var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
                return StatusCode(meta.HttpStatus, result);
            }

            var dto = await _dashboardSnapshotService.GetDashboardSnapshotAsync(hospitalId, frameworkId, cancellationToken);

            _logger.LogInformation(
                "Program overview resolved for hospital {HospitalId}: available={Available}, materialized={Materialized}",
                hospitalId,
                dto.IsSnapshotAvailable,
                dto.SnapshotMaterialized);

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving compliance snapshot for hospital {HospitalId}", hospitalId);

            var result = ApiResponseFactory.Failure<DashboardSnapshotDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Queues asynchronous snapshot recalculation for a hospital+framework (matches one compliance journey).
    /// Does not block on calculation.
    /// </summary>
    [HttpPost("recalculate")]
    [ProducesResponseType(typeof(SnapshotRecalculateResponseDto), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<SnapshotRecalculateResponseDto>> QueueSnapshotRecalculation(
        [FromBody] SnapshotRecalculateRequestDto body,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (body == null || body.HospitalId == Guid.Empty || body.FrameworkId == Guid.Empty)
        {
            var fail = ApiResponseFactory.Failure<SnapshotRecalculateResponseDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, fail);
        }

        _snapshotRecalculationQueue.EnqueueHospitalFramework(body.HospitalId, body.FrameworkId);

        var dto = new SnapshotRecalculateResponseDto { Queued = true };
        var ok = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, ok);
    }
}
