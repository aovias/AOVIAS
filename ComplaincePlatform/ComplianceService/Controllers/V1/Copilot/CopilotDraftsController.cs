using ComplianceService.Application.Contracts;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1.Copilot;

/// <summary>
/// Copilot drafts API — generation, versioning, and lifecycle management.
/// Sprint 13C: adds POST /generate and POST /{id}/regenerate and GET /{id}/versions.
/// Consumed via the Gateway — portal never calls this directly.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/copilot/drafts")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class CopilotDraftsController : ControllerBase
{
    private readonly ICopilotDraftService _service;
    private readonly IUserContextService _userContext;
    private readonly ILogger<CopilotDraftsController> _logger;

    public CopilotDraftsController(
        ICopilotDraftService service,
        IUserContextService userContext,
        ILogger<CopilotDraftsController> logger)
    {
        _service     = service;
        _userContext = userContext;
        _logger      = logger;
    }

    // ── GET /copilot/drafts ───────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiDraftDto>>>> GetDrafts(
        CancellationToken cancellationToken)
    {
        var requestId      = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        if (organizationId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<AiDraftDto>>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        var results = await _service.GetDraftsAsync(organizationId, cancellationToken: cancellationToken);
        var ok      = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(ok.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, results, requestId));
    }

    // ── GET /copilot/drafts/{id} ──────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AiDraftDto>>> GetDraft(
        Guid id,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        var result    = await _service.GetDraftAsync(id, cancellationToken);

        if (result is null)
        {
            var notFound = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(notFound.HttpStatus,
                ApiResponseFactory.Failure<AiDraftDto>(ApiResponseCode.RESOURCE_NOT_FOUND, requestId));
        }

        var ok = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(ok.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, result, requestId));
    }

    // ── GET /copilot/drafts/{id}/versions ────────────────────────────────────

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiDraftDto>>>> GetVersions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        try
        {
            var versions = await _service.GetVersionsAsync(id, cancellationToken);
            var ok = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
            return StatusCode(ok.HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, versions, requestId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("GetVersions failed: {Message}", ex.Message);
            var notFound = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(notFound.HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<AiDraftDto>>(
                    ApiResponseCode.RESOURCE_NOT_FOUND, requestId));
        }
    }

    // ── POST /copilot/drafts/generate ─────────────────────────────────────────

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<AiDraftDto>>> Generate(
        [FromBody] GenerateDraftRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId      = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        if (organizationId == Guid.Empty)
        {
            var orgMissing = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(orgMissing.HttpStatus,
                ApiResponseFactory.Failure<AiDraftDto>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        _logger.LogInformation(
            "Generate draft requested. OrgId={OrgId} DraftType={DraftType} Persona={Persona}",
            organizationId, request.DraftType, request.Persona);

        try
        {
            var draft = await _service.GenerateDraftAsync(organizationId, request, cancellationToken);
            var ok    = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
            return StatusCode(ok.HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, draft, requestId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Draft generation failed. OrgId={OrgId} DraftType={DraftType}", organizationId, request.DraftType);
            var err = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(err.HttpStatus,
                ApiResponseFactory.Failure<AiDraftDto>(ApiResponseCode.INTERNAL_ERROR, requestId));
        }
    }

    // ── POST /copilot/drafts/{id}/regenerate ─────────────────────────────────

    [HttpPost("{id:guid}/regenerate")]
    public async Task<ActionResult<ApiResponse<AiDraftDto>>> Regenerate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var requestId      = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        _logger.LogInformation(
            "Regenerate draft requested. DraftId={DraftId} OrgId={OrgId}", id, organizationId);

        try
        {
            var draft = await _service.RegenerateDraftAsync(id, organizationId, cancellationToken);
            var ok    = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
            return StatusCode(ok.HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, draft, requestId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Regenerate failed: {Message}", ex.Message);
            var notFound = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(notFound.HttpStatus,
                ApiResponseFactory.Failure<AiDraftDto>(ApiResponseCode.RESOURCE_NOT_FOUND, requestId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Regenerate failed for DraftId={DraftId}", id);
            var err = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(err.HttpStatus,
                ApiResponseFactory.Failure<AiDraftDto>(ApiResponseCode.INTERNAL_ERROR, requestId));
        }
    }
}
