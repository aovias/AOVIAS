using ComplianceService.Application.Commands;

using ComplianceService.Application.Contracts;

using ComplianceService.Application.DTOs;

using ComplianceService.Application.Services;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;



namespace ComplianceService.Controllers.Internal;



/// <summary>

/// Internal deficiency API exposed only to the Gateway via InternalOnly policy.

/// All routes under internal/v1/deficiencies.

/// </summary>

[ApiController]

[Route("internal/v1/deficiencies")]

[Authorize(Policy = "InternalOnly")]

public class InternalDeficienciesController : ControllerBase

{

    private readonly IDeficiencyQueryService _queryService;

    private readonly IComplianceJourneyContextResolver _contextResolver;

    private readonly PatchDeficiencyStatusCommandHandler _patchStatusHandler;

    private readonly PatchDeficiencyRootCauseCommandHandler _patchRootCauseHandler;

    private readonly PatchDeficiencyRcaCommandHandler _patchRcaHandler;

    private readonly PatchDeficiencyRemediationCommandHandler _patchRemediationHandler;

    private readonly PatchDeficiencyCorrectiveActionCommandHandler _patchCorrectiveActionHandler;

    private readonly PatchDeficiencyPreventiveActionCommandHandler _patchPreventiveActionHandler;

    private readonly PatchDeficiencyEffectivenessReviewCommandHandler _patchEffectivenessReviewHandler;

    private readonly ICapaQueryService _capaQuery;

    private readonly AssignDeficiencyCommandHandler _assignHandler;

    private readonly AcceptDeficiencyCommandHandler _acceptHandler;

    private readonly RejectDeficiencyOwnershipCommandHandler _rejectOwnershipHandler;

    private readonly IDeficiencyCommandCenterQueryService _commandCenter;

    private readonly ILogger<InternalDeficienciesController> _logger;



    public InternalDeficienciesController(

        IDeficiencyQueryService queryService,

        IDeficiencyCommandCenterQueryService commandCenter,

        IComplianceJourneyContextResolver contextResolver,

        PatchDeficiencyStatusCommandHandler patchStatusHandler,

        PatchDeficiencyRootCauseCommandHandler patchRootCauseHandler,

        PatchDeficiencyRcaCommandHandler patchRcaHandler,

        PatchDeficiencyRemediationCommandHandler patchRemediationHandler,

        PatchDeficiencyCorrectiveActionCommandHandler patchCorrectiveActionHandler,

        PatchDeficiencyPreventiveActionCommandHandler patchPreventiveActionHandler,

        PatchDeficiencyEffectivenessReviewCommandHandler patchEffectivenessReviewHandler,

        ICapaQueryService capaQuery,

        AssignDeficiencyCommandHandler assignHandler,

        AcceptDeficiencyCommandHandler acceptHandler,

        RejectDeficiencyOwnershipCommandHandler rejectOwnershipHandler,

        ILogger<InternalDeficienciesController> logger)

    {

        _queryService       = queryService;

        _commandCenter      = commandCenter;

        _contextResolver    = contextResolver;

        _patchStatusHandler = patchStatusHandler;

        _patchRootCauseHandler = patchRootCauseHandler;

        _patchRcaHandler = patchRcaHandler;

        _patchRemediationHandler = patchRemediationHandler;

        _patchCorrectiveActionHandler = patchCorrectiveActionHandler;

        _patchPreventiveActionHandler = patchPreventiveActionHandler;

        _patchEffectivenessReviewHandler = patchEffectivenessReviewHandler;

        _capaQuery = capaQuery;

        _assignHandler      = assignHandler;

        _acceptHandler = acceptHandler;

        _rejectOwnershipHandler = rejectOwnershipHandler;

        _logger             = logger;

    }



    /// <summary>

    /// GET /internal/v1/deficiencies?hospitalId=&amp;frameworkId=&amp;status=&amp;severity=&amp;page=&amp;pageSize=

    /// </summary>

    [HttpGet]

    public async Task<IActionResult> GetList(

        [FromQuery] Guid hospitalId,

        [FromQuery] Guid frameworkId,

        [FromQuery] string? status = null,

        [FromQuery] string? severity = null,

        [FromQuery] int page = 1,

        [FromQuery] int pageSize = 20,

        CancellationToken ct = default)

