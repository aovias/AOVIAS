using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.TaskExecution;
using ComplianceService.Domain;
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
[Route("api/v{version:apiVersion}/tasks")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class TasksV1Controller : ControllerBase
{
    private readonly ITaskQueryService _query;
    private readonly ITaskExecutionService _execution;
    private readonly ILogger<TasksV1Controller> _logger;

    public TasksV1Controller(
        ITaskQueryService query,
        ITaskExecutionService execution,
        ILogger<TasksV1Controller> logger)
    {
        _query = query;
        _execution = execution;
        _logger = logger;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<TaskSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TaskSummaryDto>>> Summary(
        [FromQuery] Guid frameworkId,
        [FromQuery] TaskApiStatus? status,
        [FromQuery] ComplianceTaskPriority? priority,
        [FromQuery] string? ownerId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? chapterId,
        [FromQuery] TaskRiskLevelApi? riskLevel,
        [FromQuery] string? search,
        [FromQuery] bool? overdueOnly,
        [FromQuery] bool? dueThisWeek,
        [FromQuery] bool? focus,
        [FromQuery] DeficiencySeverity? deficiencySeverity,
        [FromQuery] DeficiencyType? deficiencyType,
        [FromQuery] ObjectiveElementClassification? maturityStage,
        [FromQuery] EvidenceStatus? obligationEvidenceStatus,
        [FromQuery] VerificationStatus? obligationVerificationStatus,
        [FromQuery] bool? deficiencyLinkedOnly,
        [FromQuery] TaskCategory? category,
        [FromQuery] TaskKind? taskKind,
        [FromQuery] bool? teamOnly,
        [FromQuery] DateTime? dueFrom,
        [FromQuery] DateTime? dueTo,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<TaskSummaryDto>(traceId);
        }

        var spec = BuildSpec(
            frameworkId,
            status,
            priority,
            ownerId,
            departmentId,
            chapterId,
            riskLevel,
            search,
            overdueOnly,
            dueThisWeek,
            focus,
            deficiencySeverity,
            deficiencyType,
            maturityStage,
            obligationEvidenceStatus,
            obligationVerificationStatus,
            deficiencyLinkedOnly,
            category,
            taskKind,
            teamOnly,
            dueFrom,
            dueTo,
            view: "table");
        var data = await _query.GetSummaryAsync(spec, ct);
        return OkSuccess(data, traceId);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<TaskListItemResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskKanbanResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid frameworkId,
        [FromQuery] string? view,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] TaskApiStatus? status,
        [FromQuery] ComplianceTaskPriority? priority,
        [FromQuery] string? ownerId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? chapterId,
        [FromQuery] TaskRiskLevelApi? riskLevel,
        [FromQuery] string? search,
        [FromQuery] bool? overdueOnly,
        [FromQuery] bool? dueThisWeek,
        [FromQuery] bool? focus,
        [FromQuery] DeficiencySeverity? deficiencySeverity,
        [FromQuery] DeficiencyType? deficiencyType,
        [FromQuery] ObjectiveElementClassification? maturityStage,
        [FromQuery] EvidenceStatus? obligationEvidenceStatus,
        [FromQuery] VerificationStatus? obligationVerificationStatus,
        [FromQuery] bool? deficiencyLinkedOnly,
        [FromQuery] TaskCategory? category,
        [FromQuery] TaskKind? taskKind,
        [FromQuery] bool? teamOnly,
        [FromQuery] DateTime? dueFrom,
        [FromQuery] DateTime? dueTo,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return new ObjectResult(bad)
            {
                StatusCode = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus
            };
        }

        var v = string.IsNullOrWhiteSpace(view) ? "table" : view.Trim().ToLowerInvariant();
        var spec = BuildSpec(
            frameworkId,
            status,
            priority,
            ownerId,
            departmentId,
            chapterId,
            riskLevel,
            search,
            overdueOnly,
            dueThisWeek,
            focus,
            deficiencySeverity,
            deficiencyType,
            maturityStage,
            obligationEvidenceStatus,
            obligationVerificationStatus,
            deficiencyLinkedOnly,
            category,
            taskKind,
            teamOnly,
            dueFrom,
            dueTo,
            v);

        if (v == "kanban")
        {
            var kanban = await _query.GetKanbanAsync(spec, ct);
            Response.Headers.Append("X-Truncated", kanban.Truncated ? "true" : "false");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, kanban, traceId));
        }

        if (!page.HasValue || !pageSize.HasValue || page < 1 || pageSize < 1)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError> { new() { Message = "page and pageSize are required for table/risk views." } },
                    traceId));
        }

        var paged = await _query.GetPagedAsync(spec, page.Value, pageSize.Value, ct);
        return StatusCode(
            ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.SUCCESS, paged, traceId));
    }

    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDetailResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TaskDetailResponseDto>>> GetById(
        Guid taskId,
        [FromQuery] Guid frameworkId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<TaskDetailResponseDto>(traceId);
        }

        var dto = await _query.GetDetailAsync(taskId, frameworkId, ct);
        if (dto == null)
        {
            return NotFoundFailure<TaskDetailResponseDto>(traceId);
        }

        return OkSuccess(dto, traceId);
    }

    [HttpGet("{taskId:guid}/evidences")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<TaskEvidenceItemDto>>>> Evidences(
        Guid taskId,
        [FromQuery] Guid frameworkId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<PagedResultDto<TaskEvidenceItemDto>>(traceId);
        }

        var data = await _query.GetMergedEvidencesAsync(taskId, frameworkId, page, pageSize, ct);
        return OkSuccess(data, traceId);
    }

    [HttpGet("{taskId:guid}/comments")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<TaskCommentDto>>>> Comments(
        Guid taskId,
        [FromQuery] Guid frameworkId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<PagedResultDto<TaskCommentDto>>(traceId);
        }

        var data = await _query.GetCommentsAsync(taskId, frameworkId, page, pageSize, ct);
        return OkSuccess(data, traceId);
    }

    [HttpGet("{taskId:guid}/history")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<TaskHistoryEntryDto>>>> History(
        Guid taskId,
        [FromQuery] Guid frameworkId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<PagedResultDto<TaskHistoryEntryDto>>(traceId);
        }

        var data = await _query.GetHistoryAsync(taskId, frameworkId, page, pageSize, ct);
        return OkSuccess(data, traceId);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskDetailResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TaskDetailResponseDto>>> Create(
        [FromQuery] Guid frameworkId,
        [FromBody] CreateTaskExecutionRequestDto body,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<TaskDetailResponseDto>(traceId);
        }

        try
        {
            var dto = await _execution.CreateAsync(frameworkId, body, ct);
            return OkSuccess(dto, traceId);
        }
        catch (ArgumentException ex)
        {
            return ValidationFailure<TaskDetailResponseDto>(traceId, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ValidationFailure<TaskDetailResponseDto>(traceId, ex.Message);
        }
    }

    [HttpPatch("{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<TaskDetailResponseDto>>> Patch(
        Guid taskId,
        [FromQuery] Guid frameworkId,
        [FromBody] PatchTaskExecutionRequestDto body,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<TaskDetailResponseDto>(traceId);
        }

        var ifMatch = Request.Headers.IfMatch.FirstOrDefault();
        var token = RowVersionCodec.FromBase64(ifMatch);
        if (token == null || token.Length == 0)
        {
            return StatusCode(
                StatusCodes.Status428PreconditionRequired,
                ApiResponseFactory.Failure<TaskDetailResponseDto>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError> { new() { Message = "If-Match with base64 rowVersion is required." } },
                    traceId));
        }

        try
        {
            var dto = await _execution.PatchAsync(frameworkId, taskId, body, token, ct);
            if (dto == null)
            {
                return NotFoundFailure<TaskDetailResponseDto>(traceId);
            }

            return OkSuccess(dto, traceId);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "CONCURRENCY_CONFLICT", StringComparison.Ordinal))
        {
            return StatusCode(
                StatusCodes.Status412PreconditionFailed,
                ApiResponseFactory.Failure<TaskDetailResponseDto>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError> { new() { Message = "The task was modified by another user." } },
                    traceId));
        }
        catch (ArgumentException ex)
        {
            return ValidationFailure<TaskDetailResponseDto>(traceId, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ValidationFailure<TaskDetailResponseDto>(traceId, ex.Message);
        }
    }

    [HttpPut("{taskId:guid}/complete")]
    public async Task<ActionResult<ApiResponse<object>>> Complete(
        Guid taskId,
        [FromQuery] Guid frameworkId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<object>(traceId);
        }

        var ifMatch = Request.Headers.IfMatch.FirstOrDefault();
        var token = RowVersionCodec.FromBase64(ifMatch);
        if (token == null || token.Length == 0)
        {
            return StatusCode(
                StatusCodes.Status428PreconditionRequired,
                ApiResponseFactory.Failure<object>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError> { new() { Message = "If-Match with base64 rowVersion is required." } },
                    traceId));
        }

        try
        {
            await _execution.CompleteAsync(frameworkId, taskId, token, ct);
            return OkSuccess((object)new { taskId }, traceId);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "CONCURRENCY_CONFLICT", StringComparison.Ordinal))
        {
            return StatusCode(
                StatusCodes.Status412PreconditionFailed,
                ApiResponseFactory.Failure<object>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError> { new() { Message = "The task was modified by another user." } },
                    traceId));
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "EVIDENCE_PENDING_REVIEW", StringComparison.Ordinal))
        {
            return ValidationFailure<object>(
                traceId,
                "This task has evidence pending review. Approve or reject each item before completing the task.");
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, TaskDependencyGate.BlockedByIncomplete, StringComparison.Ordinal))
        {
            return ValidationFailure<object>(
                traceId,
                "This task is blocked by an incomplete dependency. Complete the blocking task first.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Complete task failed");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpPost("{taskId:guid}/reassign")]
    [ProducesResponseType(typeof(ApiResponse<TaskDetailResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TaskDetailResponseDto>>> Reassign(
        Guid taskId,
        [FromQuery] Guid frameworkId,
        [FromBody] ReassignTaskRequestDto body,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<TaskDetailResponseDto>(traceId);
        }

        var ifMatch = Request.Headers.IfMatch.FirstOrDefault();
        var token = RowVersionCodec.FromBase64(ifMatch);
        if (token == null || token.Length == 0)
        {
            return StatusCode(
                StatusCodes.Status428PreconditionRequired,
                ApiResponseFactory.Failure<TaskDetailResponseDto>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError> { new() { Message = "If-Match with base64 rowVersion is required." } },
                    traceId));
        }

        try
        {
            var dto = await _execution.ReassignAsync(frameworkId, taskId, body, token, ct);
            if (dto == null)
            {
                return NotFoundFailure<TaskDetailResponseDto>(traceId);
            }

            return OkSuccess(dto, traceId);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "CONCURRENCY_CONFLICT", StringComparison.Ordinal))
        {
            return StatusCode(
                StatusCodes.Status412PreconditionFailed,
                ApiResponseFactory.Failure<TaskDetailResponseDto>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError> { new() { Message = "The task was modified by another user." } },
                    traceId));
        }
        catch (ArgumentException ex)
        {
            return ValidationFailure<TaskDetailResponseDto>(traceId, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ValidationFailure<TaskDetailResponseDto>(traceId, ex.Message);
        }
    }

    [HttpPost("{taskId:guid}/comments")]
    public async Task<ActionResult<ApiResponse<TaskCommentDto>>> AddComment(
        Guid taskId,
        [FromQuery] Guid frameworkId,
        [FromBody] AddTaskCommentRequestDto body,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<TaskCommentDto>(traceId);
        }

        try
        {
            var dto = await _execution.AddCommentAsync(frameworkId, taskId, body, ct);
            return OkSuccess(dto, traceId);
        }
        catch (InvalidOperationException ex)
        {
            return NotFoundFailure<TaskCommentDto>(traceId);
        }
        catch (ArgumentException ex)
        {
            return ValidationFailure<TaskCommentDto>(traceId, ex.Message);
        }
    }

    [HttpPost("{taskId:guid}/evidences")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ApiResponse<object>>> UploadEvidence(
        Guid taskId,
        [FromQuery] Guid frameworkId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            return BadRequestFailure<object>(traceId);
        }

        try
        {
            var id = await _execution.UploadEvidenceAsync(frameworkId, taskId, Request, ct);
            return OkSuccess((object)new { evidenceId = id }, traceId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Task evidence upload");
            return ValidationFailure<object>(traceId, ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.FORBIDDEN, traceId));
        }
    }

    private static TaskQuerySpec BuildSpec(
        Guid frameworkId,
        TaskApiStatus? status,
        ComplianceTaskPriority? priority,
        string? ownerId,
        Guid? departmentId,
        Guid? chapterId,
        TaskRiskLevelApi? riskLevel,
        string? search,
        bool? overdueOnly,
        bool? dueThisWeek,
        bool? focus,
        DeficiencySeverity? deficiencySeverity,
        DeficiencyType? deficiencyType,
        ObjectiveElementClassification? maturityStage,
        EvidenceStatus? obligationEvidenceStatus,
        VerificationStatus? obligationVerificationStatus,
        bool? deficiencyLinkedOnly,
        TaskCategory? category,
        TaskKind? taskKind,
        bool? teamOnly,
        DateTime? dueFrom,
        DateTime? dueTo,
        string view) =>
        new()
        {
            FrameworkId = frameworkId,
            Status = status,
            Priority = priority,
            OwnerId = ownerId,
            DepartmentId = departmentId,
            ChapterId = chapterId,
            RiskLevel = riskLevel,
            Search = search,
            OverdueOnly = overdueOnly,
            DueThisWeek = dueThisWeek,
            FocusCriticalOverdue = focus,
            DeficiencySeverity = deficiencySeverity,
            DeficiencyType = deficiencyType,
            MaturityStage = maturityStage,
            ObligationEvidenceStatus = obligationEvidenceStatus,
            ObligationVerificationStatus = obligationVerificationStatus,
            DeficiencyLinkedOnly = deficiencyLinkedOnly,
            Category = category,
            TaskKind = taskKind,
            TeamOnly = teamOnly,
            DueFrom = dueFrom,
            DueTo = dueTo,
            View = view
        };

    private static ActionResult<ApiResponse<T>> BadRequestFailure<T>(string traceId) =>
        new ObjectResult(ApiResponseFactory.Failure<T>(ApiResponseCode.VALIDATION_FAILED, traceId))
        {
            StatusCode = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus
        };

    private static ActionResult<ApiResponse<T>> NotFoundFailure<T>(string traceId) =>
        new ObjectResult(ApiResponseFactory.Failure<T>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId))
        {
            StatusCode = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus
        };

    private static ActionResult<ApiResponse<T>> ValidationFailure<T>(string traceId, string message) =>
        new ObjectResult(
            ApiResponseFactory.Failure<T>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError> { new() { Message = message } },
                traceId))
        {
            StatusCode = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus
        };

    private static ActionResult<ApiResponse<T>> OkSuccess<T>(T data, string traceId) =>
        new ObjectResult(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId))
        {
            StatusCode = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus
        };
}
