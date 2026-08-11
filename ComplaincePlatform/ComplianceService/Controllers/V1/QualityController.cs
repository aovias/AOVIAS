using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>Sprint 17 — Clinical Indicators &amp; Quality Performance Intelligence.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/quality")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class QualityController : ControllerBase
{
    private readonly IClinicalIndicatorQueryService _query;
    private readonly IClinicalIndicatorCommandService _commands;
    private readonly IUserContextService _userContext;
    private readonly ILogger<QualityController> _logger;

    public QualityController(
        IClinicalIndicatorQueryService query,
        IClinicalIndicatorCommandService commands,
        IUserContextService userContext,
        ILogger<QualityController> logger)
    {
        _query = query;
        _commands = commands;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<QualityDashboardDto>>> GetDashboard(CancellationToken ct)
        => await Wrap(() => _query.GetDashboardAsync(RequireOrg(), _userContext.HospitalId, ct));

    [HttpGet("indicators")]
    public async Task<ActionResult<ApiResponse<ClinicalIndicatorListDto>>> GetIndicators(
        [FromQuery] string? category, [FromQuery] string? department, [FromQuery] string? thresholdStatus,
        [FromQuery] string? trend, [FromQuery] string? search, [FromQuery] string? sortBy, [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => await Wrap(() => _query.GetIndicatorsAsync(
            RequireOrg(), category, department, thresholdStatus, trend, search, sortBy, sortDesc, page, pageSize, ct));

    [HttpGet("indicators/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClinicalIndicatorDetailDto>>> GetIndicator(Guid id, CancellationToken ct)
    {
        var dto = await _query.GetByIdAsync(RequireOrg(), id, ct);
        if (dto == null) return NotFoundResponse<ClinicalIndicatorDetailDto>();
        return Success(dto);
    }

    [HttpPatch("thresholds")]
    public async Task<ActionResult<ApiResponse<ClinicalIndicatorDetailDto>>> PatchThresholds(
        [FromBody] PatchIndicatorThresholdsRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<ClinicalIndicatorDetailDto>();
        try
        {
            var dto = await _commands.PatchThresholdsAsync(RequireOrg(), _userContext.UserId, request, ct);
            return Success(dto);
        }
        catch (InvalidOperationException ex) { return ValidationFailed<ClinicalIndicatorDetailDto>(ex.Message); }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to patch thresholds for indicator {IndicatorId}", request.IndicatorId);
            return StatusCode(500, ApiResponseFactory.Failure<ClinicalIndicatorDetailDto>(ApiResponseCode.INTERNAL_ERROR, HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("scorecards")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentScorecardDto>>>> GetScorecards(CancellationToken ct)
        => await Wrap(() => _query.GetScorecardsAsync(RequireOrg(), ct));

    [HttpGet("trends")]
    public async Task<ActionResult<ApiResponse<QualityTrendsDto>>> GetTrends(
        [FromQuery] string? period, [FromQuery] Guid? indicatorId, CancellationToken ct)
        => await Wrap(() => _query.GetTrendsAsync(RequireOrg(), period, indicatorId, ct));

    [HttpGet("benchmarking")]
    public async Task<ActionResult<ApiResponse<QualityBenchmarkingDto>>> GetBenchmarking(
        [FromQuery] string? comparisonPeriod, CancellationToken ct)
        => await Wrap(() => _query.GetBenchmarkingAsync(RequireOrg(), comparisonPeriod, ct));

    [HttpGet("executive")]
    public async Task<ActionResult<ApiResponse<QualityExecutiveDto>>> GetExecutive(CancellationToken ct)
        => await Wrap(() => _query.GetExecutiveAsync(RequireOrg(), ct));

    [HttpGet("analytics")]
    public async Task<ActionResult<ApiResponse<QualityAnalyticsDto>>> GetAnalytics(CancellationToken ct)
        => await Wrap(() => _query.GetAnalyticsAsync(RequireOrg(), _userContext.HospitalId, ct));

    private Guid RequireOrg()
    {
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty) throw new UnauthorizedAccessException();
        return orgId;
    }

    private async Task<ActionResult<ApiResponse<T>>> Wrap<T>(Func<Task<T>> action)
    {
        try
        {
            return Success(await action());
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(401, ApiResponseFactory.Failure<T>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quality Intelligence API error");
            return StatusCode(500, ApiResponseFactory.Failure<T>(ApiResponseCode.INTERNAL_ERROR, HttpContext.TraceIdentifier));
        }
    }

    private ActionResult<ApiResponse<T>> Success<T>(T data)
    {
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier);
        return Ok(response);
    }

    private ActionResult<ApiResponse<T>> NotFoundResponse<T>(string? message = null)
    {
        var errors = message == null ? null : new List<ApiError> { new() { Message = message } };
        return StatusCode(404, ApiResponseFactory.Failure<T>(ApiResponseCode.RESOURCE_NOT_FOUND, errors, HttpContext.TraceIdentifier));
    }

    private ActionResult<ApiResponse<T>> ValidationFailed<T>(string? message = null)
    {
        var errors = message == null ? null : new List<ApiError> { new() { Message = message } };
        return StatusCode(400, ApiResponseFactory.Failure<T>(ApiResponseCode.VALIDATION_FAILED, errors, HttpContext.TraceIdentifier));
    }
}
