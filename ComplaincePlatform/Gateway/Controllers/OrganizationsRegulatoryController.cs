using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace Gateway.Controllers;

/// <summary>
/// Gateway controller for organization-level regulatory and framework enrollment operations.
/// All client calls for regulatory overview, accreditations, renewals, and risk recalculation
/// terminate here and are orchestrated via MediatR and proxied to ComplianceService.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organizations/{orgId:guid}")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[Authorize(Policy = "OrgAccess")]
public class OrganizationsRegulatoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrganizationsRegulatoryController> _logger;

    public OrganizationsRegulatoryController(
        IMediator mediator,
        ILogger<OrganizationsRegulatoryController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // NOTE: Organization access validation is now handled by the [Authorize(Policy = "OrgAccess")] policy
    // applied at the controller level. The policy automatically validates that:
    // 1. User has organization_id claim in JWT
    // 2. Route orgId matches user's organization_id claim
    // 3. SUPER_ADMIN can access any organization
    // This prevents multi-tenant data breaches without requiring manual validation in each endpoint.

    /// <summary>
    /// Gets aggregated regulatory overview for an organization including active accreditations,
    /// upcoming renewals, and risk index classification and trend.
    /// </summary>
    [HttpGet("regulatory-overview")]
    [Authorize(Roles = ApplicationRoles.Authorization.RegulatoryOverviewReaders)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.RegulatoryOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DTOs.RegulatoryOverviewDto>>> GetRegulatoryOverview(
        Guid orgId,
        CancellationToken cancellationToken)
    {
        // Organization access is validated by [Authorize(Policy = "OrgAccess")] policy at controller level
        // Get traceId for distributed tracing
        // This will be propagated as X-Correlation-Id header to downstream services via ProxyService
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "GET /api/v1/organizations/{OrgId}/regulatory-overview - Dispatching GetRegulatoryOverviewQuery, TraceId: {TraceId}",
            orgId, traceId);

        var query = new GetRegulatoryOverviewQuery(orgId, traceId);
        var response = await _mediator.Send(query, cancellationToken);

        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }

    /// <summary>
    /// Updates primary jurisdiction for an organization.
    /// </summary>
    [HttpPut("jurisdiction")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.UpdateJurisdictionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DTOs.UpdateJurisdictionResponseDto>>> UpdateJurisdiction(
        Guid orgId,
        [FromBody] DTOs.UpdateJurisdictionRequestDto request,
        CancellationToken cancellationToken)
    {
        // Organization access is validated by [Authorize(Policy = "OrgAccess")] policy at controller level

        // Get traceId for distributed tracing
        // This will be propagated as X-Correlation-Id header to downstream services via ProxyService
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "PUT /api/v1/organizations/{OrgId}/jurisdiction - Dispatching UpdateJurisdictionCommand, TraceId: {TraceId}",
            orgId, traceId);

        var command = new UpdateJurisdictionCommand(orgId, request, traceId);
        var response = await _mediator.Send(command, cancellationToken);

        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }

    /// <summary>
    /// Gets active accreditations for an organization.
    /// </summary>
    [HttpGet("accreditations")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DTOs.AccreditationSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DTOs.AccreditationSummaryDto>>>> GetAccreditations(
        Guid orgId,
        CancellationToken cancellationToken)
    {
        // Organization access is validated by [Authorize(Policy = "OrgAccess")] policy at controller level
        // Get traceId for distributed tracing
        // This will be propagated as X-Correlation-Id header to downstream services via ProxyService
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "GET /api/v1/organizations/{OrgId}/accreditations - Dispatching GetAccreditationsQuery, TraceId: {TraceId}",
            orgId, traceId);

        var query = new GetAccreditationsQuery(orgId, traceId);
        var response = await _mediator.Send(query, cancellationToken);

        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }

    /// <summary>
    /// Gets upcoming accreditation renewals for an organization.
    /// </summary>
    [HttpGet("renewals")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DTOs.RenewalDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DTOs.RenewalDto>>>> GetRenewals(
        Guid orgId,
        [FromQuery] int withinDays = 90,
        CancellationToken cancellationToken = default)
    {
        // Organization access is validated by [Authorize(Policy = "OrgAccess")] policy at controller level
        // Get traceId for distributed tracing
        // This will be propagated as X-Correlation-Id header to downstream services via ProxyService
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "GET /api/v1/organizations/{OrgId}/renewals?withinDays={WithinDays} - Dispatching GetRenewalsQuery, TraceId: {TraceId}",
            orgId, withinDays, traceId);

        var query = new GetRenewalsQuery(orgId, withinDays, traceId);
        var response = await _mediator.Send(query, cancellationToken);

        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }

    /// <summary>
    /// Gets framework catalog for the organization and jurisdiction filters.
    /// 
    /// PERFORMANCE PROTECTION:
    /// - Server-side pageSize cap: Maximum 100 items per page
    /// - Values exceeding 100 are automatically clamped to 100
    /// - This prevents performance degradation from large page requests
    /// </summary>
    [HttpGet("frameworks/catalog")]
    [Authorize(Roles = ApplicationRoles.Authorization.RegulatoryOverviewReaders)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.FrameworkCatalogPageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DTOs.FrameworkCatalogPageDto>>> GetFrameworkCatalog(
        Guid orgId,
        [FromQuery] string? country,
        [FromQuery] string? accreditationBody,
        [FromQuery] string? type,
        [FromQuery] string? version,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        // Organization access is validated by [Authorize(Policy = "OrgAccess")] policy at controller level

        // Server-side enforcement: Clamp pageSize to maximum 100 to prevent performance issues
        // This protects against:
        // - Validation bypass attempts
        // - Direct API calls with excessive pageSize values
        // - Performance degradation from large result sets
        const int MAX_PAGE_SIZE = 100;
        if (pageSize > MAX_PAGE_SIZE)
        {
            _logger.LogWarning(
                "GET /api/v1/organizations/{OrgId}/frameworks/catalog - pageSize {RequestedPageSize} exceeds maximum {MaxPageSize}, clamping to {MaxPageSize}",
                orgId, pageSize, MAX_PAGE_SIZE, MAX_PAGE_SIZE);
            pageSize = MAX_PAGE_SIZE;
        }

        // Ensure pageSize is at least 1
        if (pageSize < 1)
        {
            pageSize = 1;
        }

        // Ensure page is at least 1
        if (page < 1)
        {
            page = 1;
        }

        // Get traceId for distributed tracing
        // This will be propagated as X-Correlation-Id header to downstream services via ProxyService
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "GET /api/v1/organizations/{OrgId}/frameworks/catalog?page={Page}&pageSize={PageSize} - Dispatching GetFrameworkCatalogQuery, TraceId: {TraceId}",
            orgId, page, pageSize, traceId);

        var query = new GetFrameworkCatalogQuery(
            orgId,
            country,
            accreditationBody,
            type,
            version,
            page,
            pageSize,
            search,
            traceId);

        var response = await _mediator.Send(query, cancellationToken);

        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }

    /// <summary>
    /// Gets activation metadata for a framework for the organization.
    /// </summary>
    [HttpGet("frameworks/{frameworkId:guid}/activation-metadata")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminRegulatoryOrgAdmin)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.ActivationMetadataDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DTOs.ActivationMetadataDto>>> GetActivationMetadata(
        Guid orgId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        // Organization access is validated by [Authorize(Policy = "OrgAccess")] policy at controller level
        // Get traceId for distributed tracing
        // This will be propagated as X-Correlation-Id header to downstream services via ProxyService
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "GET /api/v1/organizations/{OrgId}/frameworks/{FrameworkId}/activation-metadata - Dispatching GetActivationMetadataQuery, TraceId: {TraceId}",
            orgId, frameworkId, traceId);

        var query = new GetActivationMetadataQuery(orgId, frameworkId, traceId);
        var response = await _mediator.Send(query, cancellationToken);

        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }

    /// <summary>
    /// Activates a new accreditation for the organization.
    /// Supports Idempotency-Key header to prevent duplicate activations on retries.
    /// If the same idempotency key is used, the same accreditation will be returned.
    /// </summary>
    [HttpPost("accreditations")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminRegulatoryOrgAdmin)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.ActivateAccreditationResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<DTOs.ActivateAccreditationResponseDto>>> ActivateAccreditation(
        Guid orgId,
        [FromBody] DTOs.ActivateAccreditationRequestDto request,
        CancellationToken cancellationToken)
    {
        // Organization access is validated by [Authorize(Policy = "OrgAccess")] policy at controller level

        // Extract Idempotency-Key header to prevent duplicate activations on retries
        // This is critical for enterprise systems where network timeouts can cause retries
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault() 
                          ?? Request.Headers["idempotency-key"].FirstOrDefault();

        // Get traceId for distributed tracing
        // This will be propagated as X-Correlation-Id header to downstream services via ProxyService
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "POST /api/v1/organizations/{OrgId}/accreditations - Dispatching ActivateAccreditationCommand, TraceId: {TraceId}, IdempotencyKey: {IdempotencyKey}",
            orgId, traceId, idempotencyKey ?? "none");

        var command = new ActivateAccreditationCommand(orgId, request, traceId, idempotencyKey);
        var response = await _mediator.Send(command, cancellationToken);

        var statusCode = response.Error?.Status ?? StatusCodes.Status201Created;
        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Suspends an active accreditation.
    /// Requires If-Match header with RowVersion (ETag) for optimistic concurrency control.
    /// If the RowVersion doesn't match the current state, returns 412 Precondition Failed.
    /// This prevents race conditions when multiple admins attempt to modify the same accreditation simultaneously.
    /// </summary>
    [HttpPatch("accreditations/{id:guid}/suspend")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminRegulatoryOrgAdmin)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.AccreditationLifecycleChangeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.AccreditationLifecycleChangeResponseDto>), StatusCodes.Status412PreconditionFailed)]
    public async Task<ActionResult<ApiResponse<DTOs.AccreditationLifecycleChangeResponseDto>>> SuspendAccreditation(
        Guid orgId,
        Guid id,
        [FromBody] DTOs.SuspendAccreditationRequestDto request,
        CancellationToken cancellationToken)
    {
        // Organization access is validated by [Authorize(Policy = "OrgAccess")] policy at controller level

        // Extract If-Match header for optimistic concurrency control
        // This should contain the RowVersion (ETag) from the last GET request
        var ifMatch = Request.Headers["If-Match"].FirstOrDefault() 
                   ?? Request.Headers["if-match"].FirstOrDefault();

        // Get traceId for distributed tracing
        // This will be propagated as X-Correlation-Id header to downstream services via ProxyService
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "PATCH /api/v1/organizations/{OrgId}/accreditations/{AccreditationId}/suspend - Dispatching SuspendAccreditationCommand, TraceId: {TraceId}, If-Match: {IfMatch}",
            orgId, id, traceId, ifMatch ?? "none");

        var command = new SuspendAccreditationCommand(orgId, id, request, traceId, ifMatch);
        var response = await _mediator.Send(command, cancellationToken);

        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }

    /// <summary>
    /// Archives an accreditation that has expired.
    /// Requires If-Match header with RowVersion (ETag) for optimistic concurrency control.
    /// If the RowVersion doesn't match the current state, returns 412 Precondition Failed.
    /// This prevents race conditions when multiple admins attempt to modify the same accreditation simultaneously.
    /// 
    /// Optional: Include X-Current-Status header with the accreditation's current status (from last GET request)
    /// to enable fail-fast validation of obviously invalid transitions (e.g., archiving an already archived accreditation).
    /// </summary>
    [HttpPatch("accreditations/{id:guid}/archive")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminRegulatoryOrgAdmin)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.AccreditationLifecycleChangeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.AccreditationLifecycleChangeResponseDto>), StatusCodes.Status412PreconditionFailed)]
    public async Task<ActionResult<ApiResponse<DTOs.AccreditationLifecycleChangeResponseDto>>> ArchiveAccreditation(
        Guid orgId,
        Guid id,
        CancellationToken cancellationToken)
    {
        // Organization access is validated by [Authorize(Policy = "OrgAccess")] policy at controller level

        // Extract If-Match header for optimistic concurrency control
        // This should contain the RowVersion (ETag) from the last GET request
        var ifMatch = Request.Headers["If-Match"].FirstOrDefault() 
                   ?? Request.Headers["if-match"].FirstOrDefault();

        // Extract optional current status for lifecycle transition guard (fail-fast validation)
        var currentStatus = Request.Headers["X-Current-Status"].FirstOrDefault() 
                         ?? Request.Headers["x-current-status"].FirstOrDefault();

        // Get traceId for distributed tracing
        // This will be propagated as X-Correlation-Id header to downstream services via ProxyService
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "PATCH /api/v1/organizations/{OrgId}/accreditations/{AccreditationId}/archive - Dispatching ArchiveAccreditationCommand, TraceId: {TraceId}, If-Match: {IfMatch}, CurrentStatus: {CurrentStatus}",
            orgId, id, traceId, ifMatch ?? "none", currentStatus ?? "none");

        var command = new ArchiveAccreditationCommand(orgId, id, traceId, ifMatch, currentStatus);
        var response = await _mediator.Send(command, cancellationToken);

        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }

    /// <summary>
    /// Triggers background recalculation of risk profile for the organization.
    /// 
    /// PROTECTIONS:
    /// - Rate limiting: Maximum 2 requests per organization per 5-minute window
    /// - Idempotency: Include Idempotency-Key header to prevent duplicate job scheduling
    /// - Distributed lock: Downstream service should use distributed lock to prevent concurrent execution
    /// - Job existence check: Downstream service should check if job already exists before creating new one
    /// 
    /// This endpoint is protected against:
    /// - UI spam/retries creating multiple heavy background jobs
    /// - Concurrent requests from multiple admins
    /// - Network timeouts causing duplicate job scheduling
    /// </summary>
    [HttpPost("risk/recalculate")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminRegulatoryOrgAdmin)]
    [EnableRateLimiting("RiskRecalculationPolicy")]
    [ProducesResponseType(typeof(ApiResponse<DTOs.RiskRecalculationResponseDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<DTOs.RiskRecalculationResponseDto>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<DTOs.RiskRecalculationResponseDto>>> RecalculateRisk(
        Guid orgId,
        CancellationToken cancellationToken)
    {
        // Organization access is validated by [Authorize(Policy = "OrgAccess")] policy at controller level

        // Extract Idempotency-Key header to prevent duplicate job scheduling on retries
        // Downstream service should check if a job with this key already exists
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault() 
                          ?? Request.Headers["idempotency-key"].FirstOrDefault();

        // Get traceId for distributed tracing
        // This will be propagated as X-Correlation-Id header to downstream services via ProxyService
        var traceId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "POST /api/v1/organizations/{OrgId}/risk/recalculate - Dispatching RecalculateRiskCommand, TraceId: {TraceId}, IdempotencyKey: {IdempotencyKey}",
            orgId, traceId, idempotencyKey ?? "none");

        var command = new RecalculateRiskCommand(orgId, traceId, idempotencyKey);
        var response = await _mediator.Send(command, cancellationToken);

        var statusCode = response.Error?.Status ?? StatusCodes.Status202Accepted;
        return StatusCode(statusCode, response);
    }
}

