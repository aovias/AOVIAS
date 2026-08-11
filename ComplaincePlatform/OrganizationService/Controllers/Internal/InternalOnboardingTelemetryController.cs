using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Application.Services;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;

namespace OrganizationService.Controllers.Internal;

[ApiController]
[Route("internal/onboarding")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public sealed class InternalOnboardingTelemetryController : ControllerBase
{
    private readonly OnboardingTelemetryAppService _telemetry;
    private readonly ILogger<InternalOnboardingTelemetryController> _logger;

    public InternalOnboardingTelemetryController(
        OnboardingTelemetryAppService telemetry,
        ILogger<InternalOnboardingTelemetryController> logger)
    {
        _telemetry = telemetry;
        _logger = logger;
    }

    public record RecordRequest(
        Guid OrganizationId,
        Guid? UserId,
        Guid? HospitalId,
        string EventType,
        string? Stage,
        string? CorrelationId,
        string? PayloadJson);

    [HttpPost("events")]
    public async Task<IActionResult> Record([FromBody] RecordRequest request, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var entity = await _telemetry.RecordAsync(
                request.OrganizationId,
                request.UserId,
                request.HospitalId,
                request.EventType,
                request.Stage,
                request.CorrelationId ?? traceId,
                request.PayloadJson,
                cancellationToken);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)new { entity.Id, entity.EventType, entity.OccurredAtUtc },
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Onboarding telemetry rejected");
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, failure);
        }
    }
}
