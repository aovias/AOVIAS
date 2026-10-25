using ComplianceService.Application.Contracts;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1.Copilot;

/// <summary>
/// Copilot recommendations API (Sprint 13B).
/// Exposes deterministic rule-engine-generated recommendations.
/// Consumed via the Gateway — portal never calls this directly.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/copilot/recommendations")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class CopilotRecommendationsController : ControllerBase
{
    private readonly ICopilotRecommendationService _service;
    private readonly IUserContextService _userContext;
    private readonly ILogger<CopilotRecommendationsController> _logger;

    public CopilotRecommendationsController(
        ICopilotRecommendationService service,
        IUserContextService userContext,
        ILogger<CopilotRecommendationsController> logger)
    {
        _service     = service;
        _userContext = userContext;
        _logger      = logger;
    }

    /// <summary>GET /copilot/recommendations — list all current recommendations for the organization.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CopilotRecommendationDto>>>> GetRecommendations(
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        if (organizationId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<CopilotRecommendationDto>>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        var results = await _service.GetRecommendationsAsync(organizationId, cancellationToken: cancellationToken);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(successMeta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, results, requestId));
    }

    /// <summary>GET /copilot/recommendations/persona/{persona} — top 5 for a specific persona.</summary>
    [HttpGet("persona/{persona}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CopilotRecommendationDto>>>> GetByPersona(
        string persona,
        [FromQuery] int max = 5,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        if (organizationId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<CopilotRecommendationDto>>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        var results = await _service.GetByPersonaAsync(organizationId, persona, max, cancellationToken);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(successMeta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, results, requestId));
    }

    /// <summary>GET /copilot/recommendations/{id} — retrieve a single recommendation by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CopilotRecommendationDto>>> GetRecommendation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        var result = await _service.GetRecommendationAsync(id, cancellationToken);

        if (result is null)
        {
            var notFoundMeta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(notFoundMeta.HttpStatus,
                ApiResponseFactory.Failure<CopilotRecommendationDto>(
                    ApiResponseCode.RESOURCE_NOT_FOUND, requestId));
        }

        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(successMeta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, result, requestId));
    }

    /// <summary>
    /// POST /copilot/recommendations/refresh
    /// Triggers the rule engine to regenerate recommendations from operational SSOT data.
    /// No writes to operational domains — recommendations only.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CopilotRecommendationDto>>>> Refresh(
        [FromBody] RefreshRecommendationsRequest? request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;
        var hospitalId = _userContext.HospitalId;

        if (organizationId == Guid.Empty || hospitalId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<CopilotRecommendationDto>>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        _logger.LogInformation(
            "Copilot recommendation refresh triggered OrgId={OrgId} HospId={HospId}",
            organizationId, hospitalId);

        var results = await _service.RefreshAsync(
            organizationId,
            hospitalId,
            request ?? new RefreshRecommendationsRequest(),
            cancellationToken);

        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(successMeta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, results, requestId));
    }
}
