using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using WorkforceIntelligenceService.Application.Queries;

namespace WorkforceIntelligenceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/intelligence")]
[Authorize]
public class IntelligenceController(IMediator mediator) : ControllerBase
{
    private string TraceId => HttpContext.TraceIdentifier;

    [HttpGet("dashboard")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.ExecutiveDashboardDto>>> Dashboard(
        [FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var data = await mediator.Send(new GetExecutiveDashboardQuery(organizationId, hospitalId, departmentId, asOf), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("headcount")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.HeadcountAnalyticsDto>>> Headcount(
        [FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var data = await mediator.Send(new GetHeadcountAnalyticsQuery(organizationId, hospitalId, departmentId, asOf), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("skills")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.SkillMatrixDto>>> Skills(
        [FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var data = await mediator.Send(new GetSkillMatrixQuery(organizationId, hospitalId, departmentId, asOf), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("competencies")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.CompetencyMatrixDto>>> Competencies(
        [FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var data = await mediator.Send(new GetCompetencyMatrixQuery(organizationId, hospitalId, departmentId, asOf), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("training")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.TrainingMatrixDto>>> Training(
        [FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var data = await mediator.Send(new GetTrainingMatrixQuery(organizationId, hospitalId, departmentId, asOf), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("licenses")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.LicenseDashboardDto>>> Licenses(
        [FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var data = await mediator.Send(new GetLicenseDashboardQuery(organizationId, hospitalId, departmentId, asOf), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("certifications")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.CertificationDashboardDto>>> Certifications(
        [FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, [FromQuery] Guid? departmentId, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var data = await mediator.Send(new GetCertificationDashboardQuery(organizationId, hospitalId, departmentId, asOf), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("forecast")]
    [RequirePermission("Intelligence.Forecast.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.ForecastDto>>> Forecast(
        [FromQuery] Guid organizationId, [FromQuery] string forecastType = "CertificationExpiry", CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetWorkforceForecastQuery(organizationId, forecastType), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("recommendations")]
    [RequirePermission("Intelligence.Recommendations.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.RecommendationListDto>>> Recommendations(
        [FromQuery] Guid organizationId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetWorkforceRecommendationsQuery(organizationId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("search")]
    [RequirePermission("Intelligence.Search.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.WorkforceSearchResultDto>>> Search(
        [FromQuery] Guid organizationId, [FromQuery] string query = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var data = await mediator.Send(new SearchWorkforceQuery(organizationId, query, page, pageSize), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("organization")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.OrganizationAnalyticsDto>>> Organization(
        [FromQuery] Guid organizationId, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var data = await mediator.Send(new GetOrganizationAnalyticsQuery(organizationId, asOf), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("responsibilities")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.ResponsibilityMatrixDto>>> Responsibilities(
        [FromQuery] Guid organizationId, [FromQuery] DateTime? asOf, CancellationToken ct)
    {
        var data = await mediator.Send(new GetResponsibilityMatrixQuery(organizationId, asOf), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("committees")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.CommitteeAnalyticsDto>>> Committees(
        [FromQuery] Guid organizationId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetCommitteeAnalyticsQuery(organizationId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpGet("availability")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<Application.Contracts.AvailabilityAnalyticsDto>>> Availability(
        [FromQuery] Guid organizationId, [FromQuery] Guid? hospitalId, CancellationToken ct)
    {
        var data = await mediator.Send(new GetAvailabilityAnalyticsQuery(organizationId, hospitalId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, TraceId));
    }

    [HttpPost("refresh")]
    [RequirePermission("Intelligence.Dashboard.Read")]
    public async Task<ActionResult<ApiResponse<int>>> Refresh([FromQuery] Guid organizationId, CancellationToken ct)
    {
        var count = await mediator.Send(new RefreshDashboardCommand(organizationId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, count, TraceId));
    }
}
