using ComplianceService.Application.Dashboard;
using ComplianceService.Application.Dashboard.Summary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Platform.Dashboard.Models;
using Platform.Dashboard.Registry;
using Platform.Dashboard.Services;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/dashboard")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class PlatformDashboardController : ControllerBase
{
    private readonly IDashboardSummaryService _summaryService;
    private readonly IDashboardProviderRegistry _registry;
    private readonly DashboardContextBuilder _contextBuilder;
    private readonly IWebHostEnvironment _environment;
    private readonly DashboardSummaryOptions _summaryOptions;
    private readonly ILogger<PlatformDashboardController> _logger;

    public PlatformDashboardController(
        IDashboardSummaryService summaryService,
        IDashboardProviderRegistry registry,
        DashboardContextBuilder contextBuilder,
        IWebHostEnvironment environment,
        IOptions<DashboardSummaryOptions> summaryOptions,
        ILogger<PlatformDashboardController> logger)
    {
        _summaryService = summaryService;
        _registry = registry;
        _contextBuilder = contextBuilder;
        _environment = environment;
        _summaryOptions = summaryOptions.Value;
        _logger = logger;
    }

    [HttpGet("experience")]
    public async Task<ActionResult<ApiResponse<DashboardExperienceResult>>> GetExperience(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] bool forceRefresh = false,
        CancellationToken ct = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var context = _contextBuilder.Build(hospitalId, frameworkId, forceRefresh);

        if (context.OrganizationId == Guid.Empty)
        {
            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            var response = ApiResponseFactory.Failure<DashboardExperienceResult>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                requestId);
            return StatusCode(meta.HttpStatus, response);
        }

        _logger.LogInformation(
            "Building platform dashboard experience for OrganizationId={OrganizationId}, HospitalId={HospitalId}, ForceRefresh={ForceRefresh}",
            context.OrganizationId,
            context.HospitalId,
            forceRefresh);

        var summary = await _summaryService.GetSummaryAsync(context, ct);
        ApplySummaryTelemetryHeader();

        var widgets = _registry.GetWidgets(context);
        var sections = _registry.GetSections(context);

        var experience = new DashboardExperienceResult
        {
            Summary = summary,
            Widgets = widgets,
            Sections = sections,
        };

        var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, experience, requestId);
        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, success);
    }

    private void ApplySummaryTelemetryHeader()
    {
        if (!_summaryOptions.ExposeTimingHeaders && !_environment.IsDevelopment())
        {
            return;
        }

        if (HttpContext.Items.TryGetValue(DashboardSummaryTelemetryKeys.HttpContextItemKey, out var value)
            && value is DashboardSummaryTelemetry telemetry)
        {
            Response.Headers["X-Dashboard-Summary-Timing"] = telemetry.ToCompactJson();
        }
    }
}
