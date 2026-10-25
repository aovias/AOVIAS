using Gateway.Application.Queries;
using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Dashboard.Models;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IMediator mediator,
        ILogger<DashboardController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        // Extract hospital_id from JWT token
        var hospitalIdClaim = User.FindFirst("hospital_id")?.Value;
        if (string.IsNullOrEmpty(hospitalIdClaim) || !Guid.TryParse(hospitalIdClaim, out var hospitalId))
        {
            _logger.LogWarning("Missing or invalid hospital_id claim in JWT token");

            var errors = new List<ApiError>
            {
                new ApiError
                {
                    Message = "Missing or invalid hospital_id claim in token"
                }
            };

            var response = ApiResponseFactory.Failure<DashboardSummaryDto>(ApiResponseCode.UNAUTHORIZED, errors, requestId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.UNAUTHORIZED];

            return StatusCode(meta.HttpStatus, response);
        }

        var query = new GetDashboardSummaryQuery(hospitalId);
        var summary = await _mediator.Send(query, cancellationToken);

        // This endpoint is a BFF aggregator; wrap the DTO for the UI
        var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, summary, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];

        return StatusCode(successMeta.HttpStatus, success);
    }

    [HttpGet("program-overview-snapshot")]
    [HttpGet("{hospitalId}/program-overview-snapshot")]
    public async Task<ActionResult<ApiResponse<DashboardSnapshotDto>>> GetProgramOverviewSnapshot(
        [FromRoute] Guid? hospitalId,
        [FromQuery] Guid? hospitalIdQuery,
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        // Support both path-based and query-based hospitalId
        var actualHospitalId = hospitalId ?? hospitalIdQuery;
        
        if (actualHospitalId == null || actualHospitalId == Guid.Empty)
        {
            _logger.LogWarning("Invalid hospital ID provided for program overview snapshot");

            var errors = new List<ApiError>
            {
                new ApiError
                {
                    Field = nameof(hospitalId),
                    Message = "Hospital ID is required"
                }
            };

            var response = ApiResponseFactory.Failure<DashboardSnapshotDto>(ApiResponseCode.VALIDATION_FAILED, errors, requestId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];

            return StatusCode(meta.HttpStatus, response);
        }

        // Extract hospital_id from JWT token for validation
        var hospitalIdClaim = User.FindFirst("hospital_id")?.Value;
        if (string.IsNullOrEmpty(hospitalIdClaim) || !Guid.TryParse(hospitalIdClaim, out var tokenHospitalId))
        {
            _logger.LogWarning("Missing or invalid hospital_id claim in JWT token");

            var errors = new List<ApiError>
            {
                new ApiError
                {
                    Message = "Missing or invalid hospital_id claim in token"
                }
            };

            var response = ApiResponseFactory.Failure<DashboardSnapshotDto>(ApiResponseCode.UNAUTHORIZED, errors, requestId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.UNAUTHORIZED];

            return StatusCode(meta.HttpStatus, response);
        }

        // Validate that the requested hospital ID matches the token's hospital ID
        if (actualHospitalId.Value != tokenHospitalId)
        {
            _logger.LogWarning(
                "Hospital ID mismatch: requested {RequestedHospitalId} does not match token {TokenHospitalId}",
                actualHospitalId.Value, tokenHospitalId);

            var errors = new List<ApiError>
            {
                new ApiError
                {
                    Message = "Access denied: HospitalId does not match token."
                }
            };

            var response = ApiResponseFactory.Failure<DashboardSnapshotDto>(ApiResponseCode.FORBIDDEN, errors, requestId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];

            return StatusCode(meta.HttpStatus, response);
        }

        var query = new GetProgramOverviewSnapshotQuery(actualHospitalId.Value, frameworkId);
            var snapshot = await _mediator.Send(query, cancellationToken);

            var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, snapshot, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];

        return StatusCode(successMeta.HttpStatus, success);
    }

    [HttpGet("hospital")]
    public async Task<ActionResult<ApiResponse<HospitalDashboardDto>>> GetHospitalDashboard(
        [FromQuery] Guid? hospitalId,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        var actualHospitalId = hospitalId ?? Guid.Empty;

        if (actualHospitalId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
            var response = ApiResponseFactory.Failure<HospitalDashboardDto>(ApiResponseCode.HOSPITAL_ID_REQUIRED, requestId);
            return StatusCode(meta.HttpStatus, response);
        }

        var hospitalIdClaim = User.FindFirst("hospital_id")?.Value;
        if (string.IsNullOrEmpty(hospitalIdClaim) || !Guid.TryParse(hospitalIdClaim, out var tokenHospitalId))
        {
            _logger.LogWarning("Missing or invalid hospital_id claim in JWT token");

            var errors = new List<ApiError>
            {
                new ApiError
                {
                    Message = "Missing or invalid hospital_id claim in token"
                }
            };

            var response = ApiResponseFactory.Failure<HospitalDashboardDto>(ApiResponseCode.UNAUTHORIZED, errors, requestId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.UNAUTHORIZED];

            return StatusCode(meta.HttpStatus, response);
        }

        if (tokenHospitalId != actualHospitalId)
        {
            _logger.LogWarning(
                "Hospital ID mismatch: requested {RequestedHospitalId} does not match token {TokenHospitalId}",
                actualHospitalId, tokenHospitalId);

            var errors = new List<ApiError>
            {
                new ApiError
                {
                    Message = "Access denied: HospitalId does not match token."
                }
            };

            var response = ApiResponseFactory.Failure<HospitalDashboardDto>(ApiResponseCode.FORBIDDEN, errors, requestId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];

            return StatusCode(meta.HttpStatus, response);
        }

        var dashboard = await _mediator.Send(new GetHospitalDashboardQuery(actualHospitalId), cancellationToken);
        var success = ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, dashboard, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];

        return StatusCode(successMeta.HttpStatus, success);
    }

    [HttpGet("experience")]
    public async Task<ActionResult<ApiResponse<DashboardExperienceResult>>> GetExperience(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;

        var hospitalIdClaim = User.FindFirst("hospital_id")?.Value;
        if (string.IsNullOrEmpty(hospitalIdClaim) || !Guid.TryParse(hospitalIdClaim, out var tokenHospitalId))
        {
            _logger.LogWarning("Missing or invalid hospital_id claim in JWT token");

            var errors = new List<ApiError>
            {
                new ApiError { Message = "Missing or invalid hospital_id claim in token" },
            };

            var response = ApiResponseFactory.Failure<DashboardExperienceResult>(
                ApiResponseCode.UNAUTHORIZED,
                errors,
                requestId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.UNAUTHORIZED];
            return StatusCode(meta.HttpStatus, response);
        }

        var actualHospitalId = hospitalId ?? tokenHospitalId;
        if (actualHospitalId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
            var response = ApiResponseFactory.Failure<DashboardExperienceResult>(
                ApiResponseCode.HOSPITAL_ID_REQUIRED,
                requestId);
            return StatusCode(meta.HttpStatus, response);
        }

        if (actualHospitalId != tokenHospitalId)
        {
            _logger.LogWarning(
                "Hospital ID mismatch: requested {RequestedHospitalId} does not match token {TokenHospitalId}",
                actualHospitalId,
                tokenHospitalId);

            var errors = new List<ApiError>
            {
                new ApiError { Message = "Access denied: HospitalId does not match token." },
            };

            var response = ApiResponseFactory.Failure<DashboardExperienceResult>(
                ApiResponseCode.FORBIDDEN,
                errors,
                requestId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, response);
        }

        var experience = await _mediator.Send(
            new GetDashboardExperienceQuery(actualHospitalId, frameworkId, forceRefresh),
            cancellationToken);

        var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, experience, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, success);
    }
}

