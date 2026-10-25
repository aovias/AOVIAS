using Gateway.Application.Queries;
using Gateway.Application.Validators;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gateway.DTOs;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenant")]
[Authorize]
[Authorize(Policy = "OrgAccess")]
public class TenantController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<GetOrganizationDetailQuery> _validator;
    private readonly ILogger<TenantController> _logger;

    public TenantController(
        IMediator mediator,
        IValidator<GetOrganizationDetailQuery> validator,
        ILogger<TenantController> logger)
    {
        _mediator = mediator;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the complete organization detail including hospitals and members for a given tenant.
    /// </summary>
    /// <param name="tenantId">The organization/tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Organization detail with hierarchy</returns>
    [HttpGet("{tenantId}/organization-detail")]
    [ProducesResponseType(typeof(ApiResponse<OrganizationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<OrganizationDetailDto>>> GetOrganizationDetail(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetOrganizationDetail requested for tenant {TenantId}", tenantId);

        var query = new GetOrganizationDetailQuery(tenantId);

        // Validate query
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for GetOrganizationDetail query: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var traceId = HttpContext.TraceIdentifier;
            var response = ApiResponseFactory.Failure<OrganizationDetailDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, response);
        }

        try
        {
            var result = await _mediator.Send(query, cancellationToken);

            var traceId = HttpContext.TraceIdentifier;
            var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, success);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Organization not found for tenant {TenantId}", tenantId);

            var traceId = HttpContext.TraceIdentifier;
            var failure = ApiResponseFactory.Failure<OrganizationDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization detail for tenant {TenantId}", tenantId);

            var traceId = HttpContext.TraceIdentifier;
            var failure = ApiResponseFactory.Failure<OrganizationDetailDto>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }
}
