using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Phase 5 Gateway endpoints for compliance deficiency engine.
/// All endpoints proxy to ComplianceService internal/v1/deficiencies/* via IProxyService.
/// Route prefix: api/v{version}/deficiencies
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/deficiencies")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class DeficienciesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DeficienciesController> _logger;

    public DeficienciesController(
        IMediator mediator,
        ILogger<DeficienciesController> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>
    /// Returns paginated, filterable deficiency list for a hospital × framework.
    /// GET /api/v1/deficiencies?hospitalId=&amp;frameworkId=&amp;status=&amp;severity=&amp;page=&amp;pageSize=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "GET /api/v{Version}/deficiencies - hospitalId={HospitalId}, frameworkId={FrameworkId}",
            apiVersion, hospitalId, frameworkId);

        return await _mediator.Send(
            new GetDeficienciesQuery(hospitalId, frameworkId, status, severity, page, pageSize, apiVersion),
            cancellationToken);
    }

    /// <summary>
    /// Returns a single deficiency by ID.
    /// GET /api/v1/deficiencies/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation("GET /api/v{Version}/deficiencies/{Id}", apiVersion, id);

        return await _mediator.Send(new GetDeficiencyByIdQuery(id, apiVersion), cancellationToken);
    }

    /// <summary>
    /// Returns aggregate severity summary (Critical/Major/Moderate/Minor counts).
    /// GET /api/v1/deficiencies/summary?hospitalId=&amp;frameworkId=
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "GET /api/v{Version}/deficiencies/summary - hospitalId={HospitalId}, frameworkId={FrameworkId}",
            apiVersion, hospitalId, frameworkId);

        return await _mediator.Send(
            new GetDeficiencySummaryQuery(hospitalId, frameworkId, apiVersion),
            cancellationToken);
    }

    /// <summary>
    /// Returns deficiency breakdown per maturity stage.
    /// GET /api/v1/deficiencies/by-stage?hospitalId=&amp;frameworkId=
    /// </summary>
    [HttpGet("by-stage")]
    public async Task<IActionResult> GetByStage(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation(
            "GET /api/v{Version}/deficiencies/by-stage - hospitalId={HospitalId}, frameworkId={FrameworkId}",
            apiVersion, hospitalId, frameworkId);

        return await _mediator.Send(
            new GetDeficienciesByStageQuery(hospitalId, frameworkId, apiVersion),
            cancellationToken);
    }

    /// <summary>
    /// Transitions a deficiency to a new status.
    /// PATCH /api/v1/deficiencies/{id}/status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> PatchStatus(
        Guid id,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation("PATCH /api/v{Version}/deficiencies/{Id}/status", apiVersion, id);

        return await _mediator.Send(new PatchDeficiencyStatusCommand(id, body, apiVersion), cancellationToken);
    }

    /// <summary>
    /// Assigns a deficiency to a person with optional due date.
    /// POST /api/v1/deficiencies/{id}/assign
    /// </summary>
    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        _logger.LogInformation("POST /api/v{Version}/deficiencies/{Id}/assign", apiVersion, id);

        return await _mediator.Send(new AssignDeficiencyCommand(id, body, apiVersion), cancellationToken);
    }

    /// <summary>POST /api/v1/deficiencies/{id}/accept — CAPA owner acceptance.</summary>
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(
        Guid id,
        [FromBody] object? body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(new AcceptDeficiencyGatewayCommand(id, body, apiVersion), cancellationToken);
    }

    /// <summary>POST /api/v1/deficiencies/{id}/reject-ownership — CAPA ownership rejection.</summary>
    [HttpPost("{id:guid}/reject-ownership")]
    public async Task<IActionResult> RejectOwnership(
        Guid id,
        [FromBody] object? body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(new RejectDeficiencyOwnershipGatewayCommand(id, body, apiVersion), cancellationToken);
    }

    /// <summary>
    /// Applies saved onboarding assessment answers to obligations and evaluates deficiencies.
    /// POST /api/v1/deficiencies/bootstrap-from-onboarding
    /// </summary>
    [HttpPost("bootstrap-from-onboarding")]
    public async Task<IActionResult> BootstrapFromOnboarding(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        // The tenant a write lands in is decided by the token, never by the caller.
        //
        // This endpoint creates deficiencies stamped with the supplied organization id. Taking that
        // id from the query string meant a client could write into any tenant, and in practice one
        // did: the web app fell back to sending the *hospital* id when its organization context had
        // not hydrated, producing a full duplicate set stamped OrganizationId = TenantId = HospitalId.
        // The query parameter is retained for contract compatibility but is no longer authoritative.
        var authenticatedOrganizationId = ResolveOrganizationId();
        if (authenticatedOrganizationId == Guid.Empty)
        {
            _logger.LogWarning("bootstrap-from-onboarding rejected: no organization claim on the token.");
            return Unauthorized();
        }

        if (organizationId != Guid.Empty && organizationId != authenticatedOrganizationId)
        {
            _logger.LogWarning(
                "bootstrap-from-onboarding organizationId mismatch — caller sent {Supplied}, token asserts {Authenticated}. Using the token value.",
                organizationId, authenticatedOrganizationId);
        }

        _logger.LogInformation(
            "POST /api/v{Version}/deficiencies/bootstrap-from-onboarding - organizationId={OrganizationId}, hospitalId={HospitalId}, frameworkId={FrameworkId}",
            apiVersion, authenticatedOrganizationId, hospitalId, frameworkId);

        return await _mediator.Send(
            new BootstrapDeficienciesFromOnboardingCommand(authenticatedOrganizationId, hospitalId, frameworkId, apiVersion),
            cancellationToken);
    }

    /// <summary>
    /// Organization id asserted by the caller's token. Empty when absent — callers must treat that
    /// as "no tenant context" and refuse to proceed rather than substituting another identifier.
    /// </summary>
    private Guid ResolveOrganizationId()
    {
        var claim = User.FindFirst("organization_id")?.Value
                    ?? User.FindFirst("organizationId")?.Value
                    ?? User.FindFirst("org_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    [HttpGet("command-center/dashboard")]
    public async Task<IActionResult> GetCommandCenterDashboard(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(
            new GetDeficiencyCommandCenterDashboardQuery(hospitalId, frameworkId, apiVersion),
            cancellationToken);
    }

    [HttpGet("command-center/workspace")]
    public async Task<IActionResult> GetWorkspace(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? maturityLevel = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? objectiveElementId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(
            new GetDeficiencyWorkspaceQuery(
                hospitalId, frameworkId, status, severity, maturityLevel, search,
                objectiveElementId, sortBy, sortDesc, page, pageSize, apiVersion),
            cancellationToken);
    }

    [HttpGet("command-center/workspace/{id:guid}")]
    public async Task<IActionResult> GetWorkspaceDetail(Guid id, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(new GetDeficiencyWorkspaceDetailQuery(id, apiVersion), cancellationToken);
    }

    [HttpPatch("{id:guid}/root-cause")]
    public async Task<IActionResult> PatchRootCause(
        Guid id,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(new PatchDeficiencyRootCauseCommand(id, body, apiVersion), cancellationToken);
    }

    [HttpPatch("{id:guid}/rca")]
    public async Task<IActionResult> PatchRca(
        Guid id,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(new PatchDeficiencyRcaCommand(id, body, apiVersion), cancellationToken);
    }

    [HttpPatch("{id:guid}/remediation")]
    public async Task<IActionResult> PatchRemediation(
        Guid id,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(new PatchDeficiencyRemediationCommand(id, body, apiVersion), cancellationToken);
    }

    [HttpPatch("{id:guid}/corrective-action")]
    public async Task<IActionResult> PatchCorrectiveAction(
        Guid id,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(new PatchDeficiencyCorrectiveActionCommand(id, body, apiVersion), cancellationToken);
    }

    [HttpPatch("{id:guid}/preventive-action")]
    public async Task<IActionResult> PatchPreventiveAction(
        Guid id,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(new PatchDeficiencyPreventiveActionCommand(id, body, apiVersion), cancellationToken);
    }

    [HttpPatch("{id:guid}/effectiveness-review")]
    public async Task<IActionResult> PatchEffectivenessReview(
        Guid id,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(new PatchDeficiencyEffectivenessReviewCommand(id, body, apiVersion), cancellationToken);
    }

    [HttpGet("{id:guid}/capa")]
    public async Task<IActionResult> GetCapa(Guid id, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return await _mediator.Send(new GetDeficiencyCapaQuery(id, apiVersion), cancellationToken);
    }
}
