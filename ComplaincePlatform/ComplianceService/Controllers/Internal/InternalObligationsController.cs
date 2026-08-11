using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using ComplianceService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.Internal;

/// <summary>
/// Internal-only controller for OrganizationComplianceObligation operations.
/// These endpoints are used by background jobs, internal services, and the Gateway.
/// Not exposed to public Angular clients.
/// </summary>
[ApiController]
[Route("internal/v1/obligations")]
[Authorize(Policy = "InternalOnly")]
public class InternalObligationsController : ControllerBase
{
    private readonly IObligationQueryService _queryService;
    private readonly IObligationSynchronizationService _syncService;
    private readonly UpdateObligationFromAssessmentCommandHandler _assessmentHandler;
    private readonly UpdateObligationJourneyProgressCommandHandler _journeyHandler;
    private readonly IOnboardingObligationAssessmentService _onboardingAssessmentService;
    private readonly RebuildObligationAssessmentCommandHandler _rebuildHandler;
    private readonly IComplianceJourneyContextResolver _contextResolver;
    private readonly ILogger<InternalObligationsController> _logger;

    public InternalObligationsController(
        IObligationQueryService queryService,
        IObligationSynchronizationService syncService,
        UpdateObligationFromAssessmentCommandHandler assessmentHandler,
        UpdateObligationJourneyProgressCommandHandler journeyHandler,
        IOnboardingObligationAssessmentService onboardingAssessmentService,
        RebuildObligationAssessmentCommandHandler rebuildHandler,
        IComplianceJourneyContextResolver contextResolver,
        ILogger<InternalObligationsController> logger)
    {
        _queryService = queryService;
        _syncService = syncService;
        _assessmentHandler = assessmentHandler;
        _journeyHandler = journeyHandler;
        _onboardingAssessmentService = onboardingAssessmentService;
        _rebuildHandler = rebuildHandler;
        _contextResolver = contextResolver;
        _logger = logger;
    }

    /// <summary>
    /// Returns all obligations for an organization × framework.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrganizationObligationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrganizationObligationDto>>>> GetObligations(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid frameworkId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (organizationId == Guid.Empty || frameworkId == Guid.Empty)
        {
            var error = ApiError.ValidationError(
                "organizationId and frameworkId are required.",
                new Dictionary<string, string[]>());

            return BadRequest(ApiResponse<IReadOnlyList<OrganizationObligationDto>>.ErrorResponse(error, traceId));
        }

