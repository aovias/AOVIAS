using ComplianceService.Application.Contracts;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Domain.Copilot;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using ComplianceService.Infrastructure.Clients;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1.Copilot;

/// <summary>
/// Copilot persona routing API (Sprint 13A).
/// Returns the active persona for the current session context and the full persona catalog.
/// Consumed via the Gateway — portal never calls this directly.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/copilot/personas")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class CopilotPersonasController : ControllerBase
{
    private readonly ICopilotPersonaService _service;
    private readonly IUserContextService _userContext;
    private readonly IAiProfileBridgeClient _aiProfileBridge;
    private readonly ILogger<CopilotPersonasController> _logger;

    public CopilotPersonasController(
        ICopilotPersonaService service,
        IUserContextService userContext,
        IAiProfileBridgeClient aiProfileBridge,
        ILogger<CopilotPersonasController> logger)
    {
        _service     = service;
        _userContext = userContext;
        _aiProfileBridge = aiProfileBridge;
        _logger      = logger;
    }

    /// <summary>Returns the full persona catalog.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PersonaResponseDto>>>> GetPersonas(CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;
        var catalog = _service.GetPersonaCatalog();
        var enriched = new List<PersonaResponseDto>();
        foreach (var p in catalog)
        {
            var profileId = await _aiProfileBridge.GetProfileIdForPersonaAsync(p.Id, ct);
            enriched.Add(new PersonaResponseDto { Id = p.Id, DisplayName = p.DisplayName, Description = p.Description, PageContexts = p.PageContexts, IsActive = p.IsActive, AiProfileId = profileId });
        }
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(successMeta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, (IReadOnlyList<PersonaResponseDto>)enriched, requestId));
    }

    /// <summary>
    /// Resolves the active persona given the current session context.
    /// Accepts page context and optional manual override as query parameters.
    /// </summary>
    [HttpGet("active")]
    public ActionResult<ApiResponse<PersonaResponseDto>> GetActivePersona(
        [FromQuery] string? pageContext = null,
        [FromQuery] string? focusedEntityType = null,
        [FromQuery] Guid? focusedEntityId = null,
        [FromQuery] string? personaOverride = null,
        [FromQuery] string? conversationSummary = null)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        CopilotPersona? overrideValue = null;
        if (!string.IsNullOrWhiteSpace(personaOverride) &&
            Enum.TryParse<CopilotPersona>(personaOverride, ignoreCase: true, out var parsed))
        {
            overrideValue = parsed;
        }

        var context = new CopilotContext
        {
            OrganizationId      = organizationId,
            PageContext           = pageContext ?? string.Empty,
            FocusedEntityId       = focusedEntityId,
            FocusedEntityType     = focusedEntityType,
            ConversationSummary   = conversationSummary,
            PersonaOverride       = overrideValue,
            UserRoles             = _userContext.Roles,
        };

        var active = _service.GetActivePersona(context);
        var catalog = _service.GetPersonaCatalog();
        var matched = catalog.FirstOrDefault(p => p.Id == active.ToString()) ?? new PersonaResponseDto
        {
            Id          = active.ToString(),
            DisplayName = active.ToString(),
            IsActive    = true,
        };

        var activeDto = new PersonaResponseDto
        {
            Id           = matched.Id,
            DisplayName  = matched.DisplayName,
            Description  = matched.Description,
            PageContexts = matched.PageContexts,
            IsActive     = true,
        };

        _logger.LogInformation(
            "Copilot persona resolved: {Persona} for PageContext={PageContext}", active, pageContext);

        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(successMeta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, activeDto, requestId));
    }
}
