using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Application.Contracts;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;

namespace OrganizationService.Controllers.Internal;

/// <summary>CRP Phase 7 — internal invitation redeem/pipeline helpers for Gateway orchestration.</summary>
[ApiController]
[Route("internal/invitations")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public sealed class InternalInvitationsController : ControllerBase
{
    private readonly IInvitationAppService _invitations;

    public InternalInvitationsController(IInvitationAppService invitations)
    {
        _invitations = invitations;
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] string token, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var preview = await _invitations.PreviewByTokenAsync(token ?? string.Empty, ct);
        if (preview == null)
        {
            return Fail(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
        }

        return OkSuccess(preview, traceId);
    }

    [HttpGet("by-token")]
    public async Task<IActionResult> ByToken([FromQuery] string token, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var invite = await _invitations.GetByTokenAsync(token ?? string.Empty, ct);
        if (invite == null)
        {
            return Fail(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
        }

        return OkSuccess(invite, traceId);
    }

    [HttpPost("{id:guid}/accept-for-user")]
    public async Task<IActionResult> AcceptForUser(Guid id, [FromBody] AcceptForUserRequest body, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var member = await _invitations.AcceptAsync(id, body.UserId, ct);
            return OkSuccess(member, traceId);
        }
        catch (InvalidOperationException ex)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId, ex.Message);
        }
    }

    public record AcceptForUserRequest(Guid UserId);

    [HttpPost("{id:guid}/pipeline")]
    public async Task<IActionResult> UpdatePipeline(Guid id, [FromBody] InvitationPipelineUpdateRequest body, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var dto = await _invitations.UpdatePipelineAsync(id, body, ct);
            return OkSuccess(dto, traceId);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
        }
    }

    [HttpPost("link-employment")]
    public async Task<IActionResult> LinkEmployment([FromBody] LinkEmploymentRequest body, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            await _invitations.LinkEmploymentAsync(body.MembershipId, body.EmploymentId, body.PersonId, ct);
            return OkSuccess(new { body.MembershipId, body.EmploymentId }, traceId);
        }
        catch (InvalidOperationException)
        {
            return Fail(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
        }
    }

    public record LinkEmploymentRequest(Guid MembershipId, Guid EmploymentId, Guid? PersonId);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvitationRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var actor = request.OrganizationId; // system/wizard; InvitedBy set when actor known
            var dto = await _invitations.CreateAsync(request, Guid.Empty, ct);
            return OkSuccess(dto, traceId);
        }
        catch (InvalidOperationException ex)
        {
            return Fail(ApiResponseCode.VALIDATION_FAILED, traceId, ex.Message);
        }
    }

    private static IActionResult OkSuccess(object data, string traceId)
    {
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return new ObjectResult(response) { StatusCode = meta.HttpStatus };
    }

    private static IActionResult Fail(ApiResponseCode code, string traceId, string? message = null)
    {
        var failure = ApiResponseFactory.Failure<object>(code, traceId);
        if (!string.IsNullOrWhiteSpace(message) && failure.Error != null)
        {
            failure.Error.Message = message;
        }

        var meta = ApiResponseCodes.Registry[code];
        return new ObjectResult(failure) { StatusCode = meta.HttpStatus };
    }
}
