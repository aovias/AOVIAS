using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Quality Documentation &amp; Policy Management (QDMS) — Sprint 15.
/// Controlled documentation lifecycle independent from Evidence Library.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/documentation")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class DocumentationController : ControllerBase
{
    private readonly IDocumentationQueryService _queries;
    private readonly IDocumentationCommandService _commands;
    private readonly IDocumentationAuthoringService _authoring;
    private readonly IDocumentationIntelligenceService _intelligence;
    private readonly ComplianceDbContext _db;
    private readonly IUserContextService _userContext;
    private readonly ILogger<DocumentationController> _logger;

    public DocumentationController(
        IDocumentationQueryService queries,
        IDocumentationCommandService commands,
        IDocumentationAuthoringService authoring,
        IDocumentationIntelligenceService intelligence,
        ComplianceDbContext db,
        IUserContextService userContext,
        ILogger<DocumentationController> logger)
    {
        _queries = queries;
        _commands = commands;
        _authoring = authoring;
        _intelligence = intelligence;
        _db = db;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DocumentationDashboardDto>>> GetDashboard(CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<DocumentationDashboardDto>(traceId);

        var dto = await _queries.GetDashboardAsync(orgId, _userContext.HospitalId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("documents")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ControlledDocumentListItemDto>>>> GetDocuments(
        [FromQuery] ControlledDocumentType? type,
        [FromQuery] ControlledDocumentStatus? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<IReadOnlyList<ControlledDocumentListItemDto>>(traceId);

        var list = await _queries.GetDocumentsAsync(orgId, type, status, search, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, list, traceId));
    }

    [HttpGet("documents/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ControlledDocumentDetailDto>>> GetDocument(Guid id, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<ControlledDocumentDetailDto>(traceId);

        var dto = await _queries.GetDocumentByIdAsync(orgId, id, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.MANAGER + "," + ApplicationRoles.ORG_ADMIN + "," + ApplicationRoles.SUPER_ADMIN)]
    public async Task<ActionResult<ApiResponse<ControlledDocumentDetailDto>>> Create(
        [FromBody] CreateControlledDocumentRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<ControlledDocumentDetailDto>(traceId);

        try
        {
            var dto = await _commands.CreateDocumentAsync(
                orgId, _userContext.HospitalId, _userContext.UserId, _userContext.Email, request, ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create controlled document");
            return BadRequest(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = ApplicationRoles.MANAGER + "," + ApplicationRoles.ORG_ADMIN + "," + ApplicationRoles.SUPER_ADMIN)]
    public async Task<ActionResult<ApiResponse<ControlledDocumentDetailDto>>> Update(
        Guid id, [FromBody] UpdateControlledDocumentRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<ControlledDocumentDetailDto>(traceId);

        try
        {
            var dto = await _commands.UpdateDocumentAsync(
                orgId, id, _userContext.UserId, _userContext.Email, request, ct);
            if (dto == null)
                return NotFound(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));

            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Document update blocked");
            return BadRequest(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpPost("{id:guid}/versions")]
    [Authorize(Roles = ApplicationRoles.MANAGER + "," + ApplicationRoles.ORG_ADMIN + "," + ApplicationRoles.SUPER_ADMIN)]
    public async Task<ActionResult<ApiResponse<ControlledDocumentDetailDto>>> CreateVersion(
        Guid id, [FromBody] CreateDocumentVersionRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<ControlledDocumentDetailDto>(traceId);

        try
        {
            var dto = await _commands.CreateVersionAsync(
                orgId, id, _userContext.UserId, _userContext.Email, request, ct);
            if (dto == null)
                return NotFound(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));

            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Create version blocked");
            return BadRequest(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DocumentVersionSummaryDto>>>> GetVersions(
        Guid id, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<IReadOnlyList<DocumentVersionSummaryDto>>(traceId);

        var list = await _queries.GetVersionHistoryAsync(orgId, id, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, list, traceId));
    }

    [HttpGet("{id:guid}/compare")]
    public async Task<ActionResult<ApiResponse<VersionCompareDto>>> CompareVersions(
        Guid id, [FromQuery] Guid? versionId, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<VersionCompareDto>(traceId);

        var dto = await _queries.CompareVersionsAsync(orgId, id, versionId, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<VersionCompareDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("{id:guid}/transition")]
    [Authorize(Roles = ApplicationRoles.MANAGER + "," + ApplicationRoles.ORG_ADMIN + "," + ApplicationRoles.SUPER_ADMIN)]
    public async Task<ActionResult<ApiResponse<ControlledDocumentDetailDto>>> Transition(
        Guid id, [FromBody] TransitionDocumentStatusRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<ControlledDocumentDetailDto>(traceId);

        try
        {
            var dto = await _commands.TransitionStatusAsync(
                orgId, id, _userContext.UserId, _userContext.Email, request, ct);
            if (dto == null)
                return NotFound(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid lifecycle transition");
            return BadRequest(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpPost("approve")]
    [Authorize(Roles = ApplicationRoles.MANAGER + "," + ApplicationRoles.ORG_ADMIN + "," + ApplicationRoles.SUPER_ADMIN)]
    public async Task<ActionResult<ApiResponse<ControlledDocumentDetailDto>>> Approve(
        [FromBody] ApproveDocumentRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<ControlledDocumentDetailDto>(traceId);

        try
        {
            var docId = await ResolveDocumentIdForVersionAsync(orgId, request.DocumentVersionId, ct);
            if (docId == null)
                return NotFound(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));

            var dto = await _commands.ApproveAsync(
                orgId, docId.Value, _userContext.UserId, _userContext.Email, request, ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto!, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Approval failed");
            return BadRequest(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpPost("publish")]
    [Authorize(Roles = ApplicationRoles.ORG_ADMIN + "," + ApplicationRoles.SUPER_ADMIN)]
    public async Task<ActionResult<ApiResponse<ControlledDocumentDetailDto>>> Publish(
        [FromQuery] Guid documentId, [FromBody] PublishDocumentRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<ControlledDocumentDetailDto>(traceId);

        try
        {
            var dto = await _commands.PublishAsync(
                orgId, documentId, _userContext.UserId, _userContext.Email, request, ct);
            if (dto == null)
                return NotFound(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Publish failed");
            return BadRequest(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpPost("acknowledge")]
    public async Task<ActionResult<ApiResponse<DocumentAcknowledgementListItemDto>>> Acknowledge(
        [FromBody] AcknowledgeDocumentRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<DocumentAcknowledgementListItemDto>(traceId);

        var dto = await _commands.AcknowledgeAsync(orgId, _userContext.UserId, request, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<DocumentAcknowledgementListItemDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("{id:guid}/reviews/complete")]
    [Authorize(Roles = ApplicationRoles.MANAGER + "," + ApplicationRoles.ORG_ADMIN + "," + ApplicationRoles.SUPER_ADMIN)]
    public async Task<ActionResult<ApiResponse<ControlledDocumentDetailDto>>> CompleteReview(
        Guid id, [FromBody] CompleteDocumentReviewRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<ControlledDocumentDetailDto>(traceId);

        try
        {
            var dto = await _commands.CompleteReviewAsync(
                orgId, id, _userContext.UserId, _userContext.Email, request, ct);
            if (dto == null)
                return NotFound(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Complete review failed");
            return BadRequest(ApiResponseFactory.Failure<ControlledDocumentDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpGet("acknowledgements")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DocumentAcknowledgementListItemDto>>>> GetAcknowledgements(
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<IReadOnlyList<DocumentAcknowledgementListItemDto>>(traceId);

        var list = await _queries.GetAcknowledgementsAsync(orgId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, list, traceId));
    }

    [HttpGet("review-calendar")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReviewCalendarItemDto>>>> GetReviewCalendar(
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<IReadOnlyList<ReviewCalendarItemDto>>(traceId);

        var list = await _queries.GetReviewCalendarAsync(orgId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, list, traceId));
    }

    [HttpGet("templates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DocumentTemplateDto>>>> GetTemplates(CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<IReadOnlyList<DocumentTemplateDto>>(traceId);

        var list = await _queries.GetTemplatesAsync(orgId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, list, traceId));
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<ApiResponse<DocumentationAnalyticsDto>>> GetAnalytics(CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<DocumentationAnalyticsDto>(traceId);

        var dto = await _queries.GetAnalyticsAsync(orgId, _userContext.HospitalId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("authoring")]
    [Authorize(Roles = ApplicationRoles.MANAGER + "," + ApplicationRoles.ORG_ADMIN + "," + ApplicationRoles.SUPER_ADMIN)]
    public async Task<ActionResult<ApiResponse<AuthoringSessionDto>>> CreateAuthoring(
        [FromBody] CreateAuthoringRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<AuthoringSessionDto>(traceId);

        var dto = await _authoring.CreateAuthoringAsync(
            orgId, _userContext.HospitalId, _userContext.UserId, _userContext.Email, request, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("authoring/{id:guid}")]
    public async Task<ActionResult<ApiResponse<AuthoringSessionDto>>> GetAuthoring(Guid id, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<AuthoringSessionDto>(traceId);

        var dto = await _authoring.GetAuthoringAsync(orgId, id, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<AuthoringSessionDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPatch("authoring/{id:guid}")]
    [Authorize(Roles = ApplicationRoles.MANAGER + "," + ApplicationRoles.ORG_ADMIN + "," + ApplicationRoles.SUPER_ADMIN)]
    public async Task<ActionResult<ApiResponse<AuthoringSessionDto>>> PatchAuthoring(
        Guid id, [FromBody] PatchAuthoringRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<AuthoringSessionDto>(traceId);

        var dto = await _authoring.PatchAuthoringAsync(orgId, id, _userContext.UserId, _userContext.Email, request, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<AuthoringSessionDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("compare")]
    public async Task<ActionResult<ApiResponse<StructuredCompareResultDto>>> CompareStructured(
        [FromBody] StructuredCompareRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<StructuredCompareResultDto>(traceId);

        var dto = await _authoring.CompareStructuredAsync(orgId, request, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<StructuredCompareResultDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("draft-ai")]
    public async Task<ActionResult<ApiResponse<AuthoringAiDraftResponse>>> DraftAi(
        [FromBody] AuthoringAiDraftRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<AuthoringAiDraftResponse>(traceId);

        var dto = await _authoring.DraftAiAsync(orgId, _userContext.UserId, request, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("authoring/comments")]
    public async Task<ActionResult<ApiResponse<AuthoringCommentDto>>> AddComment(
        [FromBody] CreateAuthoringCommentRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<AuthoringCommentDto>(traceId);

        var dto = await _authoring.AddCommentAsync(orgId, _userContext.UserId, _userContext.Email, request, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<AuthoringCommentDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("authoring/cross-references")]
    public async Task<ActionResult<ApiResponse<CrossReferenceDto>>> AddCrossReference(
        [FromBody] AddCrossReferenceRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<CrossReferenceDto>(traceId);

        var dto = await _authoring.AddCrossReferenceAsync(orgId, request, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<CrossReferenceDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("export")]
    public async Task<ActionResult<ApiResponse<DocumentExportResultDto>>> Export(
        [FromBody] DocumentExportRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<DocumentExportResultDto>(traceId);

        var dto = await _authoring.ExportAsync(orgId, request, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<DocumentExportResultDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("graph")]
    public async Task<ActionResult<ApiResponse<ComplianceGraphDto>>> GetGraph(CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<ComplianceGraphDto>(traceId);

        var dto = await _intelligence.GetGraphAsync(orgId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("health-dashboard")]
    public async Task<ActionResult<ApiResponse<DocumentHealthDashboardDto>>> GetHealthDashboard(CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<DocumentHealthDashboardDto>(traceId);

        var dto = await _intelligence.GetHealthDashboardAsync(orgId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("intelligence/{id:guid}")]
    public async Task<ActionResult<ApiResponse<DocumentIntelligenceDto>>> GetIntelligence(Guid id, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<DocumentIntelligenceDto>(traceId);

        var dto = await _intelligence.GetIntelligenceAsync(orgId, id, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<DocumentIntelligenceDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("impact/{id:guid}")]
    public async Task<ActionResult<ApiResponse<DocumentImpactDto>>> GetImpact(Guid id, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<DocumentImpactDto>(traceId);

        var dto = await _intelligence.GetImpactAsync(orgId, id, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<DocumentImpactDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("dependencies/{id:guid}")]
    public async Task<ActionResult<ApiResponse<DocumentDependenciesDto>>> GetDependencies(Guid id, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<DocumentDependenciesDto>(traceId);

        var dto = await _intelligence.GetDependenciesAsync(orgId, id, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<DocumentDependenciesDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("relationships/{id:guid}")]
    public async Task<ActionResult<ApiResponse<DocumentRelationshipsDto>>> GetRelationships(Guid id, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<DocumentRelationshipsDto>(traceId);

        var dto = await _intelligence.GetRelationshipsAsync(orgId, id, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<DocumentRelationshipsDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("review-ai")]
    public async Task<ActionResult<ApiResponse<AiComplianceReviewDto>>> ReviewAi(
        [FromBody] AiComplianceReviewRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<AiComplianceReviewDto>(traceId);

        var dto = await _intelligence.ReviewAiAsync(orgId, request, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<AiComplianceReviewDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("explorer")]
    public async Task<ActionResult<ApiResponse<DocumentExplorerDto>>> GetExplorer(CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<DocumentExplorerDto>(traceId);

        var dto = await _intelligence.GetExplorerAsync(orgId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DocumentSearchResultDto>>>> Search(
        [FromQuery] string q, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<IReadOnlyList<DocumentSearchResultDto>>(traceId);

        var dto = await _intelligence.SearchAsync(orgId, q ?? string.Empty, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("cross-reference-suggestions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CrossReferenceSuggestionDto>>>> CrossReferenceSuggestions(
        [FromQuery] Guid documentId, [FromQuery] string q, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<IReadOnlyList<CrossReferenceSuggestionDto>>(traceId);

        var dto = await _intelligence.GetCrossReferenceSuggestionsAsync(orgId, documentId, q ?? string.Empty, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    private async Task<Guid?> ResolveDocumentIdForVersionAsync(Guid orgId, Guid versionId, CancellationToken ct)
    {
        return await _db.DocumentVersions.AsNoTracking()
            .Where(v => v.Id == versionId && v.ControlledDocument.TenantId == orgId)
            .Select(v => (Guid?)v.ControlledDocumentId)
            .FirstOrDefaultAsync(ct);
    }

    private ActionResult<ApiResponse<T>> OrgMissing<T>(string traceId) =>
        BadRequest(ApiResponseFactory.Failure<T>(ApiResponseCode.VALIDATION_FAILED, traceId));
}
