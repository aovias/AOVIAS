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

/// <summary>
/// Sprint 16 — Incident Management &amp; Patient Safety platform.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/incidents")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentQueryService _query;
    private readonly IIncidentCommandService _commands;
    private readonly IIncidentPromotionService _promotion;
    private readonly IUserContextService _userContext;
    private readonly ILogger<IncidentsController> _logger;

    public IncidentsController(
        IIncidentQueryService query,
        IIncidentCommandService commands,
        IIncidentPromotionService promotion,
        IUserContextService userContext,
        ILogger<IncidentsController> logger)
    {
        _query = query;
        _commands = commands;
        _promotion = promotion;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<IncidentDashboardDto>>> GetDashboard(CancellationToken ct)
        => await Wrap(() => _query.GetDashboardAsync(RequireOrg(), ct));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IncidentListDto>>> GetIncidents(
        [FromQuery] string? status, [FromQuery] string? severity, [FromQuery] string? category,
        [FromQuery] string? department, [FromQuery] string? incidentType, [FromQuery] string? quickFilter,
        [FromQuery] string? search, [FromQuery] string? sortBy, [FromQuery] bool sortDesc = true,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
        => await Wrap(() => _query.GetIncidentsAsync(
            RequireOrg(), status, severity, category, department, incidentType,
            quickFilter, search, sortBy, sortDesc, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<IncidentDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _query.GetByIdAsync(RequireOrg(), id, ct);
        if (dto == null) return NotFoundResponse<IncidentDetailDto>();
        return Success(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<IncidentDetailDto>>> Create(
        [FromBody] CreateIncidentRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<IncidentDetailDto>();
        try
        {
            var dto = await _commands.CreateAsync(RequireOrg(), _userContext.HospitalId, _userContext.UserId, request, ct);
            return Created(dto);
        }
        catch (ArgumentException ex) { return ValidationFailed<IncidentDetailDto>(ex.Message); }
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApiResponse<IncidentDetailDto>>> Patch(
        Guid id, [FromBody] PatchIncidentRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<IncidentDetailDto>();
        try
        {
            var dto = await _commands.PatchAsync(RequireOrg(), id, _userContext.UserId, request, ct);
            return Success(dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFoundResponse<IncidentDetailDto>(ex.Message);
        }
        catch (InvalidOperationException ex) { return ValidationFailed<IncidentDetailDto>(ex.Message); }
    }

    [HttpPost("investigation")]
    public async Task<ActionResult<ApiResponse<IncidentInvestigationDto>>> SaveInvestigation(
        [FromBody] SaveInvestigationRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<IncidentInvestigationDto>();
        try
        {
            var dto = await _commands.SaveInvestigationAsync(RequireOrg(), _userContext.UserId, request, ct);
            return Success(dto);
        }
        catch (InvalidOperationException ex) { return ValidationFailed<IncidentInvestigationDto>(ex.Message); }
    }

    [HttpPost("rca")]
    public async Task<ActionResult<ApiResponse<IncidentRcaDto>>> SaveRca(
        [FromBody] SaveRcaRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<IncidentRcaDto>();
        try
        {
            var dto = await _commands.SaveRcaAsync(RequireOrg(), _userContext.UserId, request, ct);
            return Success(dto);
        }
        catch (InvalidOperationException ex) { return ValidationFailed<IncidentRcaDto>(ex.Message); }
    }

    [HttpPost("promote-finding")]
    public async Task<ActionResult<ApiResponse<PromoteIncidentResultDto>>> PromoteFinding(
        [FromBody] PromoteIncidentFindingRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<PromoteIncidentResultDto>();
        try
        {
            var dto = await _promotion.PromoteToFindingAsync(
                RequireOrg(), _userContext.HospitalId, _userContext.UserId, request, ct);
            return Created(dto);
        }
        catch (InvalidOperationException ex) { return ValidationFailed<PromoteIncidentResultDto>(ex.Message); }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to promote incident {IncidentId} to finding", request.IncidentId);
            return StatusCode(500, ApiResponseFactory.Failure<PromoteIncidentResultDto>(ApiResponseCode.INTERNAL_ERROR, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("promote-deficiency")]
    public async Task<ActionResult<ApiResponse<PromoteIncidentResultDto>>> PromoteDeficiency(
        [FromBody] PromoteIncidentDeficiencyRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<PromoteIncidentResultDto>();
        try
        {
            var dto = await _promotion.PromoteToDeficiencyAsync(
                RequireOrg(), _userContext.HospitalId, _userContext.UserId, request, ct);
            return Created(dto);
        }
        catch (InvalidOperationException ex) { return ValidationFailed<PromoteIncidentResultDto>(ex.Message); }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to promote incident {IncidentId} to deficiency", request.IncidentId);
            return StatusCode(500, ApiResponseFactory.Failure<PromoteIncidentResultDto>(ApiResponseCode.INTERNAL_ERROR, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("lesson")]
    public async Task<ActionResult<ApiResponse<LessonLearnedDto>>> CreateLesson(
        [FromBody] CreateLessonLearnedRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<LessonLearnedDto>();
        try
        {
            var dto = await _commands.CreateLessonLearnedAsync(
                RequireOrg(), _userContext.HospitalId, _userContext.UserId, request, ct);
            return Created(dto);
        }
        catch (InvalidOperationException ex) { return ValidationFailed<LessonLearnedDto>(ex.Message); }
    }

    [HttpGet("feedback")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PatientFeedbackDto>>>> GetFeedback(CancellationToken ct)
        => await Wrap(() => _query.GetPatientFeedbackAsync(RequireOrg(), ct));

    [HttpPost("feedback")]
    public async Task<ActionResult<ApiResponse<PatientFeedbackDto>>> CreateFeedback(
        [FromBody] CreatePatientFeedbackRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<PatientFeedbackDto>();
        var dto = await _commands.CreatePatientFeedbackAsync(RequireOrg(), _userContext.HospitalId, request, ct);
        return Created(dto);
    }

    [HttpGet("lessons")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LessonLearnedDto>>>> GetLessons(
        [FromQuery] string? search, CancellationToken ct)
        => await Wrap(() => _query.GetLessonsLearnedAsync(RequireOrg(), search, ct));

    [HttpGet("analytics")]
    public async Task<ActionResult<ApiResponse<IncidentAnalyticsDto>>> GetAnalytics(CancellationToken ct)
        => await Wrap(() => _query.GetAnalyticsAsync(RequireOrg(), ct));

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
            _logger.LogError(ex, "Incidents API error");
            return StatusCode(500, ApiResponseFactory.Failure<T>(ApiResponseCode.INTERNAL_ERROR, HttpContext.TraceIdentifier));
        }
    }

    private ActionResult<ApiResponse<T>> Success<T>(T data)
    {
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier);
        return Ok(response);
    }

    private ActionResult<ApiResponse<T>> Created<T>(T data)
    {
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier);
        return StatusCode(StatusCodes.Status201Created, response);
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
