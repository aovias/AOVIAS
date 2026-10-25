using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfessionalService.Application.Training;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace ProfessionalService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/training/programs")]
[Authorize]
public class TrainingProgramsController : ControllerBase
{
    private readonly ITrainingAppService _training;

    public TrainingProgramsController(ITrainingAppService training) => _training = training;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrainingProgramDto>>>> List(
        [FromQuery] Guid? organizationId = null,
        [FromQuery] bool? mandatoryOnly = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _training.ListProgramsAsync(organizationId, mandatoryOnly, cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("matrix")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrainingMatrixItemDto>>>> Matrix(
        [FromQuery] Guid? organizationId = null,
        [FromQuery] string? roleCode = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _training.GetMatrixAsync(organizationId, roleCode, departmentId, cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TrainingProgramDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _training.GetProgramAsync(id, cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<TrainingProgramDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TrainingProgramDto>>> Create(
        [FromBody] CreateTrainingProgramRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.CreatedByUserId ??= ActorUserId();
        try
        {
            var data = await _training.CreateProgramAsync(request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException)
        {
            var failure = ApiResponseFactory.Failure<TrainingProgramDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus, failure);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TrainingProgramDto>>> Update(
        Guid id, [FromBody] UpdateTrainingProgramRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _training.UpdateProgramAsync(id, request, cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<TrainingProgramDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var deleted = await _training.DeleteProgramAsync(id, cancellationToken);
        if (!deleted) return NotFound(ApiResponseFactory.Failure<bool>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, true, traceId));
    }

    private Guid? ActorUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
