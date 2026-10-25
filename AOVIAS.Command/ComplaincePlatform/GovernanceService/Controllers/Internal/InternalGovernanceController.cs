using GovernanceService.Application.Commands;
using GovernanceService.Application.Contracts;
using GovernanceService.Application.Queries;
using GovernanceService.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace GovernanceService.Controllers.Internal;

[ApiController]
[Route("internal/governance")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalGovernanceController(IMediator mediator, IGovernanceTimelineService timeline) : ControllerBase
{
    [HttpPost("approval/evaluate")]
    public async Task<ActionResult<ApiResponse<ApprovalDecisionDto>>> EvaluateApproval([FromBody] ApprovalEvaluateRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new EvaluateApprovalCommand(request), ct), HttpContext.TraceIdentifier));

    [HttpPost("escalation/resolve")]
    public async Task<ActionResult<ApiResponse<EscalationResolutionDto>>> ResolveEscalation([FromBody] EscalationResolveRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ResolveEscalationCommand(request), ct), HttpContext.TraceIdentifier));

    [HttpGet("timeline")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GovernanceTimelineDto>>>> Timeline([FromQuery] string? resourceType, [FromQuery] Guid? resourceId, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetGovernanceTimelineQuery(resourceType, resourceId, null), ct), HttpContext.TraceIdentifier));

    [HttpGet("owners")]
    public async Task<ActionResult<ApiResponse<OwnerLookupResultDto>>> Owners([FromQuery] string resourceType, [FromQuery] Guid resourceId, [FromQuery] string? responsibilityTypeCode, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await timeline.GetOwnersAsync(resourceType, resourceId, responsibilityTypeCode, ct), HttpContext.TraceIdentifier));
}
