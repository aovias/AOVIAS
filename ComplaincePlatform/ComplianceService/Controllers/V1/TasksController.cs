using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
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
public class TasksController : ControllerBase
{
    private readonly IComplianceProgramRepository _programRepository;
    private readonly IUserContextService _userContext;
    private readonly ILogger<TasksController> _logger;
    private readonly ComplianceCommandService _complianceCommandService;
    private readonly IComplianceTaskAppService _complianceTaskAppService;

    public TasksController(
        IComplianceProgramRepository programRepository,
        IUserContextService userContext,
        ILogger<TasksController> logger,
        ComplianceCommandService complianceCommandService,
        IComplianceTaskAppService complianceTaskAppService)
    {
        _programRepository = programRepository;
        _userContext = userContext;
        _logger = logger;
        _complianceCommandService = complianceCommandService;
        _complianceTaskAppService = complianceTaskAppService;
    }

    [Obsolete("Use POST /compliance/compliance-tasks instead.")]
    [HttpPost("programs/{programId}/tasks")]
    public Task<IActionResult> Assign(Guid programId, [FromBody] AssignTasksRequest? request, CancellationToken ct)
    {
        _logger.LogWarning("Deprecated program-task assign endpoint invoked for program {ProgramId}", programId);
        var traceId = HttpContext.TraceIdentifier;
        var envelope = ApiResponseFactory.Failure<object>(
            ApiResponseCode.VALIDATION_FAILED,
            new List<ApiError>
            {
                new()
                {
                    Message =
                        "This endpoint is no longer supported. Create tasks with POST /api/v1/compliance/compliance-tasks."
                }
            },
            traceId);

        return Task.FromResult<IActionResult>(new ObjectResult(envelope) { StatusCode = StatusCodes.Status410Gone });
    }

    [HttpGet("programs/{programId}/tasks")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ComplianceTaskListItemDto>>>> GetByProgramId(
        Guid programId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (programId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<IReadOnlyList<ComplianceTaskListItemDto>>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var program = await _programRepository.GetAsync(programId);
        if (program == null)
        {
            var result = ApiResponseFactory.Failure<IReadOnlyList<ComplianceTaskListItemDto>>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }

        var tasks = await _complianceTaskAppService.ListAsync(program.ComplianceFrameworkId, ct);

        var response = ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            tasks,
            traceId);

        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }

    [HttpPut("frameworks/{frameworkId}/tasks/{taskId}/complete")]
    public async Task<ActionResult<ApiResponse<object>>> CompleteTask(Guid frameworkId, Guid taskId, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        // Basic validation
        var validationErrors = new Dictionary<string, string[]>();

        if (frameworkId == Guid.Empty)
        {
            validationErrors[nameof(frameworkId)] = new[] { "FrameworkId is required." };
        }

        if (taskId == Guid.Empty)
        {
            validationErrors[nameof(taskId)] = new[] { "TaskId is required." };
        }

        if (validationErrors.Count > 0)
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var hospitalId = _userContext.HospitalId;

            await _complianceCommandService.CompleteTaskAsync(hospitalId, frameworkId, taskId, ct);

            _logger.LogInformation(
                "Completed task {TaskId} for hospital {HospitalId} and framework {FrameworkId}",
                taskId, hospitalId, frameworkId);

            var data = new
            {
                TaskId = taskId,
                FrameworkId = frameworkId,
                HospitalId = hospitalId
            };

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)data,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "EVIDENCE_PENDING_REVIEW", StringComparison.Ordinal))
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError>
                {
                    new()
                    {
                        Message =
                            "This task has evidence pending review. Approve or reject each item before completing the task."
                    }
                },
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Error completing task {TaskId} for hospital {HospitalId} and framework {FrameworkId}",
                taskId, _userContext.HospitalId, frameworkId);

            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }
    }
}

public record UpdateTaskStatusRequest
{
    public string Status { get; init; } = string.Empty;
}

public record AssignTasksRequest
{
    public Guid? HospitalId { get; init; } // Optional - will be validated against token
}
