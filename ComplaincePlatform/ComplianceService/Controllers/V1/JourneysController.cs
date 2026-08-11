using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Http;
using ComplianceService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain.Enums;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Services;

namespace ComplianceService.Controllers.V1;

/// <summary>
    /// Controller for compliance journey lifecycle operations (create, details, transitions).
/// All client calls flow via Gateway; this service only exposes HTTP APIs.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/journeys")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class JourneysController : ControllerBase
{
    private readonly ComplianceJourneyService _journeyService;
    private readonly ComplianceAuditService _auditService;
    private readonly IComplianceJourneyRepository _journeyRepository;
    private readonly IUserContextService _userContext;
    private readonly IJourneyWorkspaceService _journeyWorkspaceService;
    private readonly IObjectiveJourneyService _objectiveJourneyService;
    private readonly IComplianceTaskGenerationService _taskGenerationService;
    private readonly ILogger<JourneysController> _logger;

    public JourneysController(
        ComplianceJourneyService journeyService,
        ComplianceAuditService auditService,
        IComplianceJourneyRepository journeyRepository,
        IUserContextService userContext,
        IJourneyWorkspaceService journeyWorkspaceService,
        IObjectiveJourneyService objectiveJourneyService,
        IComplianceTaskGenerationService taskGenerationService,
        ILogger<JourneysController> logger)
    {
        _journeyService = journeyService;
        _auditService = auditService;
        _journeyRepository = journeyRepository;
        _userContext = userContext;
        _journeyWorkspaceService = journeyWorkspaceService;
        _objectiveJourneyService = objectiveJourneyService;
        _taskGenerationService = taskGenerationService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new compliance journey for a hospital and framework.
    /// Initializes journey in Draft state using framework configuration.
    /// </summary>
    [HttpPost("/api/v{version:apiVersion}/hospitals/{hospitalId}/frameworks/{frameworkId}/journeys")]
    public async Task<ActionResult<ApiResponse<object>>> CreateJourney(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (hospitalId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.HOSPITAL_ID_REQUIRED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
            return StatusCode(meta.HttpStatus, result);
        }

        if (frameworkId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        // Tenant and organization are both represented by OrganizationId in this service
        var organizationId = _userContext.OrganizationId;
        if (organizationId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var journey = await _journeyService.CreateJourneyAsync(
                tenantId: organizationId,
                organizationId: organizationId,
                hospitalId: hospitalId,
                frameworkId: frameworkId,
                creatingUserId: _userContext.UserId,
                cancellationToken: ct);

            var data = new
            {
                JourneyId = journey.Id,
                InitialStatus = journey.Status.ToString()
            };

            var response = ApiResponseFactory.Success(
                ApiResponseCode.JOURNEY_INITIALIZED,
                (object)data,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.JOURNEY_INITIALIZED];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Error creating compliance journey for HospitalId={HospitalId}, FrameworkId={FrameworkId}",
                hospitalId, frameworkId);

            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while creating compliance journey for HospitalId={HospitalId}, FrameworkId={FrameworkId}: {Message}",
                hospitalId, frameworkId, ex.Message);

            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Gets detailed information about a compliance journey by hospital ID and framework ID.
    /// </summary>
    [HttpGet("/api/v{version:apiVersion}/hospitals/{hospitalId:guid}/frameworks/{frameworkId:guid}/journey")]
    public async Task<ActionResult<ApiResponse<JourneyDetailsDto>>> GetJourneyByHospitalAndFramework(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (hospitalId == Guid.Empty || frameworkId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            // Get journey by hospital and framework
            var journey = await _journeyRepository.GetByHospitalAndFrameworkAsync(hospitalId, frameworkId, ct);
            if (journey == null)
            {
                var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                    ApiResponseCode.RESOURCE_NOT_FOUND,
                    traceId);

                var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(meta.HttpStatus, result);
            }

            // Enforce tenant isolation
            if (journey.TenantId != tenantId)
            {
                var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                    ApiResponseCode.FORBIDDEN,
                    traceId);

                var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
                return StatusCode(meta.HttpStatus, result);
            }

            // Get journey details using the journey ID
            var details = await _journeyService.GetJourneyDetailsAsync(journey.Id, tenantId, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                details,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Journey for hospital {HospitalId} and framework {FrameworkId} not found or invalid", hospitalId, frameworkId);

            var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to journey for hospital {HospitalId} and framework {FrameworkId}", hospitalId, frameworkId);

            var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving journey for hospital {HospitalId} and framework {FrameworkId}", hospitalId, frameworkId);

            var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Gets detailed information about a compliance journey.
    /// </summary>
    [HttpGet("{journeyId:guid}")]
    public async Task<ActionResult<ApiResponse<JourneyDetailsDto>>> GetJourneyDetails(
        Guid journeyId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (journeyId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var details = await _journeyService.GetJourneyDetailsAsync(journeyId, tenantId, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                details,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Journey {JourneyId} not found or invalid", journeyId);

            var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to journey {JourneyId}", journeyId);

            var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving journey {JourneyId}", journeyId);

            var result = ApiResponseFactory.Failure<JourneyDetailsDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Gets valid next lifecycle states for a journey,
    /// computed from state machine, framework policy, and caller role.
    /// </summary>
    [HttpGet("{journeyId:guid}/valid-transitions")]
    public async Task<ActionResult<ApiResponse<List<ValidTransitionDto>>>> GetValidTransitions(
        Guid journeyId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (journeyId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<ValidTransitionDto>>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var userId = _userContext.UserId;
        if (userId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<ValidTransitionDto>>(
                ApiResponseCode.UNAUTHORIZED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.UNAUTHORIZED];
            return StatusCode(meta.HttpStatus, result);
        }

        var roles = _userContext.Roles;

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<ValidTransitionDto>>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var states = await _journeyService.GetValidNextStatesAsync(journeyId, userId, roles, tenantId, ct);

            // Map to DTOs with code and displayName
            var transitions = states.Select(s => new ValidTransitionDto
            {
                Code = s.ToString(),
                DisplayName = GetTransitionDisplayName(s)
            }).ToList();

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                transitions,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to journey {JourneyId} valid transitions", journeyId);

            var result = ApiResponseFactory.Failure<List<ValidTransitionDto>>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Journey {JourneyId} not found or invalid when retrieving valid transitions", journeyId);

            var result = ApiResponseFactory.Failure<List<ValidTransitionDto>>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving valid transitions for journey {JourneyId}", journeyId);

            var result = ApiResponseFactory.Failure<List<ValidTransitionDto>>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Optional maintenance: ensures tasks exist for applicable objective elements (idempotent).
    /// Evidence upload no longer depends on this endpoint.
    /// </summary>
    [Obsolete("Evidence and task assignment are ensured on upload; use only for legacy maintenance if needed.")]
    [HttpPost("{journeyId:guid}/compliance-tasks/sync")]
    public async Task<ActionResult<ApiResponse<object>>> SyncComplianceTasks(Guid journeyId, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        if (journeyId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var journey = await _journeyRepository.GetByIdAsync(journeyId, ct);
        if (journey == null)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }

        if (journey.TenantId != tenantId)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.FORBIDDEN, traceId));
        }

        try
        {
            await _taskGenerationService.GenerateTasksForJourneyAsync(journeyId, _userContext.UserId, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { journeyId }, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Sync compliance tasks for journey {JourneyId}", journeyId);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    /// <summary>
    /// Helper method to get human-readable display name for a state transition.
    /// </summary>
    private static string GetTransitionDisplayName(ComplianceLifecycleState state)
    {
        return state switch
        {
            ComplianceLifecycleState.Draft => "Draft",
            ComplianceLifecycleState.Enrolled => "Enroll in Framework",
            ComplianceLifecycleState.GapAssessment => "Start Gap Assessment",
            ComplianceLifecycleState.InProgress => "Continue Journey",
            ComplianceLifecycleState.Submitted => "Submit for Review",
            ComplianceLifecycleState.AuditScheduled => "Schedule Audit",
            ComplianceLifecycleState.AuditCompleted => "Complete Audit",
            ComplianceLifecycleState.Approved => "Approve Journey",
            ComplianceLifecycleState.Rejected => "Reject Journey",
            ComplianceLifecycleState.Suspended => "Suspend Journey",
            ComplianceLifecycleState.Expired => "Expired",
            ComplianceLifecycleState.RenewalInProgress => "Renewal In Progress",
            ComplianceLifecycleState.Renewed => "Renewed",
            _ => state.ToString()
        };
    }

    /// <summary>
    /// Transitions a compliance journey to a new lifecycle state.
    /// Applies core state machine rules plus framework-specific validations.
    /// </summary>
    [HttpPost("{journeyId:guid}/transition")]
    [HttpPost("{journeyId:guid}/transitions")]
    public async Task<ActionResult<ApiResponse<object>>> TransitionJourney(
        Guid journeyId,
        [FromBody] TransitionJourneyRequest request,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (journeyId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        if (!Enum.TryParse<ComplianceLifecycleState>(request.TargetStatus, ignoreCase: true, out var targetState))
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var userId = _userContext.UserId;
        if (userId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.UNAUTHORIZED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.UNAUTHORIZED];
            return StatusCode(meta.HttpStatus, result);
        }

        var roles = _userContext.Roles;

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            await _journeyService.TransitionJourneyAsync(
                journeyId,
                targetState,
                userId,
                roles,
                tenantId,
                request.Remarks,
                ct);

            _logger.LogInformation(
                "Transitioned journey {JourneyId} to {TargetState} by user {UserId} with roles {Roles}",
                journeyId,
                targetState,
                userId,
                string.Join(',', roles));

            var data = new
            {
                JourneyId = journeyId,
                TargetStatus = targetState.ToString()
            };

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)data,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            // Business-rule validation failures (return 409 Conflict)
            _logger.LogWarning(
                ex,
                "Business rule violation while transitioning journey {JourneyId} to {TargetState} by user {UserId}",
                journeyId,
                targetState,
                userId);

            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to transition journey {JourneyId}", journeyId);

            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while transitioning journey {JourneyId} to {TargetState} by user {UserId}",
                journeyId,
                targetState,
                userId);

            var result = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Chapter index for journey / execution UI (mode-aware, spec v2 <c>items</c> wrapper).
    /// </summary>
    [HttpGet("{journeyId:guid}/chapters")]
    public async Task<ActionResult<ApiResponse<JourneyChaptersIndexDto>>> GetJourneyChapters(
        Guid journeyId,
        [FromQuery] OperationalMode? operationalMode = null,
        [FromQuery] string? auditLifecycleMode = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? classificationFilter = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (journeyId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<JourneyChaptersIndexDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<JourneyChaptersIndexDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var chapters = await _journeyWorkspaceService.GetChaptersIndexAsync(
                journeyId, tenantId, operationalMode, auditLifecycleMode, search, sort, classificationFilter, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                chapters,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Journey {JourneyId} not found when retrieving chapters", journeyId);

            var result = ApiResponseFactory.Failure<JourneyChaptersIndexDto>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to journey {JourneyId} chapters", journeyId);

            var result = ApiResponseFactory.Failure<JourneyChaptersIndexDto>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (ArgumentException)
        {
            var result = ApiResponseFactory.Failure<JourneyChaptersIndexDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving chapters for journey {JourneyId}", journeyId);

            var result = ApiResponseFactory.Failure<JourneyChaptersIndexDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Gets non-conformance summary for a compliance journey.
    /// </summary>
    [HttpGet("{journeyId:guid}/non-conformances")]
    public async Task<ActionResult<ApiResponse<NonConformanceSummaryDto>>> GetNonConformances(
        Guid journeyId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (journeyId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<NonConformanceSummaryDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<NonConformanceSummaryDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var summary = await _journeyService.GetNonConformanceSummaryAsync(journeyId, tenantId, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                summary,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Journey {JourneyId} not found when retrieving non-conformances", journeyId);

            var result = ApiResponseFactory.Failure<NonConformanceSummaryDto>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to journey {JourneyId} non-conformances", journeyId);

            var result = ApiResponseFactory.Failure<NonConformanceSummaryDto>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving non-conformances for journey {JourneyId}", journeyId);

            var result = ApiResponseFactory.Failure<NonConformanceSummaryDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Gets chronological transition history for a specific compliance journey.
    /// Used for legal defensibility, regulator audits, dispute resolution, and internal governance.
    /// 
    /// Authorization: Hospital Admin, Organization Admin, Compliance Manager
    /// </summary>
    [HttpGet("{journeyId:guid}/history")]
    public async Task<ActionResult<ApiResponse<List<JourneyHistoryItemDto>>>> GetJourneyHistory(
        Guid journeyId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (journeyId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<JourneyHistoryItemDto>>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<JourneyHistoryItemDto>>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var history = await _auditService.GetJourneyHistoryAsync(journeyId, tenantId, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                history,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Journey {JourneyId} not found when retrieving history", journeyId);

            var result = ApiResponseFactory.Failure<List<JourneyHistoryItemDto>>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to journey {JourneyId} history", journeyId);

            var result = ApiResponseFactory.Failure<List<JourneyHistoryItemDto>>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving journey {JourneyId} history", journeyId);

            var result = ApiResponseFactory.Failure<List<JourneyHistoryItemDto>>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Batch lookup: returns journeys grouped by hospital ID.
    /// Used by Gateway to enrich organization-detail response without N+1 calls.
    /// </summary>
    [HttpPost("by-hospitals")]
    public async Task<ActionResult<ApiResponse<List<HospitalJourneyBatchItemDto>>>> GetJourneysByHospitals(
        [FromBody] HospitalJourneyBatchRequest request,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (request?.HospitalIds == null || request.HospitalIds.Count == 0)
        {
            var result = ApiResponseFactory.Failure<List<HospitalJourneyBatchItemDto>>(
                ApiResponseCode.VALIDATION_FAILED, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var journeys = await _journeyRepository.GetJourneysByHospitalIdsAsync(
                request.HospitalIds, ct);

            var grouped = journeys
                .GroupBy(j => j.HospitalId)
                .Select(g => new HospitalJourneyBatchItemDto
                {
                    HospitalId = g.Key,
                    Journeys = g.Select(j => new HospitalJourneyItemDto
                    {
                        JourneyId = j.Id,
                        FrameworkId = j.FrameworkId,
                        FrameworkCode = j.Framework?.PlatformId ?? string.Empty,
                        FrameworkName = j.Framework?.Name ?? string.Empty,
                        StartDate = j.EnrollmentDate,
                        ExpiryDate = j.ExpiryDate
                    }).ToList()
                }).ToList();

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS, grouped, traceId);
            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch journey lookup for {Count} hospitals",
                request.HospitalIds.Count);
            var result = ApiResponseFactory.Failure<List<HospitalJourneyBatchItemDto>>(
                ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    // --- Journey screen API v2 (split reads; workspace retained for backward compatibility) ---

    [HttpGet("{journeyId:guid}/session")]
    public async Task<ActionResult<ApiResponse<JourneySessionDto>>> GetJourneySession(
        Guid journeyId,
        [FromQuery] OperationalMode? operationalMode = null,
        [FromQuery] string? auditLifecycleMode = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<JourneySessionDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _journeyWorkspaceService.GetSessionAsync(journeyId, tenantId, operationalMode, auditLifecycleMode, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<JourneySessionDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (ArgumentException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<JourneySessionDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                ApiResponseFactory.Failure<JourneySessionDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpGet("{journeyId:guid}/dashboard")]
    public async Task<ActionResult<ApiResponse<JourneyDashboardDto>>> GetJourneyDashboard(
        Guid journeyId,
        [FromQuery] OperationalMode? operationalMode = null,
        [FromQuery] string? auditLifecycleMode = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<JourneyDashboardDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _journeyWorkspaceService.GetDashboardAsync(journeyId, tenantId, operationalMode, auditLifecycleMode, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<JourneyDashboardDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (ArgumentException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<JourneyDashboardDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                ApiResponseFactory.Failure<JourneyDashboardDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}")]
    public async Task<ActionResult<ApiResponse<ObjectiveElementDto>>> GetObjectiveElementDetail(
        Guid journeyId,
        Guid objectiveElementId,
        [FromQuery] OperationalMode? operationalMode = null,
        [FromQuery] string? auditLifecycleMode = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveElementDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _journeyWorkspaceService.GetObjectiveElementDetailAsync(
                journeyId, objectiveElementId, tenantId, operationalMode, auditLifecycleMode, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveElementDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (ArgumentException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveElementDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveElementDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/oe-execution-wizard")]
    public async Task<ActionResult<ApiResponse<ObjectiveJourneyStateDto>>> GetOeExecutionWizard(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _objectiveJourneyService.GetAsync(journeyId, objectiveElementId, tenantId, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpPut("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/oe-execution-wizard")]
    public async Task<ActionResult<ApiResponse<ObjectiveJourneyStateDto>>> PutOeExecutionWizardDraft(
        Guid journeyId,
        Guid objectiveElementId,
        [FromBody] SaveObjectiveJourneyDraftRequest body,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _objectiveJourneyService.UpsertDraftAsync(journeyId, objectiveElementId, tenantId, body, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex) when (ex.Message == "VERSION_CONFLICT")
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId));
        }
        catch (InvalidOperationException ex) when (ex.Message == "WIZARD_COMPLETED")
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId));
        }
        catch (InvalidOperationException ex) when (ex.Message == "Journey not found.")
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "OE wizard draft validation failed: ParamName={ParamName}, Message={Message}",
                ex.ParamName,
                ex.Message);
            var errors = new List<ApiError>
            {
                new()
                {
                    Field = string.IsNullOrEmpty(ex.ParamName) ? "payload" : ex.ParamName,
                    Message = ex.Message
                }
            };
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.VALIDATION_FAILED, errors, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "OE wizard draft failed: {Message}", ex.Message);
            var errors = new List<ApiError> { new() { Field = null, Message = ex.Message } };
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.VALIDATION_FAILED, errors, traceId));
        }
    }

    [HttpPost("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/oe-execution-wizard/complete")]
    public async Task<ActionResult<ApiResponse<ObjectiveJourneyStateDto>>> PostOeExecutionWizardComplete(
        Guid journeyId,
        Guid objectiveElementId,
        [FromBody] CompleteObjectiveJourneyRequest body,
        [FromQuery] OperationalMode? operationalMode = null,
        [FromQuery] string? auditLifecycleMode = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _objectiveJourneyService.CompleteAsync(
                journeyId,
                objectiveElementId,
                tenantId,
                _userContext.UserId,
                _userContext.Roles,
                body,
                operationalMode,
                auditLifecycleMode,
                ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex) when (ex.Message == "VERSION_CONFLICT")
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId));
        }
        catch (InvalidOperationException ex) when (ex.Message == "WIZARD_COMPLETED")
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains(
                                                     "Score 5 requires at least one verified evidence",
                                                     StringComparison.Ordinal))
        {
            _logger.LogWarning(ex, "OE wizard complete blocked: score 5 without verified evidence");
            var errors = new List<ApiError>
            {
                new()
                {
                    Field = "evidence",
                    Message = ex.Message
                }
            };
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.VALIDATION_FAILED, errors, traceId));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "OE wizard complete validation failed: ParamName={ParamName}, Message={Message}",
                ex.ParamName,
                ex.Message);
            var errors = new List<ApiError>
            {
                new()
                {
                    Field = string.IsNullOrEmpty(ex.ParamName) ? "payload" : ex.ParamName,
                    Message = ex.Message
                }
            };
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.VALIDATION_FAILED, errors, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "OE wizard complete failed: {Message}", ex.Message);
            var errors = new List<ApiError> { new() { Field = null, Message = ex.Message } };
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<ObjectiveJourneyStateDto>(ApiResponseCode.VALIDATION_FAILED, errors, traceId));
        }
    }

    [HttpPatch("{journeyId:guid}/session/operational-mode")]
    public async Task<ActionResult<ApiResponse<JourneySessionDto>>> PatchJourneyOperationalMode(
        Guid journeyId,
        [FromBody] PatchOperationalModeRequestDto body,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<JourneySessionDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _journeyWorkspaceService.PatchOperationalModeAsync(journeyId, tenantId, body.OperationalMode, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<JourneySessionDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("Invalid operational mode transition", StringComparison.Ordinal))
            {
                return StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus,
                    ApiResponseFactory.Failure<JourneySessionDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId));
            }

            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<JourneySessionDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
    }

    [HttpDelete("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/{evidenceId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteObjectiveElementEvidence(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
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

        try
        {
            await _journeyWorkspaceService.DeleteObjectiveElementEvidenceAsync(
                journeyId, objectiveElementId, evidenceId, tenantId, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { deleted = true }, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    // --- NABH journey workspace (OE tree, accreditation snapshot, evidence, OE history) ---

    /// <summary>NABH journey workspace snapshot (Framework → Chapter → Standard → OE).</summary>
        [HttpGet("{journeyId:guid}/workspace")]
    public async Task<ActionResult<ApiResponse<JourneyWorkspaceSnapshotDto>>> GetJourneyWorkspace(
        Guid journeyId,
        [FromQuery] OperationalMode? operationalMode = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (journeyId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<JourneyWorkspaceSnapshotDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<JourneyWorkspaceSnapshotDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _journeyWorkspaceService.GetWorkspaceAsync(journeyId, tenantId, operationalMode, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Forbidden workspace access {JourneyId}", journeyId);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<JourneyWorkspaceSnapshotDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Workspace not found {JourneyId}", journeyId);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                ApiResponseFactory.Failure<JourneyWorkspaceSnapshotDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workspace error {JourneyId}", journeyId);
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus,
                ApiResponseFactory.Failure<JourneyWorkspaceSnapshotDto>(ApiResponseCode.INTERNAL_ERROR, traceId));
        }
    }

    /// <summary>
    /// Paginated list of compliance standards for a chapter within the journey workspace.
    /// Response items are <see cref="ComplianceStandardDto"/> (includes <c>chapterId</c>).
    /// </summary>
    [HttpGet("{journeyId:guid}/chapters/{chapterId:guid}/standards")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ComplianceStandardDto>>>> GetChapterStandards(
        Guid journeyId,
        Guid chapterId,
        [FromQuery] OperationalMode? operationalMode = null,
        [FromQuery] string? auditLifecycleMode = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? classificationFilter = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<ComplianceStandardDto>>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _journeyWorkspaceService.GetChapterStandardsAsync(
                journeyId, chapterId, tenantId, operationalMode, auditLifecycleMode, page, pageSize, classificationFilter, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<ComplianceStandardDto>>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<ComplianceStandardDto>>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpPatch("{journeyId:guid}/objective-elements/{objectiveElementId:guid}")]
    public async Task<ActionResult<ApiResponse<PatchObjectiveElementResponseDto>>> PatchObjectiveElement(
        Guid journeyId,
        Guid objectiveElementId,
        [FromBody] PatchObjectiveElementRequest request,
        [FromQuery] OperationalMode? operationalMode = null,
        [FromQuery] string? auditLifecycleMode = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<PatchObjectiveElementResponseDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _journeyWorkspaceService.PatchObjectiveElementAsync(
                journeyId,
                objectiveElementId,
                tenantId,
                _userContext.UserId,
                _userContext.Roles,
                request,
                operationalMode,
                auditLifecycleMode,
                ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<PatchObjectiveElementResponseDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex) when (ex.Message == "VERSION_CONFLICT")
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus,
                ApiResponseFactory.Failure<PatchObjectiveElementResponseDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "PATCH OE validation");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<PatchObjectiveElementResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "PATCH OE validation");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<PatchObjectiveElementResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpPost("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/bulk")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(15 * 10_485_760)]
    public async Task<ActionResult<ApiResponse<BulkEvidenceUploadResponseDto>>> PostOeEvidenceBulk(
        Guid journeyId,
        Guid objectiveElementId,
        [FromForm] List<IFormFile>? files,
        [FromForm] string evidenceType = "",
        [FromForm] string? sharedMetadata = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<BulkEvidenceUploadResponseDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        if (files is null || files.Count == 0)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<BulkEvidenceUploadResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var incoming = new List<BulkUploadIncomingFile>(files.Count);
        try
        {
            foreach (var f in files)
            {
                incoming.Add(new BulkUploadIncomingFile
                {
                    Stream = f.OpenReadStream(),
                    FileName = f.FileName ?? "upload.bin",
                    ContentType = f.ContentType,
                    Length = f.Length
                });
            }

            var data = await _journeyWorkspaceService.BulkUploadObjectiveElementEvidenceAsync(
                journeyId,
                objectiveElementId,
                tenantId,
                _userContext.UserId,
                incoming,
                evidenceType,
                sharedMetadata,
                ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<BulkEvidenceUploadResponseDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "POST OE evidence bulk");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<BulkEvidenceUploadResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
        finally
        {
            foreach (var i in incoming)
            {
                await i.Stream.DisposeAsync();
            }
        }
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/{evidenceId:guid}")]
    public async Task<ActionResult<ApiResponse<EvidenceDetailDto>>> GetOeEvidenceDetail(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<EvidenceDetailDto>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _journeyWorkspaceService.GetObjectiveElementEvidenceDetailAsync(
                journeyId,
                objectiveElementId,
                evidenceId,
                tenantId,
                ct);
            if (data is null)
            {
                return StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus,
                    ApiResponseFactory.Failure<EvidenceDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
            }

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<EvidenceDetailDto>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "GET OE evidence detail");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<EvidenceDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/{evidenceId:guid}/download")]
    public async Task<IActionResult> GetOeEvidenceDownload(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        CancellationToken ct = default)
    {
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, HttpContext.TraceIdentifier));
        }

        try
        {
            var (stream, contentType, fileName) = await _journeyWorkspaceService.DownloadObjectiveElementEvidenceAsync(
                journeyId,
                objectiveElementId,
                evidenceId,
                tenantId,
                ct);
            return File(stream, contentType, fileName);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "GET OE evidence download");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));
        }
    }

    [Obsolete("Use POST /api/v1.0/evidence/{evidenceId}/approval via EvidenceReviewService. Retained for backward compatibility.")]
    [HttpPost("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/{evidenceId:guid}/verify")]
    public async Task<ActionResult<ApiResponse<object>>> PostOeEvidenceVerify(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        [FromBody] VerifyOeEvidenceRequest request,
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

        try
        {
            await _journeyWorkspaceService.VerifyObjectiveElementEvidenceAsync(
                journeyId,
                objectiveElementId,
                evidenceId,
                tenantId,
                _userContext.UserId,
                request.Accepted,
                request.Remarks,
                ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { objectiveElementId, evidenceId }, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "POST OE evidence verify");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpPost("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence/{evidenceId:guid}/versions")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ApiResponse<object>>> PostOeEvidenceVersion(
        Guid journeyId,
        Guid objectiveElementId,
        Guid evidenceId,
        [FromForm(Name = "file")] IFormFile file,
        [FromForm] string? notes = null,
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

        if (file is null || file.Length == 0)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await _journeyWorkspaceService.UploadObjectiveElementEvidenceVersionAsync(
                journeyId,
                objectiveElementId,
                evidenceId,
                tenantId,
                _userContext.UserId,
                stream,
                file.FileName,
                file.ContentType,
                notes,
                ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { evidenceId }, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "POST OE evidence version");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OeEvidenceItemDto>>>> GetOeEvidence(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<OeEvidenceItemDto>>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _journeyWorkspaceService.GetObjectiveElementEvidenceAsync(journeyId, objectiveElementId, tenantId, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<OeEvidenceItemDto>>(ApiResponseCode.FORBIDDEN, traceId));
        }
    }

    /// <summary>
    /// Registers evidence by URL (JSON) or file upload (multipart). One action avoids duplicate-route
    /// <see cref="ConsumesAttribute"/> selection that returned 400 <c>problem+json</c> when Content-Type did not match the body.
    /// </summary>
    [HttpPost("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/evidence")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ApiResponse<object>>> PostOeEvidence(
        Guid journeyId,
        Guid objectiveElementId,
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

        if (Request.HasFormContentType)
        {
            IFormCollection form;
            try
            {
                form = await Request.ReadFormAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "POST OE evidence (multipart form read)");
                return StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                    ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
            }

            var correlationId = CorrelationIdAccessor.FromHttpContext(HttpContext);
            var fileSummaries = string.Join(
                " | ",
                form.Files.Select(f => $"{f.Name}:{f.FileName}:len={f.Length}:ct={f.ContentType}"));
            _logger.LogInformation(
                "OE evidence multipart inbound (after ReadFormAsync). CorrelationId={CorrelationId} TraceId={TraceId} JourneyId={JourneyId} ObjectiveElementId={ObjectiveElementId} " +
                "RequestContentLength={RequestContentLength} FileCount={FileCount} FileParts={FileParts}",
                correlationId,
                traceId,
                journeyId,
                objectiveElementId,
                Request.ContentLength,
                form.Files.Count,
                string.IsNullOrEmpty(fileSummaries) ? "(none)" : fileSummaries);

            var file = ResolveOeEvidenceUploadedFile(form);
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

            _logger.LogInformation(
                "OE evidence multipart resolved. CorrelationId={CorrelationId} TraceId={TraceId} JourneyId={JourneyId} ObjectiveElementId={ObjectiveElementId} " +
                "EvidenceType={EvidenceType} ResolvedField={ResolvedField} ResolvedFileName={ResolvedFileName} ResolvedLength={ResolvedLength} ResolvedContentType={ResolvedContentType}",
                correlationId,
                traceId,
                journeyId,
                objectiveElementId,
                evidenceType,
                file?.Name,
                file?.FileName,
                file?.Length,
                file?.ContentType);

            if (file is null || file.Length == 0)
            {
                return ApiResponseFactory.FailureResult<object>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError>
                    {
                        new() { Field = "file", Message = "A non-empty file is required." }
                    },
                    traceId);
            }

            if (string.IsNullOrWhiteSpace(evidenceType))
            {
                return ApiResponseFactory.FailureResult<object>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError>
                    {
                        new() { Field = "evidenceType", Message = "Evidence type is required and cannot be empty." }
                    },
                    traceId);
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var id = await _journeyWorkspaceService.RegisterObjectiveElementEvidenceFromUploadAsync(
                    journeyId,
                    objectiveElementId,
                    tenantId,
                    _userContext.UserId,
                    stream,
                    file.FileName,
                    file.ContentType,
                    evidenceType.Trim(),
                    displayName: string.IsNullOrWhiteSpace(name) ? null : name,
                    title: string.IsNullOrWhiteSpace(title) ? null : title,
                    metadata: string.IsNullOrWhiteSpace(metadata) ? null : metadata,
                    ct);
                var metaOk = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
                return StatusCode(metaOk.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { evidenceId = id }, traceId));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                    ApiResponseFactory.Failure<object>(ApiResponseCode.FORBIDDEN, traceId));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "POST OE evidence (multipart)");
                return StatusCode(
                    ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                    ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
            }
        }

        try
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(json))
            {
                return ApiResponseFactory.FailureResult<object>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError>
                    {
                        new() { Field = "body", Message = "Request body is required." }
                    },
                    traceId);
            }

            var request = JsonSerializer.Deserialize<RegisterOeEvidenceRequest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (request is null)
            {
                return ApiResponseFactory.FailureResult<object>(
                    ApiResponseCode.VALIDATION_FAILED,
                    new List<ApiError>
                    {
                        new() { Field = "body", Message = "Request body could not be parsed." }
                    },
                    traceId);
            }

            var id = await _journeyWorkspaceService.RegisterObjectiveElementEvidenceAsync(
                journeyId,
                objectiveElementId,
                tenantId,
                _userContext.UserId,
                request,
                ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)new { evidenceId = id }, traceId));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "POST OE evidence (JSON)");
            return ApiResponseFactory.FailureResult<object>(
                ApiResponseCode.VALIDATION_FAILED,
                new List<ApiError>
                {
                    new() { Field = "body", Message = "Invalid JSON body." }
                },
                traceId);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.FORBIDDEN, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "POST OE evidence");
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    private static IFormFile? ResolveOeEvidenceUploadedFile(IFormCollection form)
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

    [HttpGet("{journeyId:guid}/objective-elements/{objectiveElementId:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OeHistoryEventDto>>>> GetOeHistory(
        Guid journeyId,
        Guid objectiveElementId,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING].HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<OeHistoryEventDto>>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId));
        }

        try
        {
            var data = await _journeyWorkspaceService.GetObjectiveElementHistoryAsync(journeyId, objectiveElementId, tenantId, ct);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(
                ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN].HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<OeHistoryEventDto>>(ApiResponseCode.FORBIDDEN, traceId));
        }
    }
}

