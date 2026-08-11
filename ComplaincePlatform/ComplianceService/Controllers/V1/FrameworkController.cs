using System.Security.Claims;
using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Extensions;
using AppFrameworkDto = ComplianceService.Application.DTOs.FrameworkDto;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Controller for Framework query operations.
/// Provides endpoints to retrieve available frameworks and hospital framework enrollments.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/frameworks")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class FrameworkController : ControllerBase
{
    private readonly IFrameworkQueryService _frameworkQueryService;
    private readonly EnrollHospitalInFrameworkCommandHandler _enrollmentHandler;
    private readonly FrameworkLifecycleService _frameworkLifecycle;
    private readonly ILogger<FrameworkController> _logger;

    public FrameworkController(
        IFrameworkQueryService frameworkQueryService,
        EnrollHospitalInFrameworkCommandHandler enrollmentHandler,
        FrameworkLifecycleService frameworkLifecycle,
        ILogger<FrameworkController> logger)
    {
        _frameworkQueryService = frameworkQueryService;
        _enrollmentHandler = enrollmentHandler;
        _frameworkLifecycle = frameworkLifecycle;
        _logger = logger;
    }

    /// <summary>
    /// Catalog listing with version/status timestamps. When includeInactive=false, only Active rows.
    /// </summary>
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(ApiResponse<List<AppFrameworkDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "GET /api/v1/frameworks/catalog includeInactive={IncludeInactive}",
            includeInactive);

        try
        {
            var catalog = await _frameworkLifecycle.ListCatalogAsync(includeInactive, cancellationToken);
            return this.OkResponse(catalog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving framework catalog");
            return this.BadRequestResponse("An error occurred while retrieving the framework catalog");
        }
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(ApiResponse<AppFrameworkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
        => await TransitionAsync(id, "publish", () => _frameworkLifecycle.PublishAsync(id, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<AppFrameworkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
        => await TransitionAsync(id, "activate", () => _frameworkLifecycle.ActivateAsync(id, cancellationToken));

    [HttpPost("{id:guid}/retire")]
    [ProducesResponseType(typeof(ApiResponse<AppFrameworkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Retire(Guid id, CancellationToken cancellationToken)
        => await TransitionAsync(id, "retire", () => _frameworkLifecycle.RetireAsync(id, cancellationToken));

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(ApiResponse<AppFrameworkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
        => await TransitionAsync(id, "archive", () => _frameworkLifecycle.ArchiveAsync(id, cancellationToken));

    private async Task<IActionResult> TransitionAsync(
        Guid id,
        string action,
        Func<Task<AppFrameworkDto>> execute)
    {
        if (id == Guid.Empty)
        {
            return this.BadRequestResponse("FrameworkId is required", "id", "FrameworkId cannot be empty");
        }

        try
        {
            var result = await execute();
            return this.OkResponse(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Framework {Action} rejected for {FrameworkId}", action, id);
            return this.BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during framework {Action} for {FrameworkId}", action, id);
            return this.BadRequestResponse($"An error occurred while performing {action}");
        }
    }

    /// <summary>
    /// Gets all available compliance frameworks.
    /// Returns only active frameworks.
    /// Data source: ComplianceFrameworks table (read model).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active frameworks</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FrameworkDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFrameworks(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/frameworks - Retrieving all active frameworks");

        try
        {
            var frameworks = await _frameworkQueryService.GetAllFrameworksAsync(cancellationToken);

            return this.OkResponse(frameworks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving frameworks");
            return this.BadRequestResponse("An error occurred while retrieving frameworks");
        }
    }

    /// <summary>
    /// Gets frameworks where the specified hospital is actively enrolled.
    /// Returns only Active/InProgress/Approved frameworks (excludes Expired).
    /// Data source: ComplianceSnapshot read model (Level 2).
    /// </summary>
    /// <param name="hospitalId">Hospital identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of hospital framework enrollments</returns>
    [HttpGet("/api/v{version:apiVersion}/hospitals/{hospitalId}/frameworks")]
    [ProducesResponseType(typeof(ApiResponse<List<HospitalFrameworkDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHospitalFrameworks(
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/frameworks/hospitals/{HospitalId}/frameworks - Retrieving frameworks for hospital", hospitalId);

        if (hospitalId == Guid.Empty)
        {
            return this.BadRequestResponse("HospitalId is required", "hospitalId", "HospitalId cannot be empty");
        }

        try
        {
            var frameworks = await _frameworkQueryService.GetHospitalFrameworksAsync(hospitalId, cancellationToken);

            return this.OkResponse(frameworks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving frameworks for hospital {HospitalId}", hospitalId);
            return this.BadRequestResponse("An error occurred while retrieving hospital frameworks");
        }
    }

    /// <summary>
    /// Enrolls a hospital in a compliance framework.
    /// Creates or rehydrates HospitalComplianceAggregate and raises FrameworkEnrolledDomainEvent.
    /// </summary>
    /// <param name="hospitalId">Hospital identifier</param>
    /// <param name="frameworkId">Framework identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enrollment status</returns>
    [HttpPost("/api/v{version:apiVersion}/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll")]
    [ProducesResponseType(typeof(ApiResponse<EnrollFrameworkResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EnrollHospitalInFramework(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "POST /api/v1/hospitals/{HospitalId}/frameworks/{FrameworkId}/enroll - Enrolling hospital in framework",
            hospitalId, frameworkId);

        if (hospitalId == Guid.Empty)
        {
            return this.BadRequestResponse("HospitalId is required", "hospitalId", "HospitalId cannot be empty");
        }

        if (frameworkId == Guid.Empty)
        {
            return this.BadRequestResponse("FrameworkId is required", "frameworkId", "FrameworkId cannot be empty");
        }

        // JWT inbound claim mapping may move "sub" to ClaimTypes.NameIdentifier — match IUserContextService.UserId
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var requestedByUserId))
        {
            return this.UnauthorizedResponse("User ID not found in token");
        }

        try
        {
            var command = new EnrollHospitalInFrameworkCommand(hospitalId, frameworkId, requestedByUserId);
            var result = await _enrollmentHandler.HandleAsync(command, cancellationToken);

            // Use version from route to construct location header
            var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
            var location = $"/api/v{apiVersion}/hospitals/{hospitalId}/frameworks/{frameworkId}";
            return this.CreatedResponse(location, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, 
                "Enrollment failed for HospitalId={HospitalId}, FrameworkId={FrameworkId}: {Message}",
                hospitalId, frameworkId, ex.Message);

            // Check if it's a duplicate enrollment (409 Conflict)
            if (ex.Message.Contains("already enrolled", StringComparison.OrdinalIgnoreCase))
            {
                return this.ConflictResponse(
                    "DUPLICATE_ENROLLMENT",
                    ex.Message);
            }

            return this.BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error enrolling HospitalId={HospitalId} in FrameworkId={FrameworkId}",
                hospitalId, frameworkId);
            return this.BadRequestResponse("An error occurred while enrolling hospital in framework");
        }
    }
}
