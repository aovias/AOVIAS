using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceService.Controllers.Internal;

[ApiController]
[Route("internal/v1/assignment")]
[Authorize(Policy = "InternalOnly")]
public class InternalAssignmentController : ControllerBase
{
    private readonly IAssignmentRoutingService _routing;
    private readonly DepartmentOwnershipAppService _departmentOwnership;
    private readonly ILogger<InternalAssignmentController> _logger;

    public InternalAssignmentController(
        IAssignmentRoutingService routing,
        DepartmentOwnershipAppService departmentOwnership,
        ILogger<InternalAssignmentController> logger)
    {
        _routing = routing;
        _departmentOwnership = departmentOwnership;
        _logger = logger;
    }

    [HttpGet("deficiencies/{id:guid}/suggestion")]
    public async Task<IActionResult> GetDeficiencySuggestion(Guid id, CancellationToken ct)
    {
        var result = await _routing.ResolveForDeficiencyAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("deficiencies/{id:guid}/auto-assign")]
    public async Task<IActionResult> AutoAssignDeficiency(
        Guid id,
        [FromQuery] bool force = false,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _routing.TryAutoAssignDeficiencyAsync(
                id, forceAutoAssign: force, User.Identity?.Name, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Auto-assign failed for deficiency {Id}.", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("findings/{id:guid}/suggestion")]
    public async Task<IActionResult> GetFindingSuggestion(
        Guid id,
        [FromQuery] Guid organizationId,
        [FromQuery] Guid hospitalId,
        CancellationToken ct)
    {
        var result = await _routing.ResolveForFindingAsync(id, organizationId, hospitalId, ct);
        return Ok(result);
    }

    [HttpGet("objective-elements/{id:guid}/suggestion")]
    public async Task<IActionResult> GetOeSuggestion(
        Guid id,
        [FromQuery] Guid organizationId,
        [FromQuery] Guid hospitalId,
        CancellationToken ct)
    {
        var result = await _routing.ResolveForObjectiveElementAsync(organizationId, hospitalId, id, ct);
        return Ok(result);
    }

    [HttpGet("department-ownership")]
    public async Task<IActionResult> ListDepartmentOwnership(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? hospitalId,
        CancellationToken ct)
    {
        var result = await _departmentOwnership.ListAsync(organizationId, hospitalId, ct);
        return Ok(result);
    }

    [HttpPut("department-ownership")]
    public async Task<IActionResult> UpsertDepartmentOwnership(
        [FromBody] UpsertDepartmentOwnershipRequestDto body,
        CancellationToken ct)
    {
        if (body.OrganizationId == Guid.Empty || body.DepartmentId == Guid.Empty
            || string.IsNullOrWhiteSpace(body.PrimaryOwnerUserId))
        {
            return BadRequest(new { message = "OrganizationId, DepartmentId, and PrimaryOwnerUserId are required." });
        }

        var result = await _departmentOwnership.UpsertAsync(body, ct);
        return Ok(result);
    }

    [HttpGet("department-label-mappings")]
    public async Task<IActionResult> GetLabelMappings(CancellationToken ct)
    {
        return Ok(await _departmentOwnership.GetLabelMappingsAsync(ct));
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics([FromQuery] Guid organizationId, CancellationToken ct)
    {
        var result = await _routing.GetMetricsAsync(organizationId, ct);
        return Ok(result);
    }
}