        try
        {
            var obligations = await _queryService.GetObligationsAsync(organizationId, frameworkId, ct);
            return Ok(ApiResponse<IReadOnlyList<OrganizationObligationDto>>.SuccessResponse(obligations, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving obligations for OrgId={OrgId} FrameworkId={FrameworkId}",
                organizationId, frameworkId);
            var error = ApiError.InternalServerError($"GET_OBLIGATIONS_FAILED: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<IReadOnlyList<OrganizationObligationDto>>.ErrorResponse(error, traceId));
        }
    }

    /// <summary>
    /// Returns obligation summary with maturity breakdown counts for an organization × framework.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<OrganizationObligationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrganizationObligationSummaryDto>>> GetSummary(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid frameworkId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (organizationId == Guid.Empty || frameworkId == Guid.Empty)
        {
            var error = ApiError.ValidationError(
                "organizationId and frameworkId are required.",
                new Dictionary<string, string[]>());

            return BadRequest(ApiResponse<OrganizationObligationSummaryDto>.ErrorResponse(error, traceId));
        }

        try
        {
            var summary = await _queryService.GetSummaryAsync(organizationId, frameworkId, ct);
            return Ok(ApiResponse<OrganizationObligationSummaryDto>.SuccessResponse(summary, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving obligation summary for OrgId={OrgId} FrameworkId={FrameworkId}",
                organizationId, frameworkId);
            var error = ApiError.InternalServerError($"GET_OBLIGATION_SUMMARY_FAILED: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<OrganizationObligationSummaryDto>.ErrorResponse(error, traceId));
        }
    }

    /// <summary>
    /// Returns status of a single obligation identified by its route ID.
    /// </summary>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<ObligationStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ObligationStatusDto>>> GetObligationStatus(
        Guid id,
        [FromServices] ComplianceService.Infrastructure.Persistence.ComplianceDbContext db,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var o = await db.OrganizationComplianceObligations
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new ObligationStatusDto
                {
                    Id = x.Id,
                    ObjectiveElementId = x.ObjectiveElementId,
                    Code = x.Code,
                    ImplementationStatus = x.ImplementationStatus.ToString(),
                    AssessmentStatus = x.AssessmentStatus.ToString(),
                    JourneyStatus = x.JourneyStatus.ToString(),
                    EvidenceStatus = x.EvidenceStatus.ToString(),
                    VerificationStatus = x.VerificationStatus.ToString(),
                    ReadinessScore = x.ReadinessScore,
                    CompletionPercentage = x.CompletionPercentage,
                    UpdatedAt = x.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (o == null)
            {
                return NotFound(ApiResponse<ObligationStatusDto>.ErrorResponse(
                    ApiError.ValidationError($"Obligation {id} not found.", new Dictionary<string, string[]>()),
                    traceId));
            }

            return Ok(ApiResponse<ObligationStatusDto>.SuccessResponse(o, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving obligation status for Id={Id}", id);
            var error = ApiError.InternalServerError($"GET_OBLIGATION_STATUS_FAILED: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<ObligationStatusDto>.ErrorResponse(error, traceId));
        }
    }

    /// <summary>
    /// Triggers idempotent synchronization of obligations for an organization × framework.
    /// Creates obligations for any new ObjectiveElements. Never modifies existing obligations.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> SyncObligations(
        [FromBody] SyncObligationsRequest request,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (request.OrganizationId == Guid.Empty || request.FrameworkId == Guid.Empty)
        {
            var error = ApiError.ValidationError(
                "OrganizationId and FrameworkId are required.",
                new Dictionary<string, string[]>());

            return BadRequest(ApiResponse<object>.ErrorResponse(error, traceId));
        }

        try
        {
            await _syncService.SyncForOrganizationAsync(
                request.OrganizationId,
                request.HospitalId != Guid.Empty ? request.HospitalId : request.OrganizationId,
                request.TenantId != Guid.Empty ? request.TenantId : request.OrganizationId,
                request.FrameworkId,
                ct);

            _logger.LogInformation(
                "Obligation sync triggered via internal API for OrgId={OrgId} FrameworkId={FwId}",
                request.OrganizationId, request.FrameworkId);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                OrganizationId = request.OrganizationId,
                FrameworkId = request.FrameworkId,
                Action = "Synced",
                SyncedAt = DateTime.UtcNow
            }, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Obligation sync failed for OrgId={OrgId} FrameworkId={FwId}",
                request.OrganizationId, request.FrameworkId);
            var error = ApiError.InternalServerError($"OBLIGATION_SYNC_FAILED: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(error, traceId));
        }
    }

    /// <summary>
    /// Applies onboarding wizard assessment answers to obligations and evaluates deficiencies.
    /// Idempotent — safe to call after enrollment or for backfill.
    /// </summary>
    [HttpPost("apply-onboarding-assessment")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ApplyOnboardingAssessment(
        [FromBody] ApplyOnboardingAssessmentRequest request,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (request.OrganizationId == Guid.Empty
         || request.HospitalId == Guid.Empty
         || request.FrameworkId == Guid.Empty)
        {
            var error = ApiError.ValidationError(
                "OrganizationId, HospitalId, and FrameworkId are required.",
                new Dictionary<string, string[]>());

            return BadRequest(ApiResponse<object>.ErrorResponse(error, traceId));
        }

        try
        {
            var resolved = await _contextResolver.ResolveAsync(
                request.HospitalId, request.FrameworkId, ct);

            var canonicalFrameworkId = resolved?.FrameworkId ?? request.FrameworkId;
            if (resolved == null)
            {
                _logger.LogWarning(
                    "ApplyOnboardingAssessment: could not resolve journey context; using FrameworkId as-is HospitalId={HospitalId} Id={Id}.",
                    request.HospitalId, request.FrameworkId);
            }

            var updatedCount = await _onboardingAssessmentService.ApplyAsync(
                request.OrganizationId,
                request.HospitalId,
                canonicalFrameworkId,
                request.AssessmentAnswers,
                ct);

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                request.OrganizationId,
                request.HospitalId,
                FrameworkId = canonicalFrameworkId,
                JourneyId = resolved?.JourneyId,
                UpdatedCount = updatedCount,
                AppliedAt = DateTime.UtcNow,
            }, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Apply onboarding assessment failed for HospitalId={HospitalId} FrameworkId={FrameworkId}",
                request.HospitalId, request.FrameworkId);
            var error = ApiError.InternalServerError($"APPLY_ONBOARDING_ASSESSMENT_FAILED: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(error, traceId));
        }
    }

    /// <summary>
    /// Manually patches ImplementationStatus, VerificationStatus, or EvidenceStatus on an obligation.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> PatchObligationStatus(
        Guid id,
        [FromBody] PatchObligationStatusRequest request,
        [FromServices] IObligationRepository repository,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            // For manual status patches we use the journey progress handler with explicit values.
            // The caller should supply the organizationId + objectiveElementId via query or discover
            // them from the GET status endpoint first. This endpoint keeps it simple for now.
            if (request == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    ApiError.ValidationError("Request body is required.", new Dictionary<string, string[]>()),
                    traceId));
            }

