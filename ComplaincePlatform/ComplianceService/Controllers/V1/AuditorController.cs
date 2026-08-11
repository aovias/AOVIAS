using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// External Auditor Workspace — independent assessment layer (Sprint 14).
/// Read-first; auditors create observations, findings (via existing API), and comments only.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auditor")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class AuditorController : ControllerBase
{
    private readonly IAuditorWorkspaceQueryService _workspace;
    private readonly IAuditorTraceabilityService _traceability;
    private readonly IAuditorReportService _report;
    private readonly IAuditSimulationAppService _simulation;
    private readonly IUserContextService _userContext;
    private readonly ILogger<AuditorController> _logger;

    public AuditorController(
        IAuditorWorkspaceQueryService workspace,
        IAuditorTraceabilityService traceability,
        IAuditorReportService report,
        IAuditSimulationAppService simulation,
        IUserContextService userContext,
        ILogger<AuditorController> logger)
    {
        _workspace = workspace;
        _traceability = traceability;
        _report = report;
        _simulation = simulation;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AuditorDashboardDto>>> GetDashboard(CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<AuditorDashboardDto>(traceId);

        var dto = await _workspace.GetDashboardAsync(orgId, _userContext.HospitalId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AuditorSessionSummaryDto>>>> GetSessions(CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<IReadOnlyList<AuditorSessionSummaryDto>>(traceId);

        var list = await _workspace.GetSessionsAsync(orgId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, list, traceId));
    }

    [HttpPost("sessions")]
    [Authorize(Roles = ApplicationRoles.EXTERNAL_AUDITOR + "," + ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
    public async Task<ActionResult<ApiResponse<AuditSessionDetailDto>>> CreateSession(
        [FromBody] CreateAuditSessionRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<AuditSessionDetailDto>(traceId);

        try
        {
            var dto = await _workspace.CreateSessionAsync(orgId, _userContext.HospitalId, _userContext.UserId, request, ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit session");
            var r = ApiResponseFactory.Failure<AuditSessionDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return BadRequest(r);
        }
    }

    [HttpGet("session/{id:guid}")]
    public async Task<ActionResult<ApiResponse<AuditSessionDetailDto>>> GetSession(Guid id, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<AuditSessionDetailDto>(traceId);

        var dto = await _workspace.GetSessionByIdAsync(orgId, id, ct);
        if (dto == null)
        {
            var r = ApiResponseFactory.Failure<AuditSessionDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return NotFound(r);
        }

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("sessions/{id:guid}/close")]
    [Authorize(Roles = ApplicationRoles.EXTERNAL_AUDITOR + "," + ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
    public async Task<ActionResult<ApiResponse<AuditSessionDetailDto>>> CloseSession(
        Guid id, [FromBody] CloseAuditSessionRequest? request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<AuditSessionDetailDto>(traceId);

        try
        {
            var dto = await _workspace.CloseSessionAsync(orgId, id, _userContext.UserId, request ?? new CloseAuditSessionRequest(), ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ApiResponseFactory.Failure<AuditSessionDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Session close blocked for {SessionId}", id);
            return BadRequest(ApiResponseFactory.Failure<AuditSessionDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpPost("simulation")]
    public async Task<ActionResult<ApiResponse<AuditSimulationResultDto>>> RunSimulation(
        [FromBody] AuditSimulationRequestDto? request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<AuditSimulationResultDto>(traceId);

        var hospitalId = request?.HospitalId is { } h && h != Guid.Empty ? h : _userContext.HospitalId;
        var dto = await _simulation.SimulateAsync(orgId, hospitalId, request?.FrameworkId, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("observations")]
    [Authorize(Roles = ApplicationRoles.EXTERNAL_AUDITOR + "," + ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
    public async Task<ActionResult<ApiResponse<AuditObservationListItemDto>>> CreateObservation(
        [FromBody] CreateAuditObservationRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<AuditObservationListItemDto>(traceId);

        try
        {
            var dto = await _workspace.CreateObservationAsync(orgId, _userContext.UserId, null, request, ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Observation creation failed");
            return NotFound(ApiResponseFactory.Failure<AuditObservationListItemDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpGet("traceability/{objectiveElementId:guid}")]
    public async Task<ActionResult<ApiResponse<EvidenceTraceabilityDto>>> GetTraceability(
        Guid objectiveElementId, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<EvidenceTraceabilityDto>(traceId);

        var dto = await _traceability.GetTraceabilityAsync(orgId, _userContext.HospitalId, objectiveElementId, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<EvidenceTraceabilityDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpPost("comments")]
    public async Task<ActionResult<ApiResponse<AuditCommentDto>>> CreateComment(
        [FromBody] CreateAuditCommentRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<AuditCommentDto>(traceId);

        try
        {
            var isAuditor = User.IsInRole(ApplicationRoles.EXTERNAL_AUDITOR);
            var dto = await _workspace.CreateCommentAsync(orgId, _userContext.UserId, null, isAuditor, request, ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (InvalidOperationException)
        {
            return NotFound(ApiResponseFactory.Failure<AuditCommentDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        }
    }

    [HttpGet("report/{sessionId:guid}")]
    public async Task<ActionResult<ApiResponse<AuditorSessionReportDto>>> GetReport(Guid sessionId, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<AuditorSessionReportDto>(traceId);

        var dto = await _report.GenerateReportAsync(orgId, _userContext.HospitalId, sessionId, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<AuditorSessionReportDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    [HttpGet("assessment-context")]
    public async Task<ActionResult<ApiResponse<AuditorAssessmentContextDto>>> GetAssessmentContext(
        [FromQuery] Guid? frameworkId, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<AuditorAssessmentContextDto>(traceId);

        var dto = await _workspace.GetAssessmentContextAsync(orgId, _userContext.HospitalId, frameworkId, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<AuditorAssessmentContextDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }

    private ActionResult<ApiResponse<T>> OrgMissing<T>(string traceId)
    {
        var r = ApiResponseFactory.Failure<T>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
        var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
        return StatusCode(m.HttpStatus, r);
    }
}
