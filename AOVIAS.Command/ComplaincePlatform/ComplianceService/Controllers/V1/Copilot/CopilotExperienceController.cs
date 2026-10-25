using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.RoleExperience;
using ComplianceService.Domain.Copilot;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1.Copilot;

/// <summary>Sprint 13E — persona experience workspace, activity feed, and usage metrics.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/copilot")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class CopilotExperienceController : ControllerBase
{
    private readonly IPersonaExperienceService _experience;
    private readonly ICopilotActivityService _activity;
    private readonly IUserContextService _userContext;

    public CopilotExperienceController(
        IPersonaExperienceService experience,
        ICopilotActivityService activity,
        IUserContextService userContext)
    {
        _experience   = experience;
        _activity     = activity;
        _userContext  = userContext;
    }

  /// <summary>GET /copilot/experience/catalog — persona experience definitions.</summary>
    [HttpGet("experience/catalog")]
    public ActionResult<ApiResponse<IReadOnlyList<PersonaExperienceDefinitionDto>>> GetCatalog()
    {
        var requestId = HttpContext.TraceIdentifier;
        var catalog = _experience.GetExperienceCatalog();
        var meta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(meta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, catalog, requestId));
    }

    /// <summary>GET /copilot/experience/workspace — persona-filtered workspace bundle.</summary>
    [HttpGet("experience/workspace")]
    public async Task<ActionResult<ApiResponse<PersonaWorkspaceDto>>> GetWorkspace(
        [FromQuery] string? persona = null,
        [FromQuery] Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;
        var hospitalId = _userContext.HospitalId;

        if (organizationId == Guid.Empty || hospitalId == Guid.Empty)
        {
            var missing = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(missing.HttpStatus,
                ApiResponseFactory.Failure<PersonaWorkspaceDto>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        CopilotPersona personaEnum;
        if (!string.IsNullOrWhiteSpace(persona)
            && Enum.TryParse(persona, ignoreCase: true, out CopilotPersona parsed))
        {
            personaEnum = parsed;
        }
        else if (RoleExperiencePackRegistry.TryResolvePersona(_userContext.Roles, out var fromRoles))
        {
            personaEnum = fromRoles;
        }
        else
        {
            personaEnum = CopilotPersona.AccreditationCoach;
        }

        var workspace = await _experience.GetWorkspaceAsync(
            personaEnum,
            organizationId,
            hospitalId,
            frameworkId ?? Guid.Empty,
            cancellationToken);

        var meta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(meta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, workspace, requestId));
    }

    /// <summary>GET /copilot/activity — recent copilot activity feed (read-only).</summary>
    [HttpGet("activity")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CopilotActivityEventDto>>>> GetActivity(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        if (organizationId == Guid.Empty)
        {
            var missing = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(missing.HttpStatus,
                ApiResponseFactory.Failure<IReadOnlyList<CopilotActivityEventDto>>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        var feed = await _activity.GetFeedAsync(organizationId, limit, cancellationToken);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(meta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, feed, requestId));
    }

    /// <summary>GET /copilot/usage-metrics — aggregated usage metrics (no dashboard yet).</summary>
    [HttpGet("usage-metrics")]
    public async Task<ActionResult<ApiResponse<CopilotUsageMetricsDto>>> GetUsageMetrics(
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        if (organizationId == Guid.Empty)
        {
            var missing = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(missing.HttpStatus,
                ApiResponseFactory.Failure<CopilotUsageMetricsDto>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        var metrics = await _activity.GetMetricsAsync(organizationId, cancellationToken);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(meta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, metrics, requestId));
    }

    /// <summary>POST /copilot/activity — record a client-side copilot engagement event.</summary>
    [HttpPost("activity")]
    public async Task<ActionResult<ApiResponse<object>>> RecordActivity(
        [FromBody] RecordCopilotActivityRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        if (organizationId == Guid.Empty)
        {
            var missing = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(missing.HttpStatus,
                ApiResponseFactory.Failure<object>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        if (request is null || string.IsNullOrWhiteSpace(request.EventType))
        {
            var invalid = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(invalid.HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, requestId));
        }

        await _activity.RecordAsync(
            organizationId,
            request.EventType,
            request.Persona,
            _userContext.UserId,
            _userContext.Email,
            request.EntityType,
            request.EntityId,
            request.Notes,
            cancellationToken);

        var meta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(meta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, new { recorded = true }, requestId));
    }
}
