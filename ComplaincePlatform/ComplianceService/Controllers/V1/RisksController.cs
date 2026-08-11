using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Phase 4 Sprint 4 — Enterprise Risk Register CRUD + matrix/heatmap.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/risks")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class RisksController : ControllerBase
{
    private readonly IRiskRegisterAppService _risks;
    private readonly IUserContextService _userContext;
    private readonly ILogger<RisksController> _logger;

    public RisksController(
        IRiskRegisterAppService risks,
        IUserContextService userContext,
        ILogger<RisksController> logger)
    {
        _risks = risks;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<RiskListDto>>> List(
        [FromQuery] string? status,
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] Guid? ownerUserId,
        [FromQuery] Guid? departmentId,
        [FromQuery] bool? reviewDueOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => await Wrap(() => _risks.ListAsync(
            RequireOrg(),
            _userContext.HospitalId,
            status,
            category,
            search,
            ownerUserId,
            departmentId,
            reviewDueOnly,
            page,
            pageSize,
            ct));

    [HttpGet("matrix")]
    public async Task<ActionResult<ApiResponse<RiskMatrixDto>>> Matrix(
        [FromQuery] bool useResidual = false,
        CancellationToken ct = default)
        => await Wrap(() => _risks.GetMatrixAsync(RequireOrg(), _userContext.HospitalId, useResidual, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RiskDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var dto = await _risks.GetByIdAsync(RequireOrg(), id, ct);
        if (dto == null) return NotFoundResponse<RiskDetailDto>();
        return Success(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RiskDetailDto>>> Create(
        [FromBody] CreateRiskRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<RiskDetailDto>();
        try
        {
            var hospitalId = _userContext.HospitalId;
            if (hospitalId == Guid.Empty)
                return ValidationFailed<RiskDetailDto>("Hospital context is required.");

            var dto = await _risks.CreateAsync(RequireOrg(), hospitalId, _userContext.UserId, request, ct);
            return Created(dto);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return ValidationFailed<RiskDetailDto>(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RiskDetailDto>>> Update(
        Guid id, [FromBody] UpdateRiskRequestDto? request, CancellationToken ct)
    {
        if (request == null) return ValidationFailed<RiskDetailDto>();
        try
        {
            var dto = await _risks.UpdateAsync(RequireOrg(), id, _userContext.UserId, request, ct);
            return Success(dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFoundResponse<RiskDetailDto>(ex.Message);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return ValidationFailed<RiskDetailDto>(ex.Message);
        }
    }

    [HttpPatch("{id:guid}")]
    public Task<ActionResult<ApiResponse<RiskDetailDto>>> Patch(
        Guid id, [FromBody] UpdateRiskRequestDto? request, CancellationToken ct)
        => Update(id, request, ct);

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
            _logger.LogError(ex, "Risks API error");
            return StatusCode(500, ApiResponseFactory.Failure<T>(ApiResponseCode.INTERNAL_ERROR, HttpContext.TraceIdentifier));
        }
    }

    private ActionResult<ApiResponse<T>> Success<T>(T data)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));

    private ActionResult<ApiResponse<T>> Created<T>(T data)
        => StatusCode(StatusCodes.Status201Created, ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));

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
