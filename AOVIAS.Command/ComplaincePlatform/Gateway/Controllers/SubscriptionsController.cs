using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subscriptions")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        IMediator mediator,
        ILogger<SubscriptionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<OrganizationSubscriptionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrganizationSubscriptionSummaryDto>>> GetCurrent(
        [FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            var error = new ApiError
            {
                Code = "VALIDATION_ERROR",
                Message = "organizationId is required",
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(ApiResponse<OrganizationSubscriptionSummaryDto>.ErrorResponse(error, string.Empty));
        }

        var query = new GetCurrentSubscriptionQuery(organizationId);
        var response = await _mediator.Send(query, cancellationToken);
        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrganizationSubscriptionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrganizationSubscriptionSummaryDto>>> Create(
        [FromBody] CreateSubscriptionRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSubscriptionCommand(request);
        var response = await _mediator.Send(command, cancellationToken);
        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }

    [HttpPatch("upgrade")]
    [ProducesResponseType(typeof(ApiResponse<OrganizationSubscriptionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrganizationSubscriptionSummaryDto>>> Upgrade(
        [FromBody] UpgradeSubscriptionRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpgradeSubscriptionCommand(request);
        var response = await _mediator.Send(command, cancellationToken);
        return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
    }
}

