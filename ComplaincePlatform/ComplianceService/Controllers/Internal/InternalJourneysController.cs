using ComplianceService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.Internal;

/// <summary>
/// Internal-only controller for lifecycle automation endpoints.
/// These endpoints are used by background jobs (Hangfire) and must not be exposed to public clients.
/// </summary>
[ApiController]
[Route("internal/journeys")]
[Authorize(Policy = "InternalOnly")] // Requires special internal-only policy
public class InternalJourneysController : ControllerBase
{
    private readonly ComplianceJourneyService _journeyService;
    private readonly ILogger<InternalJourneysController> _logger;

    public InternalJourneysController(
        ComplianceJourneyService journeyService,
        ILogger<InternalJourneysController> logger)
    {
        _journeyService = journeyService;
        _logger = logger;
    }

    /// <summary>
    /// Automatically expires a compliance journey that has reached its expiry date.
    /// Used by ComplianceExpiryJob (Hangfire) to transition Approved journeys to Expired state.
    /// 
    /// Business Rules:
    /// - Only allowed if current status == Approved
    /// - Only allowed if ExpiryDate <= Today (UTC)
    /// - Idempotent: If already Expired, returns 200 OK (no-op)
    /// </summary>
    [HttpPost("{journeyId:guid}/expire")]
    public async Task<ActionResult<ApiResponse<object>>> AutoExpire(
        Guid journeyId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (journeyId == Guid.Empty)
        {
            var error = ApiError.ValidationError(
                "JourneyId is required.",
                new Dictionary<string, string[]>
                {
                    [nameof(journeyId)] = new[] { "JourneyId cannot be empty." }
                });

            return BadRequest(ApiResponse<object>.ErrorResponse(error, traceId));
        }

        try
        {
            await _journeyService.AutoExpireJourneyAsync(journeyId, ct);

            _logger.LogInformation(
                "Successfully auto-expired journey {JourneyId} via internal endpoint",
                journeyId);

            var data = new
            {
                JourneyId = journeyId,
                Action = "Expired",
                ExpiredAt = DateTime.UtcNow
            };

            return Ok(ApiResponse<object>.SuccessResponse(data, traceId));
        }
        catch (InvalidOperationException ex)
        {
            // Business rule violation (e.g., not Approved, expiry date not reached, already expired)
            _logger.LogWarning(
                ex,
                "Business rule violation while auto-expiring journey {JourneyId}: {Message}",
                journeyId,
                ex.Message);

            // If already expired, return 200 OK (idempotent)
            if (ex.Message.Contains("already Expired", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("already in Expired state", StringComparison.OrdinalIgnoreCase))
            {
                var data = new
                {
                    JourneyId = journeyId,
                    Action = "NoOp",
                    Reason = "Journey already expired"
                };

                return Ok(ApiResponse<object>.SuccessResponse(data, traceId));
            }

            var error = ApiError.ValidationError(
                ex.Message,
                new Dictionary<string, string[]>());

            return BadRequest(ApiResponse<object>.ErrorResponse(error, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while auto-expiring journey {JourneyId}",
                journeyId);

            var error = ApiError.InternalServerError(
                $"AUTO_EXPIRE_FAILED: {ex.Message}");

            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(error, traceId));
        }
    }

    /// <summary>
    /// Opens the renewal window for a compliance journey that has reached its renewal window start date.
    /// Used by ComplianceRenewalWindowJob (Hangfire) to transition Approved journeys to RenewalInProgress state.
    /// 
    /// Business Rules:
    /// - Only allowed if current status == Approved
    /// - Only allowed if Today >= RenewalWindowStartDate
    /// - Idempotent: If already RenewalInProgress, returns 200 OK (no-op)
    /// - Unlocks the journey (IsLocked = false) to allow updates for renewal
    /// </summary>
    [HttpPost("{journeyId:guid}/renewal-open")]
    public async Task<ActionResult<ApiResponse<object>>> OpenRenewalWindow(
        Guid journeyId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (journeyId == Guid.Empty)
        {
            var error = ApiError.ValidationError(
                "JourneyId is required.",
                new Dictionary<string, string[]>
                {
                    [nameof(journeyId)] = new[] { "JourneyId cannot be empty." }
                });

            return BadRequest(ApiResponse<object>.ErrorResponse(error, traceId));
        }

        try
        {
            await _journeyService.OpenRenewalWindowAsync(journeyId, ct);

            _logger.LogInformation(
                "Successfully opened renewal window for journey {JourneyId} via internal endpoint",
                journeyId);

            var data = new
            {
                JourneyId = journeyId,
                Action = "RenewalWindowOpened",
                OpenedAt = DateTime.UtcNow
            };

            return Ok(ApiResponse<object>.SuccessResponse(data, traceId));
        }
        catch (InvalidOperationException ex)
        {
            // Business rule violation (e.g., not Approved, renewal window not reached, already RenewalInProgress)
            _logger.LogWarning(
                ex,
                "Business rule violation while opening renewal window for journey {JourneyId}: {Message}",
                journeyId,
                ex.Message);

            // If already RenewalInProgress, return 200 OK (idempotent)
            if (ex.Message.Contains("already in RenewalInProgress", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("already RenewalInProgress", StringComparison.OrdinalIgnoreCase))
            {
                var data = new
                {
                    JourneyId = journeyId,
                    Action = "NoOp",
                    Reason = "Journey already in renewal window"
                };

                return Ok(ApiResponse<object>.SuccessResponse(data, traceId));
            }

            var error = ApiError.ValidationError(
                ex.Message,
                new Dictionary<string, string[]>());

            return BadRequest(ApiResponse<object>.ErrorResponse(error, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while opening renewal window for journey {JourneyId}",
                journeyId);

            var error = ApiError.InternalServerError(
                $"OPEN_RENEWAL_WINDOW_FAILED: {ex.Message}");

            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(error, traceId));
        }
    }
}
