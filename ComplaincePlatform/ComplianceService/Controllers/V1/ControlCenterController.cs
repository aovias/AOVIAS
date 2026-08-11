using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs.ControlCenter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Compliance Command Center API. Exposes an aggregate <c>overview</c> snapshot (used by page load
/// and the 30s auto-refresh) plus granular per-module endpoints. All widgets derive from real
/// compliance, audit, CAPA, evidence, task and framework data.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-center/compliance")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class ControlCenterController : ControllerBase
{
    private readonly IControlCenterService _service;
    private readonly ILogger<ControlCenterController> _logger;

    public ControlCenterController(IControlCenterService service, ILogger<ControlCenterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("overview")]
    public Task<ActionResult<ApiResponse<ControlCenterOverviewDto>>> GetOverview(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetOverviewAsync(hospitalId, frameworkId, ct));

    [HttpGet("header")]
    public Task<ActionResult<ApiResponse<ComplianceHeaderDto>>> GetHeader(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetHeaderAsync(hospitalId, frameworkId, ct));

    [HttpGet("intelligence")]
    public Task<ActionResult<ApiResponse<ComplianceIntelligenceDto>>> GetIntelligence(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetIntelligenceAsync(hospitalId, frameworkId, ct));

    [HttpGet("actions")]
    public Task<ActionResult<ApiResponse<List<RecommendedActionDto>>>> GetActions(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetRecommendedActionsAsync(hospitalId, frameworkId, ct));

    [HttpGet("kpis")]
    public Task<ActionResult<ApiResponse<ComplianceCommandCenterKpiDto>>> GetKpis(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetKpisAsync(hospitalId, frameworkId, ct));

    [HttpGet("high-risk-chapters")]
    public Task<ActionResult<ApiResponse<List<HighRiskChapterDto>>>> GetHighRiskChapters(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetHighRiskChaptersAsync(hospitalId, frameworkId, ct));

    [HttpGet("department-risk")]
    public Task<ActionResult<ApiResponse<List<DepartmentRiskDto>>>> GetDepartmentRisk(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetDepartmentRiskAsync(hospitalId, frameworkId, ct));

    [HttpGet("action-center")]
    public Task<ActionResult<ApiResponse<List<ActionCenterItemDto>>>> GetActionCenter(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetActionCenterAsync(hospitalId, frameworkId, ct));

    [HttpGet("trend")]
    public Task<ActionResult<ApiResponse<List<ComplianceTrendPointDto>>>> GetTrend(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetTrendAsync(hospitalId, frameworkId, ct));

    [HttpGet("chapter-compliance")]
    public Task<ActionResult<ApiResponse<List<ChapterComplianceDto>>>> GetChapterCompliance(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetChapterComplianceAsync(hospitalId, frameworkId, ct));

    [HttpGet("evidence-coverage")]
    public Task<ActionResult<ApiResponse<EvidenceCoverageDto>>> GetEvidenceCoverage(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetEvidenceCoverageAsync(hospitalId, frameworkId, ct));

    [HttpGet("audit-readiness")]
    public Task<ActionResult<ApiResponse<AuditReadinessDto>>> GetAuditReadiness(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetAuditReadinessAsync(hospitalId, frameworkId, ct));

    [HttpGet("activity-stream")]
    public Task<ActionResult<ApiResponse<List<ActivityEventDto>>>> GetActivityStream(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, [FromQuery] string? type, CancellationToken ct)
        => Execute(() => _service.GetActivityStreamAsync(hospitalId, frameworkId, type, ct));

    [HttpGet("alerts")]
    public Task<ActionResult<ApiResponse<List<AlertDto>>>> GetAlerts(
        [FromQuery] Guid? hospitalId, [FromQuery] Guid? frameworkId, CancellationToken ct)
        => Execute(() => _service.GetAlertsAsync(hospitalId, frameworkId, ct));

    private async Task<ActionResult<ApiResponse<T>>> Execute<T>(Func<Task<T>> action)
    {
        var requestId = HttpContext.TraceIdentifier;
        try
        {
            var data = await action();
            var success = ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, data, requestId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
            return StatusCode(meta.HttpStatus, success);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Control Center request rejected: {Message}", ex.Message);
            var response = ApiResponseFactory.Failure<T>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, response);
        }
    }
}
