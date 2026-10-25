using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfessionalService.Application.Training;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace ProfessionalService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/training/sessions")]
[Authorize]
public class TrainingSessionsController : ControllerBase
{
    private readonly ITrainingAppService _training;

    public TrainingSessionsController(ITrainingAppService training) => _training = training;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrainingSessionDto>>>> List(
        [FromQuery] Guid? programId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _training.ListSessionsAsync(programId, from, to, cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TrainingSessionDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _training.GetSessionAsync(id, cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<TrainingSessionDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TrainingSessionDto>>> Create(
        [FromBody] CreateTrainingSessionRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        request.CreatedByUserId ??= ActorUserId();
        try
        {
            var data = await _training.CreateSessionAsync(request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException)
        {
            var failure = ApiResponseFactory.Failure<TrainingSessionDto>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus, failure);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TrainingSessionDto>>> Update(
        Guid id, [FromBody] UpdateTrainingSessionRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _training.UpdateSessionAsync(id, request, cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<TrainingSessionDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var deleted = await _training.DeleteSessionAsync(id, cancellationToken);
        if (!deleted) return NotFound(ApiResponseFactory.Failure<bool>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, true, traceId));
    }

    [HttpGet("{id:guid}/attendances")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrainingAttendanceDto>>>> ListAttendances(
        Guid id, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _training.ListAttendancesAsync(id, cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpPost("{id:guid}/participants")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrainingAttendanceDto>>>> AssignParticipants(
        Guid id, [FromBody] AssignParticipantsRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var data = await _training.AssignParticipantsAsync(id, request, cancellationToken);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (InvalidOperationException)
        {
            var failure = ApiResponseFactory.Failure<IReadOnlyList<TrainingAttendanceDto>>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }
    }

    [HttpPost("attendances/{attendanceId:guid}/record")]
    public async Task<ActionResult<ApiResponse<TrainingAttendanceDto>>> RecordAttendance(
        Guid attendanceId, [FromBody] RecordAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _training.RecordAttendanceAsync(attendanceId, request, cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<TrainingAttendanceDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpPost("attendances/{attendanceId:guid}/assessment")]
    public async Task<ActionResult<ApiResponse<TrainingAssessmentDto>>> RecordAssessment(
        Guid attendanceId, [FromBody] RecordAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var data = await _training.RecordAssessmentAsync(attendanceId, request, cancellationToken);
        if (data == null) return NotFound(ApiResponseFactory.Failure<TrainingAssessmentDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
    }

    [HttpPost("attendances/{attendanceId:guid}/certificate")]
    public async Task<ActionResult<ApiResponse<TrainingAttendanceDto>>> SetCertificate(
        Guid attendanceId, [FromBody] SetCertificateUrlRequest request, CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var data = await _training.SetCertificateUrlAsync(attendanceId, request, cancellationToken);
            if (data == null) return NotFound(ApiResponseFactory.Failure<TrainingAttendanceDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId));
        }
        catch (InvalidOperationException)
        {
            var failure = ApiResponseFactory.Failure<TrainingAttendanceDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, failure);
        }
    }

    private Guid? ActorUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
