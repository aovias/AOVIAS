using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/evidence")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class EvidenceLibraryController : ControllerBase
{
    private readonly IEvidenceLibraryService _evidenceLibrary;
    private readonly IEvidenceService _evidenceService;
    private readonly IEvidenceReviewService _evidenceReview;
    private readonly IComplianceSnapshotRecalculationQueue _snapshotRecalculationQueue;
    private readonly ComplianceDbContext _db;
    private readonly IUserContextService _userContext;
    private readonly ILogger<EvidenceLibraryController> _logger;

    public EvidenceLibraryController(
        IEvidenceLibraryService evidenceLibrary,
        IEvidenceService evidenceService,
        IEvidenceReviewService evidenceReview,
        IComplianceSnapshotRecalculationQueue snapshotRecalculationQueue,
        ComplianceDbContext db,
        IUserContextService userContext,
        ILogger<EvidenceLibraryController> logger)
    {
        _evidenceLibrary = evidenceLibrary;
        _evidenceService = evidenceService;
        _evidenceReview = evidenceReview;
        _snapshotRecalculationQueue = snapshotRecalculationQueue;
        _db = db;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<EvidenceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EvidenceSummaryDto>>> GetSummary(
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<EvidenceSummaryDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var badMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(badMeta.HttpStatus, bad);
        }

        try
        {
            var data = await _evidenceLibrary.GetSummaryAsync(_userContext.HospitalId, frameworkId, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence summary failed");
            var err = ApiResponseFactory.Failure<EvidenceSummaryDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(em.HttpStatus, err);
        }
    }

    [HttpGet("list")]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<EvidenceListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResultDto<EvidenceListItemDto>>>> GetList(
        [FromQuery] Guid frameworkId,
        [FromQuery] Guid? objectiveElementId,
        [FromQuery] Guid? chapterId,
        [FromQuery] Guid? standardId,
        [FromQuery] string? status,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? fileType,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<PagedResultDto<EvidenceListItemDto>>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var badMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(badMeta.HttpStatus, bad);
        }

        try
        {
            var data = await _evidenceLibrary.GetListAsync(
                _userContext.HospitalId,
                frameworkId,
                objectiveElementId,
                chapterId,
                standardId,
                status,
                departmentId,
                fileType,
                search,
                page,
                pageSize,
                sortBy,
                sortOrder,
                cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence list failed");
            var err = ApiResponseFactory.Failure<PagedResultDto<EvidenceListItemDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(em.HttpStatus, err);
        }
    }

    [HttpGet("analytics")]
    [ProducesResponseType(typeof(ApiResponse<EvidenceAnalyticsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EvidenceAnalyticsDto>>> GetAnalytics(
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<EvidenceAnalyticsDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var badMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(badMeta.HttpStatus, bad);
        }

        try
        {
            var data = await _evidenceLibrary.GetAnalyticsAsync(_userContext.HospitalId, frameworkId, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence analytics failed");
            var err = ApiResponseFactory.Failure<EvidenceAnalyticsDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(em.HttpStatus, err);
        }
    }

    [HttpGet("recent-activity")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EvidenceActivityItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EvidenceActivityItemDto>>>> GetRecentActivity(
        [FromQuery] Guid frameworkId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<IReadOnlyList<EvidenceActivityItemDto>>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var badMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(badMeta.HttpStatus, bad);
        }

        try
        {
            var data = await _evidenceLibrary.GetRecentActivityAsync(_userContext.HospitalId, frameworkId, take, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence recent activity failed");
            var err = ApiResponseFactory.Failure<IReadOnlyList<EvidenceActivityItemDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(em.HttpStatus, err);
        }
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<EvidenceDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EvidenceDashboardDto>>> GetDashboard(
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<EvidenceDashboardDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var badMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(badMeta.HttpStatus, bad);
        }

        try
        {
            var data = await _evidenceLibrary.GetDashboardAsync(_userContext.HospitalId, frameworkId, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence dashboard failed");
            var err = ApiResponseFactory.Failure<EvidenceDashboardDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(em.HttpStatus, err);
        }
    }

    [HttpGet("upload-context")]
    [ProducesResponseType(typeof(ApiResponse<EvidenceUploadContextDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EvidenceUploadContextDto>>> GetUploadContext(
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<EvidenceUploadContextDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var badMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(badMeta.HttpStatus, bad);
        }

        try
        {
            var data = await _evidenceLibrary.GetUploadContextAsync(_userContext.HospitalId, frameworkId, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence upload-context failed");
            var err = ApiResponseFactory.Failure<EvidenceUploadContextDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(em.HttpStatus, err);
        }
    }

    [HttpGet("{evidenceId:guid}/detail")]
    [ProducesResponseType(typeof(ApiResponse<EvidenceDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EvidenceDetailDto>>> GetEvidenceDetail(
        Guid evidenceId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty || evidenceId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<EvidenceDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var badMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(badMeta.HttpStatus, bad);
        }

        try
        {
            var data = await _evidenceLibrary.GetLibraryEvidenceDetailAsync(
                _userContext.HospitalId,
                frameworkId,
                evidenceId,
                cancellationToken);
            if (data == null)
            {
                var nf = ApiResponseFactory.Failure<EvidenceDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                var nm = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(nm.HttpStatus, nf);
            }

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence library detail failed for EvidenceId={EvidenceId}", evidenceId);
            var err = ApiResponseFactory.Failure<EvidenceDetailDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(em.HttpStatus, err);
        }
    }

    [HttpGet("{evidenceId:guid}/download")]
    public async Task<IActionResult> DownloadEvidence(
        Guid evidenceId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty || evidenceId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var badMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(badMeta.HttpStatus, bad);
        }

        try
        {
            var result = await _evidenceLibrary.DownloadLibraryEvidenceAsync(
                _userContext.HospitalId,
                frameworkId,
                evidenceId,
                cancellationToken);
            if (result == null)
            {
                var nf = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                var nm = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(nm.HttpStatus, nf);
            }

            var (stream, contentType, fileName) = result.Value;
            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence library download failed for EvidenceId={EvidenceId}", evidenceId);
            var err = ApiResponseFactory.Failure<object>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(em.HttpStatus, err);
        }
    }

    [HttpGet("review-queue")]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<EvidenceReviewQueueItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResultDto<EvidenceReviewQueueItemDto>>>> GetReviewQueue(
        [FromQuery] Guid frameworkId,
        [FromQuery] string? status,
        [FromQuery] string? stage,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? ownerId,
        [FromQuery] string? evidenceType,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<PagedResultDto<EvidenceReviewQueueItemDto>>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, bad);
        }

        try
        {
            var data = await _evidenceReview.GetReviewQueueAsync(
                _userContext.HospitalId,
                _userContext.OrganizationId,
                frameworkId,
                new EvidenceReviewQueueQuery
                {
                    Status = status,
                    Stage = stage,
                    DepartmentId = departmentId,
                    OwnerId = ownerId,
                    EvidenceType = evidenceType,
                    Search = search,
                    Page = page,
                    PageSize = pageSize
                },
                cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence review queue failed");
            var err = ApiResponseFactory.Failure<PagedResultDto<EvidenceReviewQueueItemDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus, err);
        }
    }

    [HttpGet("{evidenceId:guid}/audit-trail")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EvidenceReviewAuditEntryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EvidenceReviewAuditEntryDto>>>> GetAuditTrail(
        Guid evidenceId,
        [FromQuery] Guid frameworkId,
        [FromQuery] Guid? objectiveElementId,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (frameworkId == Guid.Empty || evidenceId == Guid.Empty)
        {
            var bad = ApiResponseFactory.Failure<IReadOnlyList<EvidenceReviewAuditEntryDto>>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, bad);
        }

        try
        {
            var data = await _evidenceReview.GetAuditTrailAsync(
                _userContext.HospitalId,
                frameworkId,
                evidenceId,
                objectiveElementId,
                cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence audit trail failed for EvidenceId={EvidenceId}", evidenceId);
            var err = ApiResponseFactory.Failure<IReadOnlyList<EvidenceReviewAuditEntryDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus, err);
        }
    }

    [HttpPost("{evidenceId:guid}/approval")]
    [ProducesResponseType(typeof(ApiResponse<EvidenceApprovalResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EvidenceApprovalResultDto>>> PostApproval(
        Guid evidenceId,
        [FromBody] EvidenceApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (evidenceId == Guid.Empty || request == null || string.IsNullOrWhiteSpace(request.Action))
        {
            var bad = ApiResponseFactory.Failure<EvidenceApprovalResultDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var badMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(badMeta.HttpStatus, bad);
        }

        try
        {
            if (!TryParseAction(request.Action, out var action, out var decision))
            {
                var bad = ApiResponseFactory.Failure<EvidenceApprovalResultDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
                var badMeta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
                return StatusCode(badMeta.HttpStatus, bad);
            }

            var evidenceContext = await _db.Evidences
                .AsNoTracking()
                .Where(e => e.Id == evidenceId && e.HospitalId == _userContext.HospitalId)
                .Select(e => new { e.HospitalId, e.FrameworkId })
                .FirstOrDefaultAsync(cancellationToken);
            if (evidenceContext == null)
            {
                var nf = ApiResponseFactory.Failure<EvidenceApprovalResultDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                var nm = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(nm.HttpStatus, nf);
            }

            var data = await _evidenceReview.ApplyReviewAsync(
                evidenceId,
                _userContext.UserId,
                decision,
                request.Remark,
                request.Recommendation,
                request.ReviewerRole,
                request.RowVersion,
                objectiveElementId: null,
                cancellationToken);

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Evidence approval conflict for EvidenceId={EvidenceId}", evidenceId);
            var err = ApiResponseFactory.Failure<EvidenceApprovalResultDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT];
            return StatusCode(em.HttpStatus, err);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Evidence approval validation failed for EvidenceId={EvidenceId}", evidenceId);
            var err = ApiResponseFactory.Failure<EvidenceApprovalResultDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(em.HttpStatus, err);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Evidence approval target not found for EvidenceId={EvidenceId}", evidenceId);
            var err = ApiResponseFactory.Failure<EvidenceApprovalResultDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(em.HttpStatus, err);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evidence approval failed for EvidenceId={EvidenceId}", evidenceId);
            var err = ApiResponseFactory.Failure<EvidenceApprovalResultDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var em = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(em.HttpStatus, err);
        }
    }

    private static bool TryParseAction(
        string raw,
        out EvidenceApprovalAction action,
        out Application.EvidenceReview.EvidenceReviewDecision decision)
    {
        var value = raw.Trim().ToLowerInvariant();
        if (value is "approve" or "approved")
        {
            action = EvidenceApprovalAction.Approve;
            decision = Application.EvidenceReview.EvidenceReviewDecision.Approve;
            return true;
        }

        if (value is "reject" or "rejected")
        {
            action = EvidenceApprovalAction.Reject;
            decision = Application.EvidenceReview.EvidenceReviewDecision.Reject;
            return true;
        }

        if (value is "requestmoreevidence" or "needs-more" or "needsmoreevidence")
        {
            action = EvidenceApprovalAction.RequestMoreEvidence;
            decision = Application.EvidenceReview.EvidenceReviewDecision.RequestMoreEvidence;
            return true;
        }

        action = default;
        decision = default;
        return false;
    }
}
