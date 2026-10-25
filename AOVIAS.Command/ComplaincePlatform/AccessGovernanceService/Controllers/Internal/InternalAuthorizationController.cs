using AccessGovernanceService.Application.Commands;
using AccessGovernanceService.Application.Contracts;
using AccessGovernanceService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace AccessGovernanceService.Controllers.Internal;

[ApiController]
[Route("internal/authorization")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalAuthorizationController(IMediator mediator) : ControllerBase
{
    [HttpPost("evaluate")]
    public async Task<ActionResult<ApiResponse<AuthorizationDecisionDto>>> Evaluate([FromBody] EvaluateAuthorizationRequest request, CancellationToken ct = default)
    {
        var data = await mediator.Send(new EvaluateAuthorizationCommand(request), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpGet("effective-permissions/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EffectivePermissionDto>>>> EffectivePermissions(Guid userId, CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetEffectivePermissionsQuery(userId), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }
}
