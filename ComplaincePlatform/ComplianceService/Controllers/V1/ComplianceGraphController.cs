using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>Sprint 17A — Enterprise Compliance Graph &amp; Traceability (read-model projection).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/graph")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class ComplianceGraphController : ControllerBase
{
    private readonly IEnterpriseComplianceGraphService _graph;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ComplianceGraphController> _logger;

    public ComplianceGraphController(
        IEnterpriseComplianceGraphService graph,
        IUserContextService userContext,
        ILogger<ComplianceGraphController> logger)
    {
        _graph = graph;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("entity/{id:guid}")]
    public async Task<ActionResult<ApiResponse<GraphEntitySummaryDto>>> GetEntity(Guid id, CancellationToken ct)
    {
        var dto = await _graph.GetEntityAsync(RequireOrg(), id, ct);
        if (dto == null) return NotFound<GraphEntitySummaryDto>();
        return Success(dto);
    }

    [HttpGet("relationships/{id:guid}")]
    public async Task<ActionResult<ApiResponse<GraphRelationshipsDto>>> GetRelationships(Guid id, CancellationToken ct)
    {
        var dto = await _graph.GetRelationshipsAsync(RequireOrg(), id, ct);
        if (dto == null) return NotFound<GraphRelationshipsDto>();
        return Success(dto);
    }

    [HttpGet("timeline/{id:guid}")]
    public async Task<ActionResult<ApiResponse<GraphTimelineDto>>> GetTimeline(Guid id, CancellationToken ct)
    {
        var dto = await _graph.GetTimelineAsync(RequireOrg(), id, ct);
        if (dto == null) return NotFound<GraphTimelineDto>();
        return Success(dto);
    }

    [HttpGet("impact/{id:guid}")]
    public async Task<ActionResult<ApiResponse<GraphImpactDto>>> GetImpact(Guid id, CancellationToken ct)
    {
        var dto = await _graph.GetImpactAsync(RequireOrg(), id, ct);
        if (dto == null) return NotFound<GraphImpactDto>();
        return Success(dto);
    }

    [HttpGet("dependencies/{id:guid}")]
    public async Task<ActionResult<ApiResponse<GraphDependenciesDto>>> GetDependencies(Guid id, CancellationToken ct)
    {
        var dto = await _graph.GetDependenciesAsync(RequireOrg(), id, ct);
        if (dto == null) return NotFound<GraphDependenciesDto>();
        return Success(dto);
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<GraphSearchDto>>> Search(
        [FromQuery] string? q, [FromQuery] string? entityType, CancellationToken ct)
        => Success(await _graph.SearchAsync(RequireOrg(), q, entityType, ct));

    [HttpGet("navigation/{id:guid}")]
    public async Task<ActionResult<ApiResponse<GraphNavigationDto>>> GetNavigation(Guid id, CancellationToken ct)
    {
        var dto = await _graph.GetNavigationAsync(RequireOrg(), id, ct);
        if (dto == null) return NotFound<GraphNavigationDto>();
        return Success(dto);
    }

    [HttpGet("traceability/{id:guid}")]
    public async Task<ActionResult<ApiResponse<GraphTraceabilityDto>>> GetTraceability(Guid id, CancellationToken ct)
    {
        var dto = await _graph.GetTraceabilityAsync(RequireOrg(), _userContext.HospitalId, id, ct);
        if (dto == null) return NotFound<GraphTraceabilityDto>();
        return Success(dto);
    }

    [HttpGet("executive")]
    public async Task<ActionResult<ApiResponse<GraphExecutiveImpactDto>>> GetExecutive(CancellationToken ct)
        => Success(await _graph.GetExecutiveImpactAsync(RequireOrg(), ct));

    [HttpGet("analytics")]
    public async Task<ActionResult<ApiResponse<GraphAnalyticsDto>>> GetAnalytics(CancellationToken ct)
        => Success(await _graph.GetAnalyticsAsync(RequireOrg(), ct));

    [HttpGet("visual")]
    public async Task<ActionResult<ApiResponse<ComplianceGraphDto>>> GetVisualGraph(
        [FromQuery] Guid? centerId, CancellationToken ct)
        => Success(await _graph.GetVisualGraphAsync(RequireOrg(), centerId, ct));

    private Guid RequireOrg()
    {
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) throw new UnauthorizedAccessException();
        return orgId;
    }

    private ActionResult<ApiResponse<T>> Success<T>(T data)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));

    private ActionResult<ApiResponse<T>> NotFound<T>()
        => StatusCode(404, ApiResponseFactory.Failure<T>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
}
