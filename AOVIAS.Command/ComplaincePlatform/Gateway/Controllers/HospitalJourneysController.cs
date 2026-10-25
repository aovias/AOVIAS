using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Gateway controller for hospital-level journey operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hospitals")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class HospitalJourneysController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<HospitalJourneysController> _logger;

    public HospitalJourneysController(
        IMediator mediator,
        ILogger<HospitalJourneysController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets journey summary for a hospital, optionally filtered by framework ID.
    /// Uses MediatR to orchestrate the query.
    /// </summary>
    [HttpGet("{hospitalId:guid}/journey-summary")]
    public async Task<IActionResult> GetJourneySummary(
        Guid hospitalId,
        [FromQuery] Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty)
        {
            return BadRequest(new { error = "HospitalId is required" });
        }

        // Extract hospital_id from JWT token for validation
        var hospitalIdClaim = User.FindFirst("hospital_id")?.Value;
        if (string.IsNullOrEmpty(hospitalIdClaim) || !Guid.TryParse(hospitalIdClaim, out var tokenHospitalId))
        {
            _logger.LogWarning("Missing or invalid hospital_id claim in JWT token");
            return Unauthorized(new { error = "Missing or invalid hospital_id claim in token" });
        }

        // Validate that the requested hospital ID matches the token's hospital ID
        if (hospitalId != tokenHospitalId)
        {
            _logger.LogWarning(
                "Hospital ID mismatch: requested {RequestedHospitalId} does not match token {TokenHospitalId}",
                hospitalId, tokenHospitalId);
            return Forbid();
        }

        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation(
            "GET /api/v{Version}/hospitals/{HospitalId}/journey-summary - Dispatching GetJourneySummaryQuery",
            apiVersion, hospitalId);

        try
        {
            var query = new GetJourneySummaryQuery(hospitalId, frameworkId, apiVersion);
            var summary = await _mediator.Send(query, cancellationToken);

            // Handler now always returns a JourneySummaryDto (never null), even if all values are zero
            // This represents "no journeys found" rather than an error condition
            return Ok(summary);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to journey summary for hospital {HospitalId}", hospitalId);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error in GetJourneySummary for hospital {HospitalId}, framework {FrameworkId}. Exception: {ExceptionType}, Message: {Message}",
                hospitalId, frameworkId, ex.GetType().Name, ex.Message);

            // Return more details in development
            var errorMessage = "Internal server error";
            var env = HttpContext.RequestServices.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>();
            if (env?.IsDevelopment() == true)
            {
                errorMessage = $"{ex.GetType().Name}: {ex.Message}";
            }

            return StatusCode(500, new { error = errorMessage });
        }
    }
}