    {

        var resolved = await ResolveFrameworkIdAsync(hospitalId, frameworkId, ct);

        if (resolved == null)

            return Ok(new DeficiencyListDto());



        var result = await _queryService.GetListAsync(

            hospitalId, resolved.Value, status, severity, page, pageSize, ct);

        return Ok(result);

    }



    /// <summary>

    /// GET /internal/v1/deficiencies/{id}

    /// </summary>

    [HttpGet("{id:guid}")]

    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)

    {

        var result = await _queryService.GetByIdAsync(id, ct);

        if (result == null)

            return NotFound(new { message = $"Deficiency {id} not found." });

        return Ok(result);

    }



    /// <summary>

    /// GET /internal/v1/deficiencies/summary?hospitalId=&amp;frameworkId=

    /// </summary>

    [HttpGet("summary")]

    public async Task<IActionResult> GetSummary(

        [FromQuery] Guid hospitalId,

        [FromQuery] Guid frameworkId,

        CancellationToken ct = default)

    {

        var resolved = await ResolveFrameworkIdAsync(hospitalId, frameworkId, ct);

        if (resolved == null)

            return Ok(new DeficiencySummaryDto());



        var result = await _queryService.GetSummaryAsync(hospitalId, resolved.Value, ct);

        return Ok(result);

    }



    /// <summary>

    /// GET /internal/v1/deficiencies/by-stage?hospitalId=&amp;frameworkId=

    /// </summary>

    [HttpGet("by-stage")]

    public async Task<IActionResult> GetByStage(

        [FromQuery] Guid hospitalId,

        [FromQuery] Guid frameworkId,

        CancellationToken ct = default)

    {

        var resolved = await ResolveFrameworkIdAsync(hospitalId, frameworkId, ct);

        if (resolved == null)

            return Ok(Array.Empty<DeficiencyByStageDto>());



        var result = await _queryService.GetByStageAsync(hospitalId, resolved.Value, ct);

        return Ok(result);

    }



    /// <summary>GET /internal/v1/deficiencies/command-center/dashboard</summary>
    [HttpGet("command-center/dashboard")]
    public async Task<IActionResult> GetCommandCenterDashboard(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken ct = default)
    {
        var resolved = await ResolveFrameworkIdAsync(hospitalId, frameworkId, ct);
        if (resolved == null)
            return Ok(new DeficiencyCommandCenterDashboardDto());

        var result = await _commandCenter.GetDashboardAsync(hospitalId, resolved.Value, ct);
        return Ok(result);
    }

    /// <summary>GET /internal/v1/deficiencies/command-center/workspace</summary>
    [HttpGet("command-center/workspace")]
    public async Task<IActionResult> GetCommandCenterWorkspace(
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
        CancellationToken ct = default)
    {
        var resolved = await ResolveFrameworkIdAsync(hospitalId, frameworkId, ct);
        if (resolved == null)
            return Ok(new DeficiencyWorkspaceListDto());

        var result = await _commandCenter.GetWorkspaceListAsync(
            hospitalId, resolved.Value, status, severity, maturityLevel, search,
            objectiveElementId, sortBy, sortDesc, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>GET /internal/v1/deficiencies/command-center/workspace/{id}</summary>
    [HttpGet("command-center/workspace/{id:guid}")]
    public async Task<IActionResult> GetCommandCenterWorkspaceDetail(Guid id, CancellationToken ct = default)
    {
        var result = await _commandCenter.GetWorkspaceDetailAsync(id, ct);
        if (result == null)
            return NotFound(new { message = $"Deficiency {id} not found." });
        return Ok(result);
    }

    /// <summary>PATCH /internal/v1/deficiencies/{id}/root-cause</summary>
    [HttpPatch("{id:guid}/root-cause")]
    public async Task<IActionResult> PatchRootCause(
        Guid id,
        [FromBody] PatchDeficiencyRootCauseRequestDto body,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(body?.RootCauseCategory))
            return BadRequest(new { message = "RootCauseCategory is required." });

        try
        {
            await _patchRootCauseHandler.HandleAsync(
                new PatchDeficiencyRootCauseCommand(id, body.RootCauseCategory, User.Identity?.Name), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "PatchDeficiencyRootCause failed for {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PATCH /internal/v1/deficiencies/{id}/rca</summary>
    [HttpPatch("{id:guid}/rca")]
    public async Task<IActionResult> PatchRca(
        Guid id,
        [FromBody] PatchDeficiencyRcaRequestDto body,
        CancellationToken ct = default)
    {
        try
        {
            await _patchRcaHandler.HandleAsync(
                new PatchDeficiencyRcaCommand(
                    id,
                    body?.RootCauseCategory,
                    body?.ProblemStatement,
                    body?.RootCauseSummary,
                    body?.ImpactDescription,
                    body?.CorrectiveActionSummary,
                    body?.PreventiveActionSummary,
                    body?.Why1,
                    body?.Why2,
                    body?.Why3,
                    body?.Why4,
                    body?.Why5,
                    body?.FinalRootCause,
                    User.Identity?.Name),
                ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "PatchDeficiencyRca failed for {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>GET /internal/v1/deficiencies/{id}/capa</summary>
    [HttpGet("{id:guid}/capa")]
    public async Task<IActionResult> GetCapa(Guid id, CancellationToken ct = default)
    {
        var result = await _capaQuery.GetCapaAsync(id, ct);
        if (result == null)
            return NotFound(new { message = $"Deficiency {id} not found." });
        return Ok(result);
    }

    /// <summary>PATCH /internal/v1/deficiencies/{id}/corrective-action</summary>
    [HttpPatch("{id:guid}/corrective-action")]
    public async Task<IActionResult> PatchCorrectiveAction(
        Guid id,
        [FromBody] PatchDeficiencyCorrectiveActionRequestDto body,
        CancellationToken ct = default)
    {
        try
        {
            await _patchCorrectiveActionHandler.HandleAsync(
                new PatchDeficiencyCorrectiveActionCommand(
                    id,
                    body?.Summary,
                    body?.Owner,
                    body?.StartDate,
                    body?.TargetDate,
                    body?.CompletedDate,
                    body?.Status,
                    User.Identity?.Name),
                ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "PatchCorrectiveAction failed for {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PATCH /internal/v1/deficiencies/{id}/preventive-action</summary>
    [HttpPatch("{id:guid}/preventive-action")]
    public async Task<IActionResult> PatchPreventiveAction(
        Guid id,
        [FromBody] PatchDeficiencyPreventiveActionRequestDto body,
        CancellationToken ct = default)
    {
        try
        {
            await _patchPreventiveActionHandler.HandleAsync(
                new PatchDeficiencyPreventiveActionCommand(
                    id,
                    body?.Summary,
                    body?.Owner,
                    body?.TargetDate,
                    body?.CompletedDate,
                    body?.SuccessCriteria,
                    body?.Status,
                    User.Identity?.Name),
                ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "PatchPreventiveAction failed for {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PATCH /internal/v1/deficiencies/{id}/effectiveness-review</summary>
    [HttpPatch("{id:guid}/effectiveness-review")]
    public async Task<IActionResult> PatchEffectivenessReview(
        Guid id,
        [FromBody] PatchDeficiencyEffectivenessReviewRequestDto body,
        CancellationToken ct = default)
    {
        try
        {
            await _patchEffectivenessReviewHandler.HandleAsync(
                new PatchDeficiencyEffectivenessReviewCommand(
                    id,
                    body?.Status,
                    body?.ScheduledDate,
                    body?.ReviewDaysOffset,
                    body?.ReviewDate,
                    body?.Reviewer,
                    body?.Notes,
                    body?.Outcome,
                    User.Identity?.Name),
                ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "PatchEffectivenessReview failed for {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PATCH /internal/v1/deficiencies/{id}/remediation</summary>
    [HttpPatch("{id:guid}/remediation")]
    public async Task<IActionResult> PatchRemediation(
        Guid id,
        [FromBody] PatchDeficiencyRemediationRequestDto body,
        CancellationToken ct = default)
    {
        try
        {
            await _patchRemediationHandler.HandleAsync(
                new PatchDeficiencyRemediationCommand(
                    id,
                    body?.RiskLikelihood,
                    body?.RiskImpact,
                    body?.Department,
                    body?.TargetResolutionDate,
                    body?.ActionPlan,
                    body?.ExpectedOutcome,
                    body?.RemediationTargetCompletion,
                    body?.SuccessCriteria,
                    body?.EffectivenessReviewStatus,
                    User.Identity?.Name),
                ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "PatchDeficiencyRemediation failed for {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }
    }



    /// <summary>

    /// PATCH /internal/v1/deficiencies/{id}/status

    /// </summary>

    [HttpPatch("{id:guid}/status")]

    public async Task<IActionResult> PatchStatus(

        Guid id,

        [FromBody] PatchDeficiencyStatusRequestDto body,

        CancellationToken ct = default)

    {

        if (string.IsNullOrWhiteSpace(body?.Status))

            return BadRequest(new { message = "Status is required." });



        try

        {

            await _patchStatusHandler.HandleAsync(

                new PatchDeficiencyStatusCommand(id, body.Status, User.Identity?.Name), ct);

            return NoContent();

        }

        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "PatchDeficiencyStatus failed for {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "PatchDeficiencyStatus validation failed for {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }

    }



    /// <summary>

    /// POST /internal/v1/deficiencies/{id}/assign

    /// </summary>

    [HttpPost("{id:guid}/assign")]

    public async Task<IActionResult> Assign(

        Guid id,

        [FromBody] AssignDeficiencyRequestDto body,

        CancellationToken ct = default)

    {

        if (string.IsNullOrWhiteSpace(body?.AssignedTo))

            return BadRequest(new { message = "AssignedTo is required." });



        try

        {

            await _assignHandler.HandleAsync(

                new AssignDeficiencyCommand(id, body.AssignedTo, body.DueDate, User.Identity?.Name), ct);

            return NoContent();

        }

        catch (InvalidOperationException ex)

        {

            _logger.LogWarning(ex, "AssignDeficiency failed for {Id}.", id);

            return BadRequest(new { message = ex.Message });

        }

    }



    
    /// <summary>
    /// POST /internal/v1/deficiencies/{id}/accept
    /// </summary>
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(
        Guid id,
        [FromBody] AcceptDeficiencyRequestDto? body,
        CancellationToken ct = default)
    {
        var acceptedBy = !string.IsNullOrWhiteSpace(body?.AcceptedBy)
            ? body!.AcceptedBy!
            : User.Identity?.Name ?? "system";

        try
        {
            await _acceptHandler.HandleAsync(new AcceptDeficiencyCommand(id, acceptedBy), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "AcceptDeficiency failed for {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /internal/v1/deficiencies/{id}/reject-ownership
    /// </summary>
    [HttpPost("{id:guid}/reject-ownership")]
    public async Task<IActionResult> RejectOwnership(
        Guid id,
        [FromBody] RejectDeficiencyOwnershipRequestDto? body,
        CancellationToken ct = default)
    {
        var rejectedBy = !string.IsNullOrWhiteSpace(body?.RejectedBy)
            ? body!.RejectedBy!
            : User.Identity?.Name ?? "system";

        try
        {
            await _rejectOwnershipHandler.HandleAsync(
                new RejectDeficiencyOwnershipCommand(id, rejectedBy, body?.Reason), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "RejectDeficiencyOwnership failed for {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>

    /// Resolves portal journeyId (stored as frameworkId) to canonical ComplianceFramework.Id.

    /// </summary>

    private async Task<Guid?> ResolveFrameworkIdAsync(

        Guid hospitalId,

        Guid frameworkOrJourneyId,

        CancellationToken ct)

    {

        var ctx = await _contextResolver.ResolveAsync(hospitalId, frameworkOrJourneyId, ct);

        if (ctx == null)

        {

            _logger.LogWarning(

                "Deficiency query: unresolved framework context HospitalId={HospitalId} Id={Id}.",

                hospitalId, frameworkOrJourneyId);

            return null;

        }



        if (ctx.FrameworkId != frameworkOrJourneyId)

        {

            _logger.LogDebug(

                "Deficiency query: resolved JourneyId/FrameworkOrJourneyId={Input} → FrameworkId={FrameworkId}.",

                frameworkOrJourneyId, ctx.FrameworkId);

        }



        return ctx.FrameworkId;

    }

}


