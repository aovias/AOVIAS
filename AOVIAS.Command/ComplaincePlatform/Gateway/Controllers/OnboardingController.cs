using FluentValidation;
using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/onboarding")]
public class OnboardingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<OnboardWizardCommand> _onboardWizardValidator;
    private readonly IOnboardingWizardIdempotencyStore _wizardIdempotency;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(
        IMediator mediator,
        IValidator<OnboardWizardCommand> onboardWizardValidator,
        IOnboardingWizardIdempotencyStore wizardIdempotency,
        ILogger<OnboardingController> logger)
    {
        _mediator = mediator;
        _onboardWizardValidator = onboardWizardValidator;
        _wizardIdempotency = wizardIdempotency;
        _logger = logger;
    }

    /// <summary>
    /// Hospital onboarding endpoint that orchestrates organization, hospital, and admin user creation.
    /// This is a façade over the existing signup orchestration used by /api/v1/identity/register.
    /// </summary>
    /// <param name="request">Onboarding payload including organization, hospital, and admin user details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Standardized ApiResponse containing JWT and created resource identifiers.</returns>
    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SignupResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SignupResponseDto>>> OnboardHospital(
        [FromBody] SignupRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Hospital onboarding requested via /api/v1/onboarding/hospital for organization {OrganizationName}, hospital {HospitalName}",
            request.Organization?.Name,
            request.Hospital?.Name);

        try
        {
            var command = new SignupCommand(request);
            var response = await _mediator.Send(command, cancellationToken);

            var apiResponse = ApiResponseFactory.Success(
                ApiResponseCode.HOSPITAL_ONBOARDED,
                response,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_ONBOARDED];
            return StatusCode(meta.HttpStatus, apiResponse);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Hospital onboarding failed: {Message}", ex.Message);

            ApiResponseCode code;

            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                code = ApiResponseCode.RESOURCE_CONFLICT;
            }
            else
            {
                code = ApiResponseCode.VALIDATION_FAILED;
            }

            var failureResult = ApiResponseFactory.FailureResult<SignupResponseDto>(code, traceId);
            return failureResult;
        }
    }

    /// <summary>
    /// Second-step onboarding for an existing organization: hospital wizard profile, frameworks, assessment, team invites.
    /// Requires JWT with organization_id and hospital_id matching the payload.
    /// </summary>
    [HttpPost("onboard")]
    [Authorize(Policy = AuthConstants.Policies.ComplianceOrOrganizationScope)]
    [ProducesResponseType(typeof(ApiResponse<OnboardWizardResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OnboardWizardResponseDto>>> OnboardWizard(
        [FromBody] OnboardWizardRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Onboarding wizard requested for organization {OrganizationId}, hospital {HospitalId}",
            request.OrganizationId,
            request.HospitalId);

        try
        {
            var command = new OnboardWizardCommand(request);
            var validationResult = await _onboardWizardValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errList = validationResult.Errors
                    .Select(e => new ApiError { Field = e.PropertyName, Message = e.ErrorMessage })
                    .ToList();
                var fail = ApiResponseFactory.Failure<OnboardWizardResponseDto>(
                    ApiResponseCode.VALIDATION_FAILED,
                    errList,
                    traceId);
                var metaVal = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
                return StatusCode(metaVal.HttpStatus, fail);
            }

            var idempotencyHeader = Request.Headers["Idempotency-Key"].FirstOrDefault()
                ?? Request.Headers["idempotency-key"].FirstOrDefault();
            var cacheKey = _wizardIdempotency.BuildCacheKey(HttpContext, request, idempotencyHeader);
            if (cacheKey != null && _wizardIdempotency.TryGetReplay(cacheKey, out var replay) && replay != null)
            {
                var replayResp = ApiResponseFactory.Success(
                    ApiResponseCode.ONBOARDING_WIZARD_IDEMPOTENCY_REPLAY,
                    replay,
                    traceId);
                var replayMeta = ApiResponseCodes.Registry[ApiResponseCode.ONBOARDING_WIZARD_IDEMPOTENCY_REPLAY];
                return StatusCode(replayMeta.HttpStatus, replayResp);
            }

            var response = await _mediator.Send(command, cancellationToken);

            if (cacheKey != null)
            {
                _wizardIdempotency.StoreReplay(cacheKey, response);
            }

            var apiResponse = ApiResponseFactory.Success(
                ApiResponseCode.ONBOARDING_WIZARD_COMPLETED,
                response,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ONBOARDING_WIZARD_COMPLETED];
            return StatusCode(meta.HttpStatus, apiResponse);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Onboarding wizard unauthorized");
            return ApiResponseFactory.FailureResult<OnboardWizardResponseDto>(ApiResponseCode.UNAUTHORIZED, traceId);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("HOSPITAL_CLAIM_INVALID", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex, "Onboarding wizard claim mismatch");
            return ApiResponseFactory.FailureResult<OnboardWizardResponseDto>(ApiResponseCode.HOSPITAL_CLAIM_INVALID, traceId);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex, "Onboarding wizard conflict: {Message}", ex.Message);
            return ApiResponseFactory.FailureResult<OnboardWizardResponseDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Onboarding wizard validation failed: {Message}", ex.Message);
            return ApiResponseFactory.FailureResult<OnboardWizardResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }
    }

    /// <summary>
    /// Returns the latest saved wizard assessment for a hospital (proxies ComplianceService).
    /// </summary>
    [HttpGet("wizard/assessments/latest")]
    [Authorize(Policy = AuthConstants.Policies.ComplianceOrOrganizationScope)]
    public Task<IActionResult> GetLatestWizardAssessment(
        [FromQuery] Guid hospitalId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "GET wizard assessment latest: dispatching GetLatestWizardAssessmentQuery for HospitalId={HospitalId}",
            hospitalId);

        return _mediator.Send(new GetLatestWizardAssessmentQuery(hospitalId, apiVersion), cancellationToken);
    }

    /// <summary>
    /// Returns a dynamically generated assessment questionnaire for a compliance framework (proxies KnowledgeBase).
    /// </summary>
    [HttpGet("questionnaire/{frameworkId:guid}")]
    [Authorize(Policy = AuthConstants.Policies.ComplianceOrOrganizationScope)]
    [ProducesResponseType(typeof(ApiResponse<FrameworkQuestionnaireDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetOnboardingQuestionnaire(
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "GET onboarding questionnaire: dispatching GetOnboardingQuestionnaireQuery for FrameworkId={FrameworkId}",
            frameworkId);

        return _mediator.Send(new GetOnboardingQuestionnaireQuery(frameworkId, apiVersion), cancellationToken);
    }

    /// <summary>
    /// Returns the risk-selected Core maturity questions for a framework (proxies KnowledgeBase).
    /// GET api/v1/onboarding/core-assessment/{frameworkId}?count=40
    /// </summary>
    [HttpGet("core-assessment/{frameworkId:guid}")]
    [Authorize(Policy = AuthConstants.Policies.ComplianceOrOrganizationScope)]
    public Task<IActionResult> GetCoreAssessment(
        Guid frameworkId,
        [FromQuery] int count = 40,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "GET core assessment: dispatching GetCoreAssessmentQuery for FrameworkId={FrameworkId}, Count={Count}",
            frameworkId, count);

        return _mediator.Send(new GetCoreAssessmentQuery(frameworkId, count, apiVersion), cancellationToken);
    }

    /// <summary>
    /// CRP Phase 3 — composed onboarding journey (progress, trial, next actions). Does not mutate business state.
    /// </summary>
    [HttpGet("journey")]
    [Authorize(Policy = AuthConstants.Policies.ComplianceOrOrganizationScope)]
    [ProducesResponseType(typeof(ApiResponse<OnboardingJourneyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OnboardingJourneyDto>>> GetJourney(CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var journey = await _mediator.Send(new GetOnboardingJourneyQuery(), cancellationToken);
            var apiResponse = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, journey, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, apiResponse);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Onboarding journey unauthorized");
            return ApiResponseFactory.FailureResult<OnboardingJourneyDto>(ApiResponseCode.UNAUTHORIZED, traceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onboarding journey failed");
            return ApiResponseFactory.FailureResult<OnboardingJourneyDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
        }
    }

    /// <summary>CRP Phase 3 — client onboarding telemetry (funnel events).</summary>
    [HttpPost("events")]
    [Authorize(Policy = AuthConstants.Policies.ComplianceOrOrganizationScope)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public Task<IActionResult> PostTelemetryEvent(
        [FromBody] OnboardingTelemetryEventRequestDto body,
        CancellationToken cancellationToken)
        => _mediator.Send(new RecordOnboardingTelemetryCommand(body), cancellationToken);
}

