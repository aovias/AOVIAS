using ComplianceService.Application.Contracts;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1.Copilot;

/// <summary>Sprint 13F — read-only Copilot observability and effectiveness analytics.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/copilot/analytics")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class CopilotAnalyticsController : ControllerBase
{
    private readonly ICopilotAnalyticsService _analytics;
    private readonly IRecommendationImpactAnalysisService _impact;
    private readonly IUserContextService _userContext;

    public CopilotAnalyticsController(
        ICopilotAnalyticsService analytics,
        IRecommendationImpactAnalysisService impact,
        IUserContextService userContext)
    {
        _analytics   = analytics;
        _impact      = impact;
        _userContext = userContext;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<CopilotGovernanceDashboardDto>>> GetOverview(CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<CopilotGovernanceDashboardDto>(requestId);

        var data = await _analytics.GetGovernanceDashboardAsync(orgId, ct);
        return Success(data, requestId);
    }

    [HttpGet("recommendations")]
    public async Task<ActionResult<ApiResponse<RecommendationEffectivenessMetricsDto>>> GetRecommendations(CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<RecommendationEffectivenessMetricsDto>(requestId);

        var data = await _analytics.GetRecommendationMetricsAsync(orgId, ct);
        return Success(data, requestId);
    }

    [HttpGet("drafts")]
    public async Task<ActionResult<ApiResponse<DraftEffectivenessMetricsDto>>> GetDrafts(CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<DraftEffectivenessMetricsDto>(requestId);

        var data = await _analytics.GetDraftMetricsAsync(orgId, ct);
        return Success(data, requestId);
    }

    [HttpGet("proposals")]
    public async Task<ActionResult<ApiResponse<ProposalConversionMetricsDto>>> GetProposals(CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<ProposalConversionMetricsDto>(requestId);

        var data = await _analytics.GetProposalMetricsAsync(orgId, ct);
        return Success(data, requestId);
    }

    [HttpGet("personas")]
    public async Task<ActionResult<ApiResponse<PersonaAdoptionMetricsDto>>> GetPersonas(CancellationToken ct)
    {
        var requestId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<PersonaAdoptionMetricsDto>(requestId);

        var data = await _analytics.GetPersonaMetricsAsync(orgId, ct);
        return Success(data, requestId);
    }

    [HttpGet("trends")]
    public async Task<ActionResult<ApiResponse<CopilotTrendsDto>>> GetTrends(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<CopilotTrendsDto>(requestId);

        var data = await _analytics.GetTrendsAsync(orgId, days, ct);
        return Success(data, requestId);
    }

    [HttpGet("impact")]
    public async Task<ActionResult<ApiResponse<RecommendationImpactSummaryDto>>> GetImpact(
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) return OrgMissing<RecommendationImpactSummaryDto>(requestId);

        var data = await _impact.AnalyzeAsync(orgId, limit, ct);
        return Success(data, requestId);
    }

    private ActionResult<ApiResponse<T>> Success<T>(T data, string requestId)
    {
        var meta = ApiResponseCodes.Registry[ApiResponseCode.REPORT_GENERATED];
        return StatusCode(meta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, data, requestId));
    }

    private ActionResult<ApiResponse<T>> OrgMissing<T>(string requestId)
    {
        var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
        return StatusCode(meta.HttpStatus,
            ApiResponseFactory.Failure<T>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
    }
}
