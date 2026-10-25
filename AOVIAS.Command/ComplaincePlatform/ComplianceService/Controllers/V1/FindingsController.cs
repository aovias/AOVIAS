using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Organization-scoped compliance findings registry (independent of journey OE-based audit findings).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/findings")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class FindingsController : ControllerBase
{
    private readonly IFindingQueryService _findings;
    private readonly IFindingsWorkspaceQueryService _workspace;
    private readonly IFindingPromotionService _promotion;
    private readonly AddFindingCommentCommandHandler _addComment;
    private readonly AddFindingAttachmentCommandHandler _addAttachment;
    private readonly PatchFindingStatusCommandHandler _patchStatus;
    private readonly UpdateFindingComplianceContextCommandHandler _updateComplianceContext;
    private readonly IUserContextService _userContext;
    private readonly ILogger<FindingsController> _logger;

    public FindingsController(
        IFindingQueryService findings,
        IFindingsWorkspaceQueryService workspace,
        IFindingPromotionService promotion,
        AddFindingCommentCommandHandler addComment,
        AddFindingAttachmentCommandHandler addAttachment,
        PatchFindingStatusCommandHandler patchStatus,
        UpdateFindingComplianceContextCommandHandler updateComplianceContext,
        IUserContextService userContext,
        ILogger<FindingsController> logger)
    {
        _findings = findings;
        _workspace = workspace;
        _promotion = promotion;
        _addComment = addComment;
        _addAttachment = addAttachment;
        _patchStatus = patchStatus;
        _updateComplianceContext = updateComplianceContext;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FindingListItemDto>>>> GetFindings(
        [FromQuery] string? search,
        [FromQuery] FindingSeverity? severity,
        [FromQuery] FindingStatus? status,
        [FromQuery] FindingDepartment? department,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
        {
            var r = ApiResponseFactory.Failure<List<FindingListItemDto>>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
            var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(m.HttpStatus, r);
        }

        try
        {
            var filters = new ManagedFindingListFilters
            {
                Search = search,
                Severity = severity,
                Status = status,
                Department = department,
            };
            var list = await _findings.GetFindingsAsync(orgId, filters, cancellationToken);
            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                list.ToList(),
                traceId);
            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetFindings failed");
            var r = ApiResponseFactory.Failure<List<FindingListItemDto>>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, r);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FindingDetailDto>>> GetFinding(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
        {
            var r = ApiResponseFactory.Failure<FindingDetailDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
            var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(m.HttpStatus, r);
        }

        try
        {
            var dto = await _findings.GetByIdAsync(orgId, id, cancellationToken);
            if (dto == null)
            {
                var r = ApiResponseFactory.Failure<FindingDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(meta.HttpStatus, r);
            }

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetFinding failed for {Id}", id);
            var r = ApiResponseFactory.Failure<FindingDetailDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, r);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FindingDetailDto>>> CreateFinding(
        [FromBody] CreateManagedFindingRequest? request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
        {
            var r = ApiResponseFactory.Failure<FindingDetailDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
            var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(m.HttpStatus, r);
        }

        if (request == null)
        {
            var r = ApiResponseFactory.Failure<FindingDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, r);
        }

        try
        {
            var dto = await _findings.CreateAsync(orgId, _userContext.UserId, request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "CreateFinding validation");
            var r = ApiResponseFactory.Failure<FindingDetailDto>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError> { new() { Message = ex.Message } },
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, r);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "CreateFinding argument");
            var r = ApiResponseFactory.Failure<FindingDetailDto>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError> { new() { Message = ex.Message } },
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateFinding failed");
            var r = ApiResponseFactory.Failure<FindingDetailDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, r);
        }
    }

    [HttpGet("workspace/dashboard")]
    public async Task<ActionResult<ApiResponse<FindingsWorkspaceDashboardDto>>> GetWorkspaceDashboard(
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
        {
            var r = ApiResponseFactory.Failure<FindingsWorkspaceDashboardDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
            var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(m.HttpStatus, r);
        }

        var dto = await _workspace.GetDashboardAsync(orgId, cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }

    [HttpGet("workspace")]
    public async Task<ActionResult<ApiResponse<FindingsWorkspaceListDto>>> GetWorkspace(
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? classification = null,
        [FromQuery] string? source = null,
        [FromQuery] string? department = null,
        [FromQuery] string? promotionReadiness = null,
        [FromQuery] string? chapterCode = null,
        [FromQuery] string? standardCode = null,
        [FromQuery] Guid? objectiveElementId = null,
        [FromQuery] string? quickFilter = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
        {
            var r = ApiResponseFactory.Failure<FindingsWorkspaceListDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
            var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(m.HttpStatus, r);
        }

        var dto = await _workspace.GetWorkspaceListAsync(
            orgId, status, severity, classification, source, department,
            promotionReadiness, chapterCode, standardCode, objectiveElementId, quickFilter,
            search, sortBy, sortDesc, page, pageSize, cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }

    [HttpGet("workspace/compliance-context")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FindingComplianceContextOptionDto>>>> SearchComplianceContext(
        [FromQuery] string? search = null,
        [FromQuery] string? chapterCode = null,
        [FromQuery] string? standardCode = null,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
        {
            var r = ApiResponseFactory.Failure<IReadOnlyList<FindingComplianceContextOptionDto>>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
            var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(m.HttpStatus, r);
        }

        var items = await _workspace.SearchComplianceContextAsync(search, chapterCode, standardCode, limit, cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, items, traceId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }

    [HttpGet("workspace/{id:guid}")]
    public async Task<ActionResult<ApiResponse<FindingsWorkspaceDetailDto>>> GetWorkspaceDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
        {
            var r = ApiResponseFactory.Failure<FindingsWorkspaceDetailDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
            var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(m.HttpStatus, r);
        }

        var dto = await _workspace.GetWorkspaceDetailAsync(orgId, id, cancellationToken);
        if (dto == null)
        {
            var r = ApiResponseFactory.Failure<FindingsWorkspaceDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, r);
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> PatchStatus(
        Guid id,
        [FromBody] PatchFindingStatusRequestDto body,
        CancellationToken cancellationToken)
    {
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return Unauthorized();

        try
        {
            await _patchStatus.HandleAsync(orgId, id, _userContext.UserId, body.Status, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DEF-M3-001 — maps an existing finding to an objective element so it can satisfy the
    /// promotion prerequisites. Standard and chapter follow from the objective element.
    /// </summary>
    [HttpPatch("{id:guid}/compliance-context")]
    public async Task<IActionResult> PatchComplianceContext(
        Guid id,
        [FromBody] PatchFindingComplianceContextRequestDto body,
        CancellationToken cancellationToken)
    {
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return Unauthorized();

        try
        {
            await _updateComplianceContext.HandleAsync(
                orgId, id, _userContext.UserId, body.ObjectiveElementId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] AddFindingCommentRequestDto body,
        CancellationToken cancellationToken)
    {
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return Unauthorized();

        try
        {
            await _addComment.HandleAsync(orgId, id, _userContext.UserId, body.Body, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/eligibility")]
    public async Task<ActionResult<ApiResponse<FindingPromotionEligibilityDto>>> GetEligibility(
        Guid id,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
        {
            var r = ApiResponseFactory.Failure<FindingPromotionEligibilityDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
            var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(m.HttpStatus, r);
        }

        try
        {
            var dto = await _promotion.GetEligibilityAsync(orgId, id, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "GetEligibility not found {Id}", id);
            var r = ApiResponseFactory.Failure<FindingPromotionEligibilityDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetEligibility failed for {Id}", id);
            var r = ApiResponseFactory.Failure<FindingPromotionEligibilityDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, r);
        }
    }

    [HttpGet("{id:guid}/promotion-status")]
    public async Task<ActionResult<ApiResponse<FindingPromotionStatusDto>>> GetPromotionStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
        {
            var r = ApiResponseFactory.Failure<FindingPromotionStatusDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
            var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(m.HttpStatus, r);
        }

        try
        {
            var dto = await _promotion.GetPromotionStatusAsync(orgId, id, _userContext.HospitalId, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "GetPromotionStatus not found {Id}", id);
            var r = ApiResponseFactory.Failure<FindingPromotionStatusDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPromotionStatus failed for {Id}", id);
            var r = ApiResponseFactory.Failure<FindingPromotionStatusDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, r);
        }
    }

    [HttpPost("{id:guid}/promote")]
    public async Task<ActionResult<ApiResponse<PromoteFindingResultDto>>> PromoteToDeficiency(
        Guid id,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
        {
            var r = ApiResponseFactory.Failure<PromoteFindingResultDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
            var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(m.HttpStatus, r);
        }

        try
        {
            var dto = await _promotion.PromoteAsync(orgId, id, _userContext.HospitalId, _userContext.UserId, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "PromoteToDeficiency validation for {Id}", id);
            var r = ApiResponseFactory.Failure<PromoteFindingResultDto>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError> { new() { Message = ex.Message } },
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PromoteToDeficiency failed for {Id}", id);
            var r = ApiResponseFactory.Failure<PromoteFindingResultDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, r);
        }
    }

    [HttpPost("{id:guid}/attachments")]
    public async Task<IActionResult> AddAttachment(
        Guid id,
        [FromBody] AddFindingAttachmentRequestDto body,
        CancellationToken cancellationToken)
    {
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return Unauthorized();

        try
        {
            await _addAttachment.HandleAsync(
                orgId, id, _userContext.UserId,
                body.FileName, body.FileUrl, body.ContentType,
                body.AttachmentType, body.FileSizeBytes, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
