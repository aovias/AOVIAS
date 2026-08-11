using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organization-members")]
[Authorize]
public class OrganizationMembersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrganizationMembersController> _logger;

    public OrganizationMembersController(IMediator mediator, ILogger<OrganizationMembersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrganizationMemberResponseDto>>> CreateOrganizationMember(
        [FromBody] CreateOrganizationMemberRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _mediator.Send(new CreateOrganizationMemberCommand(request), cancellationToken);
            var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, success);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "CreateOrganizationMember failed");
            var failure = ApiResponseFactory.Failure<OrganizationMemberResponseDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpPut("{memberId:guid}")]
    public async Task<ActionResult<ApiResponse<OrganizationMemberResponseDto>>> UpdateOrganizationMember(
        [FromRoute] Guid memberId,
        [FromBody] UpdateOrganizationMemberRequestDto request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _mediator.Send(new UpdateOrganizationMemberCommand(memberId, request), cancellationToken);
            var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, success);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "OrganizationMember not found: {MemberId}", memberId);
            var failure = ApiResponseFactory.Failure<OrganizationMemberResponseDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpGet("{memberId:guid}")]
    public async Task<ActionResult<ApiResponse<OrganizationMemberResponseDto>>> GetOrganizationMember(
        [FromRoute] Guid memberId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _mediator.Send(new GetOrganizationMemberQuery(memberId), cancellationToken);
            var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, success);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "OrganizationMember not found: {MemberId}", memberId);
            var failure = ApiResponseFactory.Failure<OrganizationMemberResponseDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpGet("organization/{organizationId:guid}")]
    public async Task<ActionResult<ApiResponse<List<OrganizationMemberResponseDto>>>> ListOrganizationMembers(
        [FromRoute] Guid organizationId,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var result = await _mediator.Send(new ListOrganizationMembersQuery(organizationId), cancellationToken);
        var success = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, success);
    }
}