/// <summary>
/// Controller for enterprise audit and compliance reporting endpoints.
/// Provides cross-journey audit reports for corporate governance and regulator queries.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hospitals")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class AuditController : ControllerBase
{
    private readonly ComplianceAuditService _auditService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        ComplianceAuditService auditService,
        IUserContextService userContext,
        ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated transition audit report for a hospital and framework.
    /// Used by corporate governance teams, enterprise compliance dashboards, and regulator-level audit queries.
    /// 
    /// Authorization: Organization Admin, Corporate Compliance Officer, SuperAdmin
    /// </summary>
    [HttpGet("{hospitalId:guid}/frameworks/{frameworkId:guid}/transitions")]
    public async Task<ActionResult<ApiResponse<PaginatedTransitionAuditDto>>> GetTransitionAuditReport(
        Guid hospitalId,
        Guid frameworkId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? state = null,
        [FromQuery] Guid? user = null,
        [FromQuery] string? role = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;

        // Validate GUID parameters
        if (hospitalId == Guid.Empty ||
            frameworkId == Guid.Empty ||
            pageNumber < 1 ||
            pageSize < 1 ||
            pageSize > 100)
        {
            var result = ApiResponseFactory.Failure<PaginatedTransitionAuditDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<PaginatedTransitionAuditDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        // Parse state filter (can be fromState or toState - for simplicity, we'll use toState)
        ComplianceLifecycleState? fromState = null;
        ComplianceLifecycleState? toState = null;
        if (!string.IsNullOrWhiteSpace(state))
        {
            if (Enum.TryParse<ComplianceLifecycleState>(state, ignoreCase: true, out var parsedState))
            {
                toState = parsedState;
            }
            else
            {
                var result = ApiResponseFactory.Failure<PaginatedTransitionAuditDto>(
                    ApiResponseCode.VALIDATION_FAILED,
                    traceId);

                var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
                return StatusCode(meta.HttpStatus, result);
            }
        }

        try
        {
            var report = await _auditService.GetTransitionAuditReportAsync(
                hospitalId,
                frameworkId,
                tenantId,
                fromDate,
                toDate,
                fromState,
                toState,
                user,
                pageNumber,
                pageSize,
                ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                report,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in transition audit report request");

            var result = ApiResponseFactory.Failure<PaginatedTransitionAuditDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while retrieving transition audit report for HospitalId={HospitalId}, FrameworkId={FrameworkId}",
                hospitalId, frameworkId);

            var result = ApiResponseFactory.Failure<PaginatedTransitionAuditDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Gets hospital-level lifecycle summary with aggregated statistics.
    /// Returns total frameworks, approved count, expiring count, suspended count, and risk index.
    /// 
    /// Authorization: Hospital Admin, Organization Admin, Compliance Manager
    /// </summary>
    [HttpGet("{hospitalId:guid}/journey-summary")]
    public async Task<ActionResult<ApiResponse<JourneySummaryDto>>> GetJourneySummary(
        Guid hospitalId,
        [FromQuery] Guid? frameworkId = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (hospitalId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<JourneySummaryDto>(
                ApiResponseCode.HOSPITAL_ID_REQUIRED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ID_REQUIRED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<JourneySummaryDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var summary = await _auditService.GetJourneySummaryAsync(hospitalId, tenantId, frameworkId, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                summary,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while retrieving journey summary for HospitalId={HospitalId}",
                hospitalId);

            var result = ApiResponseFactory.Failure<JourneySummaryDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }
}

/// <summary>
/// Controller for organization-level compliance intelligence endpoints.
/// Provides enterprise-wide aggregated metrics and compliance overviews.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organizations")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class ComplianceIntelligenceController : ControllerBase
{
    private readonly ComplianceAuditService _auditService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ComplianceIntelligenceController> _logger;

    public ComplianceIntelligenceController(
        ComplianceAuditService auditService,
        IUserContextService userContext,
        ILogger<ComplianceIntelligenceController> logger)
    {
        _auditService = auditService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets enterprise-level compliance overview with organization-wide metrics.
    /// Returns percentage approved, at-risk count, critical count, and expiry heatmap.
    /// 
    /// Authorization: Organization Admin, Corporate Compliance Officer, SuperAdmin
    /// </summary>
    [HttpGet("{orgId:guid}/compliance-overview")]
    public async Task<ActionResult<ApiResponse<ComplianceOverviewDto>>> GetComplianceOverview(
        Guid orgId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (orgId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<ComplianceOverviewDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<ComplianceOverviewDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var overview = await _auditService.GetComplianceOverviewAsync(orgId, tenantId, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                overview,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to organization {OrgId} compliance overview", orgId);

            var result = ApiResponseFactory.Failure<ComplianceOverviewDto>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while retrieving compliance overview for OrganizationId={OrgId}",
                orgId);

            var result = ApiResponseFactory.Failure<ComplianceOverviewDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Gets organization-level regulatory overview for Gateway Framework Enrollment module.
    /// Aggregates active accreditations, upcoming renewals, and risk index.
    /// 
    /// Authorization: Organization Admin, Corporate Compliance Officer, SuperAdmin
    /// </summary>
    [HttpGet("{orgId:guid}/regulatory-overview")]
    public async Task<ActionResult<ApiResponse<RegulatoryOverviewDto>>> GetRegulatoryOverview(
        Guid orgId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (orgId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<RegulatoryOverviewDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<RegulatoryOverviewDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var overview = await _auditService.GetRegulatoryOverviewAsync(orgId, tenantId, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                overview,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to organization {OrgId} regulatory overview", orgId);

            var result = ApiResponseFactory.Failure<RegulatoryOverviewDto>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while getting regulatory overview for organization {OrgId}", orgId);

            var result = ApiResponseFactory.Failure<RegulatoryOverviewDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Gets framework catalog for an organization with basic filtering and pagination.
    /// Consumed by Gateway Framework Enrollment (Catalog) endpoint.
    /// </summary>
    [HttpGet("{orgId:guid}/frameworks/catalog")]
    public async Task<ActionResult<ApiResponse<FrameworkCatalogPageDto>>> GetFrameworkCatalog(
        Guid orgId,
        [FromQuery] string? country,
        [FromQuery] string? accreditationBody,
        [FromQuery] string? type,
        [FromQuery] string? version,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (orgId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<FrameworkCatalogPageDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<FrameworkCatalogPageDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var catalog = await _auditService.GetFrameworkCatalogAsync(
                orgId,
                tenantId,
                country,
                accreditationBody,
                type,
                version,
                page,
                pageSize,
                search,
                ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                catalog,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to organization {OrgId} framework catalog", orgId);

            var result = ApiResponseFactory.Failure<FrameworkCatalogPageDto>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while getting framework catalog for organization {OrgId}", orgId);

            var result = ApiResponseFactory.Failure<FrameworkCatalogPageDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Updates organization primary jurisdiction (country/region).
    /// Currently implemented as a stub that echoes back the requested values.
    /// </summary>
    [HttpPut("{orgId:guid}/jurisdiction")]
    public async Task<ActionResult<ApiResponse<UpdateOrganizationJurisdictionResponseDto>>> UpdateJurisdiction(
        Guid orgId,
        [FromBody] UpdateOrganizationJurisdictionRequestDto request,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (orgId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<UpdateOrganizationJurisdictionResponseDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<UpdateOrganizationJurisdictionResponseDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var result = await _auditService.UpdateOrganizationJurisdictionAsync(orgId, tenantId, request, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                result,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to update jurisdiction for organization {OrgId}", orgId);

            var resultFailure = ApiResponseFactory.Failure<UpdateOrganizationJurisdictionResponseDto>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, resultFailure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while updating jurisdiction for organization {OrgId}", orgId);

            var resultFailure = ApiResponseFactory.Failure<UpdateOrganizationJurisdictionResponseDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, resultFailure);
        }
    }
}

/// <summary>
/// Controller for organization-level accreditation listings (accreditations and renewals).
/// These endpoints are consumed by the Gateway regulatory module.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organizations")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class OrganizationAccreditationsController : ControllerBase
{
    private readonly ComplianceAuditService _auditService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<OrganizationAccreditationsController> _logger;

    public OrganizationAccreditationsController(
        ComplianceAuditService auditService,
        IUserContextService userContext,
        ILogger<OrganizationAccreditationsController> logger)
    {
        _auditService = auditService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets active accreditations for an organization.
    /// Backed by ComplianceJourney read model.
    /// </summary>
    [HttpGet("{orgId:guid}/accreditations")]
    public async Task<ActionResult<ApiResponse<List<AccreditationSummaryDto>>>> GetAccreditations(
        Guid orgId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (orgId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<AccreditationSummaryDto>>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<AccreditationSummaryDto>>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var accreditations = await _auditService.GetAccreditationsAsync(orgId, tenantId, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                accreditations,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to organization {OrgId} accreditations", orgId);

            var result = ApiResponseFactory.Failure<List<AccreditationSummaryDto>>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while getting accreditations for organization {OrgId}", orgId);

            var result = ApiResponseFactory.Failure<List<AccreditationSummaryDto>>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Gets upcoming accreditation renewals for an organization.
    /// withinDays defines the lookahead window (default 90 days).
    /// </summary>
    [HttpGet("{orgId:guid}/renewals")]
    public async Task<ActionResult<ApiResponse<List<RenewalDto>>>> GetRenewals(
        Guid orgId,
        [FromQuery] int withinDays = 90,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (orgId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<RenewalDto>>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<RenewalDto>>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var renewals = await _auditService.GetRenewalsAsync(orgId, tenantId, withinDays, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                renewals,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to organization {OrgId} renewals", orgId);

            var result = ApiResponseFactory.Failure<List<RenewalDto>>(
                ApiResponseCode.FORBIDDEN,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.FORBIDDEN];
            return StatusCode(meta.HttpStatus, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while getting renewals for organization {OrgId}", orgId);

            var result = ApiResponseFactory.Failure<List<RenewalDto>>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }
}

public record TransitionJourneyRequest
{
    /// <summary>
    /// Target lifecycle state name (e.g., Draft, Enrolled, Submitted, AuditScheduled, Approved, Suspended, Expired).
    /// Case-insensitive.
    /// </summary>
    public string TargetStatus { get; init; } = string.Empty;

    /// <summary>
    /// Optional remarks / reason for the transition (used especially for Rejected/Suspended).
    /// </summary>
    public string? Remarks { get; init; }

    /// <summary>
    /// Optional metadata bag for future extensibility.
    /// Currently not used by the service, but accepted for forward compatibility.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

