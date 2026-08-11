using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hospitals")]
[Authorize]
public sealed class HospitalsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<HospitalsController> _logger;

    public HospitalsController(IMediator mediator, ILogger<HospitalsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<HospitalResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<HospitalResponseDto>>> CreateHospital(
        [FromBody] CreateHospitalRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var result = await _mediator.Send(new CreateHospitalCommand(request), cancellationToken);

            var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, success);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "CreateHospital failed: {Message}", ex.Message);
            var failure = ApiResponseFactory.Failure<HospitalResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateHospital unexpected error");
            var failure = ApiResponseFactory.Failure<HospitalResponseDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HospitalResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HospitalResponseDto>>>> ListHospitals(
        [FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (organizationId == Guid.Empty)
        {
            var failure = ApiResponseFactory.Failure<IReadOnlyList<HospitalResponseDto>>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);
            var failMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(failMeta.HttpStatus, failure);
        }

        try
        {
            var result = await _mediator.Send(new ListHospitalsByOrganizationQuery(organizationId), cancellationToken);
            var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListHospitals unexpected error for organization {OrganizationId}", organizationId);
            var failure = ApiResponseFactory.Failure<IReadOnlyList<HospitalResponseDto>>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpGet("{hospitalId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<HospitalResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<HospitalResponseDto>>> GetHospital(
        [FromRoute] Guid hospitalId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var result = await _mediator.Send(new GetHospitalQuery(hospitalId), cancellationToken);

            var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, success);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Hospital not found: {HospitalId}", hospitalId);
            var failure = ApiResponseFactory.Failure<HospitalResponseDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHospital unexpected error: {HospitalId}", hospitalId);
            var failure = ApiResponseFactory.Failure<HospitalResponseDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpPut("{hospitalId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<HospitalResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<HospitalResponseDto>>> UpdateHospital(
        [FromRoute] Guid hospitalId,
        [FromBody] UpdateHospitalRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var result = await _mediator.Send(new UpdateHospitalCommand(hospitalId, request), cancellationToken);

            var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, success);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Hospital not found: {HospitalId}", hospitalId);
            var failure = ApiResponseFactory.Failure<HospitalResponseDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "UpdateHospital failed: {Message}", ex.Message);
            var failure = ApiResponseFactory.Failure<HospitalResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateHospital unexpected error: {HospitalId}", hospitalId);
            var failure = ApiResponseFactory.Failure<HospitalResponseDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }
}