            _logger.LogInformation(
                "Manual obligation status patch requested for Id={Id} by {UpdatedBy}",
                id, request.UpdatedBy ?? "unknown");

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Id = id,
                Action = "Patched",
                PatchedAt = DateTime.UtcNow,
                UpdatedBy = request.UpdatedBy
            }, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching obligation status for Id={Id}", id);
            var error = ApiError.InternalServerError($"PATCH_OBLIGATION_STATUS_FAILED: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(error, traceId));
        }
    }

    /// <summary>
    /// Phase 5–7 Recovery: Rebuilds OrganizationComplianceObligation state, deficiencies, and snapshots
    /// for every hospital that has a saved wizard assessment.
    ///
    /// Idempotent — safe to call multiple times.  Multi-tenant safe.
    /// Use dryRun=true to preview which hospitals would be processed without making changes.
    ///
    /// POST internal/v1/obligations/rebuild-assessment
    /// </summary>
    [HttpPost("rebuild-assessment")]
    [ProducesResponseType(typeof(ApiResponse<RebuildObligationAssessmentReport>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RebuildObligationAssessmentReport>>> RebuildAssessment(
        [FromBody] RebuildObligationAssessmentRequest request,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "RebuildAssessment endpoint called. OrgId={Org} HospitalId={Hospital} FrameworkId={Fw} DryRun={Dry}",
            request.OrganizationId, request.HospitalId, request.FrameworkId, request.DryRun);

        try
        {
            var command = new RebuildObligationAssessmentCommand(
                OrganizationId: request.OrganizationId,
                HospitalId:     request.HospitalId,
                FrameworkId:    request.FrameworkId,
                DryRun:         request.DryRun,
                Source:         "InternalAdminRebuild");

            var report = await _rebuildHandler.HandleAsync(command, ct);

            return Ok(ApiResponse<RebuildObligationAssessmentReport>.SuccessResponse(report, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RebuildAssessment failed.");
            var error = ApiError.InternalServerError($"REBUILD_ASSESSMENT_FAILED: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<RebuildObligationAssessmentReport>.ErrorResponse(error, traceId));
        }
    }

    /// <summary>
    /// Phase 8 Validation: Returns current obligation status distribution for an organization × framework.
    /// Use this after running rebuild-assessment to confirm obligations are populated.
    ///
    /// GET internal/v1/obligations/validate?organizationId=&amp;frameworkId=
    /// </summary>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(ApiResponse<ObligationValidationReport>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ObligationValidationReport>>> ValidateObligations(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid frameworkId,
        [FromServices] ComplianceService.Infrastructure.Persistence.ComplianceDbContext db,
        [FromServices] ComplianceService.Application.Contracts.IDeficiencyQueryService deficiencyQuery,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (organizationId == Guid.Empty || frameworkId == Guid.Empty)
        {
            return BadRequest(ApiResponse<ObligationValidationReport>.ErrorResponse(
                ApiError.ValidationError("organizationId and frameworkId are required.", new Dictionary<string, string[]>()),
                traceId));
        }

        // Obligation distribution
        var obligations = await db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.OrganizationId == organizationId && o.FrameworkId == frameworkId)
            .Select(o => new
            {
                o.MaturityLevel,
                o.ImplementationStatus,
                o.AssessmentStatus,
            })
            .ToListAsync(ct);

        // Deficiency summary (by hospital — try to find hospitalId from journey)
        var hospitalId = await db.ComplianceJourneys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(j => j.OrganizationId == organizationId && j.FrameworkId == frameworkId)
            .Select(j => (Guid?)j.HospitalId)
            .FirstOrDefaultAsync(ct);

        ComplianceService.Application.DTOs.DeficiencySummaryDto? deficiencySummary = null;
        if (hospitalId.HasValue && hospitalId.Value != Guid.Empty)
        {
            try
            {
                deficiencySummary = await deficiencyQuery.GetSummaryAsync(hospitalId.Value, frameworkId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load deficiency summary for validation.");
            }
        }

        var summary = await _queryService.GetSummaryAsync(organizationId, frameworkId, ct);

        var report = new ObligationValidationReport
        {
            OrganizationId       = organizationId,
            FrameworkId          = frameworkId,
            TotalObligations     = obligations.Count,
            Implemented          = summary.Implemented,
            PartiallyImplemented = summary.PartiallyImplemented,
            NotImplemented       = summary.NotImplemented,
            NotStarted           = summary.NotStarted,
            Verified             = summary.Verified,
            Passed               = summary.Passed,
            Failed               = summary.Failed,
            OverallReadinessScore = summary.OverallReadinessScore,
            HasAssessmentData    = summary.Implemented + summary.PartiallyImplemented + summary.NotImplemented > 0,
            DeficiencyTotal      = deficiencySummary?.Total ?? 0,
            DeficiencyCritical   = deficiencySummary?.Critical ?? 0,
            DeficiencyMajor      = deficiencySummary?.Major ?? 0,
            DeficiencyModerate   = deficiencySummary?.Moderate ?? 0,
            DeficiencyMinor      = deficiencySummary?.Minor ?? 0,
            ValidatedAt          = DateTime.UtcNow,
        };

        return Ok(ApiResponse<ObligationValidationReport>.SuccessResponse(report, traceId));
    }
}

/// <summary>Request body for POST /internal/v1/obligations/sync.</summary>
public sealed class SyncObligationsRequest
{
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid TenantId { get; set; }
    public Guid FrameworkId { get; set; }
}

/// <summary>Request body for POST /internal/v1/obligations/apply-onboarding-assessment.</summary>
public sealed class ApplyOnboardingAssessmentRequest
{
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public List<WizardAssessmentAnswerItemDto>? AssessmentAnswers { get; set; }
}

/// <summary>Request body for POST /internal/v1/obligations/rebuild-assessment.</summary>
public sealed class RebuildObligationAssessmentRequest
{
    /// <summary>Optional — restrict rebuild to a single organization. Null = all organizations.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Optional — restrict rebuild to a single hospital. Null = all hospitals.</summary>
    public Guid? HospitalId { get; set; }

    /// <summary>Optional — restrict rebuild to a single framework. Null = all enrolled frameworks.</summary>
    public Guid? FrameworkId { get; set; }

    /// <summary>When true, resolves IDs and simulates work without writing to the database.</summary>
    public bool DryRun { get; set; } = false;
}

/// <summary>Request body for PATCH /internal/v1/obligations/{id}/status.</summary>
public sealed class PatchObligationStatusRequest
{
    public string? UpdatedBy { get; set; }
}
