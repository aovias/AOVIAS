using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Http;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/evidence-binder")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class EvidenceBinderController : ControllerBase
{
    private readonly IEvidenceBinderQueryService _binderQuery;
    private readonly IEvidenceBinderService _binderService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<EvidenceBinderController> _logger;

    public EvidenceBinderController(
        IEvidenceBinderQueryService binderQuery,
        IEvidenceBinderService binderService,
        IUserContextService userContext,
        ILogger<EvidenceBinderController> logger)
    {
        _binderQuery = binderQuery;
        _binderService = binderService;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<EvidenceBinderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EvidenceBinderSummaryDto>>> GetSummary(
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<EvidenceBinderSummaryDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, bad);
        }

        try
        {
            var data = await _binderQuery.GetSummaryAsync(_userContext.HospitalId, frameworkId, cancellationToken);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence binder summary failed");
            var err = ApiResponseFactory.Failure<EvidenceBinderSummaryDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus, err);
        }
    }

    [HttpGet("chapters")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EvidenceBinderChapterDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EvidenceBinderChapterDto>>>> GetChapters(
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<IReadOnlyList<EvidenceBinderChapterDto>>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, bad);
        }

        try
        {
            var data = await _binderQuery.GetChaptersAsync(_userContext.HospitalId, frameworkId, cancellationToken);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence binder chapters failed");
            var err = ApiResponseFactory.Failure<IReadOnlyList<EvidenceBinderChapterDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus, err);
        }
    }

    [HttpGet("chapters/{chapterId:guid}/objectives")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EvidenceBinderObjectiveElementDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EvidenceBinderObjectiveElementDto>>>> GetObjectives(
        Guid chapterId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty || chapterId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<IReadOnlyList<EvidenceBinderObjectiveElementDto>>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, bad);
        }

        try
        {
            var data = await _binderQuery.GetObjectivesForChapterAsync(_userContext.HospitalId, frameworkId, chapterId, cancellationToken);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence binder objectives failed");
            var err = ApiResponseFactory.Failure<IReadOnlyList<EvidenceBinderObjectiveElementDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus, err);
        }
    }

    [HttpGet("objective-elements/{objectiveElementId:guid}/overview")]
    [ProducesResponseType(typeof(ApiResponse<EvidenceBinderOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EvidenceBinderOverviewDto>>> GetOverview(
        Guid objectiveElementId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty || objectiveElementId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<EvidenceBinderOverviewDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, bad);
        }

        try
        {
            var data = await _binderQuery.GetObjectiveOverviewAsync(_userContext.HospitalId, frameworkId, objectiveElementId, cancellationToken);
            if (data is null)
            {
                var nf = ApiResponseFactory.Failure<EvidenceBinderOverviewDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, nf);
            }

            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence binder overview failed");
            var err = ApiResponseFactory.Failure<EvidenceBinderOverviewDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus, err);
        }
    }

    [HttpGet("objective-elements/{objectiveElementId:guid}/evidences")]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<EvidenceBinderEvidenceItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResultDto<EvidenceBinderEvidenceItemDto>>>> GetEvidences(
        Guid objectiveElementId,
        [FromQuery] Guid frameworkId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty || objectiveElementId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<PagedResultDto<EvidenceBinderEvidenceItemDto>>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, bad);
        }

        try
        {
            var data = await _binderQuery.GetEvidencesForObjectiveAsync(
                _userContext.HospitalId,
                frameworkId,
                objectiveElementId,
                page,
                pageSize,
                cancellationToken);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence binder evidences failed");
            var err = ApiResponseFactory.Failure<PagedResultDto<EvidenceBinderEvidenceItemDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus, err);
        }
    }

    [HttpGet("objective-elements/{objectiveElementId:guid}/checklist")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EvidenceBinderChecklistItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EvidenceBinderChecklistItemDto>>>> GetChecklist(
        Guid objectiveElementId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty || objectiveElementId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<IReadOnlyList<EvidenceBinderChecklistItemDto>>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, bad);
        }

        try
        {
            var data = await _binderQuery.GetChecklistAsync(_userContext.HospitalId, frameworkId, objectiveElementId, cancellationToken);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence binder checklist failed");
            var err = ApiResponseFactory.Failure<IReadOnlyList<EvidenceBinderChecklistItemDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus, err);
        }
    }

    [HttpGet("objective-elements/{objectiveElementId:guid}/ai-mapping")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EvidenceBinderAiMappingItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EvidenceBinderAiMappingItemDto>>>> GetAiMapping(
        Guid objectiveElementId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty || objectiveElementId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<IReadOnlyList<EvidenceBinderAiMappingItemDto>>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, bad);
        }

        try
        {
            var data = await _binderQuery.GetAiMappingAsync(_userContext.HospitalId, frameworkId, objectiveElementId, cancellationToken);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence binder AI mapping failed");
            var err = ApiResponseFactory.Failure<IReadOnlyList<EvidenceBinderAiMappingItemDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus, err);
        }
    }

    [HttpGet("objective-elements/{objectiveElementId:guid}/history")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OeHistoryEventDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OeHistoryEventDto>>>> GetHistory(
        Guid objectiveElementId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<OeHistoryEventDto>>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        if (frameworkId == Guid.Empty || objectiveElementId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<IReadOnlyList<OeHistoryEventDto>>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, bad);
        }

        try
        {
            var data = await _binderQuery.GetHistoryAsync(_userContext.HospitalId, frameworkId, objectiveElementId, tenantId, cancellationToken);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence binder history failed");
            var err = ApiResponseFactory.Failure<IReadOnlyList<OeHistoryEventDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus, err);
        }
    }

    /// <summary>Multipart upload; delegates to journey workspace (journey id resolved server-side).</summary>
    [HttpPost("objective-elements/{objectiveElementId:guid}/evidences")]
    [RequestSizeLimit(10_485_760)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> PostEvidence(
        Guid objectiveElementId,
        [FromQuery] Guid frameworkId,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        if (frameworkId == Guid.Empty || objectiveElementId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        if (!Request.HasFormContentType)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        IFormCollection form;
        try
        {
            form = await Request.ReadFormAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Evidence binder POST (multipart read)");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var correlationId = CorrelationIdAccessor.FromHttpContext(HttpContext);
        _logger.LogInformation(
            "Evidence binder multipart. CorrelationId={CorrelationId} TraceId={TraceId} ObjectiveElementId={ObjectiveElementId} FrameworkId={FrameworkId}",
            correlationId,
            traceId,
            objectiveElementId,
            frameworkId);

        var file = ResolveUploadedFile(form);
        var evidenceType = form["evidenceType"].ToString();
        if (string.IsNullOrWhiteSpace(evidenceType))
        {
            evidenceType = form["EvidenceType"].ToString();
        }

        var name = form["name"].ToString();
        if (string.IsNullOrEmpty(name))
        {
            name = form["Name"].ToString();
        }

        var title = form["title"].ToString();
        if (string.IsNullOrEmpty(title))
        {
            title = form["Title"].ToString();
        }

        var metadata = form["metadata"].ToString();
        if (string.IsNullOrEmpty(metadata))
        {
            metadata = form["Metadata"].ToString();
        }

        if (file is null || file.Length == 0)
        {
            return ApiResponseFactory.FailureResult<object>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError> { new() { Field = "file", Message = "A non-empty file is required." } },
                traceId);
        }

        if (string.IsNullOrWhiteSpace(evidenceType))
        {
            return ApiResponseFactory.FailureResult<object>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError> { new() { Field = "evidenceType", Message = "Evidence type is required." } },
                traceId);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var id = await _binderService.RegisterEvidenceFromUploadAsync(
                _userContext.HospitalId,
                frameworkId,
                objectiveElementId,
                tenantId,
                _userContext.UserId,
                stream,
                file.FileName,
                file.ContentType,
                evidenceType.Trim(),
                string.IsNullOrWhiteSpace(name) ? null : name,
                string.IsNullOrWhiteSpace(title) ? null : title,
                string.IsNullOrWhiteSpace(metadata) ? null : metadata,
                ct);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { evidenceId = id }, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Evidence binder POST upload");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    private static IFormFile? ResolveUploadedFile(IFormCollection form)
    {
        var primary = form.Files["file"];
        if (primary is { Length: > 0 })
        {
            return primary;
        }

        foreach (var key in new[] { "upload", "evidence", "files" })
        {
            var f = form.Files[key];
            if (f is { Length: > 0 })
            {
                return f;
            }
        }

        foreach (var f in form.Files)
        {
            if (f.Length > 0)
            {
                return f;
            }
        }

        return null;
    }
}
