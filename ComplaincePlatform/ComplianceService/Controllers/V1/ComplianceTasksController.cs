using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compliance")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public class ComplianceTasksController : ControllerBase
{
    private readonly IComplianceTaskAppService _taskAppService;
    private readonly ILogger<ComplianceTasksController> _logger;

    public ComplianceTasksController(
        IComplianceTaskAppService taskAppService,
        ILogger<ComplianceTasksController> logger)
    {
        _taskAppService = taskAppService;
        _logger = logger;
    }

    [HttpGet("compliance-tasks")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ComplianceTaskListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ComplianceTaskListItemDto>>>> List(
        [FromQuery] Guid? frameworkId,
        [FromQuery] TaskCategory? category,
        [FromQuery] TaskKind? taskKind,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var data = await _taskAppService.ListAsync(frameworkId, cancellationToken, category, taskKind);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list compliance tasks");
            var result = ApiResponseFactory.Failure<IReadOnlyList<ComplianceTaskListItemDto>>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    [HttpGet("compliance-tasks/{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ComplianceTaskDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ComplianceTaskDetailDto>>> GetById(Guid taskId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var dto = await _taskAppService.GetByIdAsync(taskId, cancellationToken);
        if (dto == null)
        {
            var notFound = ApiResponseFactory.Failure<ComplianceTaskDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, notFound);
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
        var ok = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(ok.HttpStatus, response);
    }

    [HttpPost("compliance-tasks")]
    [ProducesResponseType(typeof(ApiResponse<ComplianceTaskDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ComplianceTaskDetailDto>>> Create(
        [FromBody] CreateComplianceTaskRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var dto = await _taskAppService.CreateAsync(request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation failed creating compliance task");
            var result = ApiResponseFactory.Failure<ComplianceTaskDetailDto>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError> { new() { Message = ex.Message } },
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Could not create compliance task");
            var code = ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                ? ApiResponseCode.RESOURCE_CONFLICT
                : ApiResponseCode.VALIDATION_FAILED;
            var result = ApiResponseFactory.Failure<ComplianceTaskDetailDto>(
                code,
                new List<ApiError> { new() { Message = ex.Message } },
                traceId);
            var meta = ApiResponseCodes.Registry[code];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    [HttpPatch("compliance-tasks/{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ComplianceTaskDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ComplianceTaskDetailDto>>> Patch(
        Guid taskId,
        [FromBody] PatchComplianceTaskRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var dto = await _taskAppService.PatchAsync(taskId, request, cancellationToken);
            if (dto == null)
            {
                var notFound = ApiResponseFactory.Failure<ComplianceTaskDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(meta.HttpStatus, notFound);
            }

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
            var ok = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(ok.HttpStatus, response);
        }
        catch (ArgumentException ex)
        {
            var result = ApiResponseFactory.Failure<ComplianceTaskDetailDto>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError> { new() { Message = ex.Message } },
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (InvalidOperationException ex)
        {
            var result = ApiResponseFactory.Failure<ComplianceTaskDetailDto>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError> { new() { Message = ex.Message } },
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }
    }
}
