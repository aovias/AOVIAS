using AIOrchestratorService.Application.AI.Identity.Commands;
using AIOrchestratorService.Application.AI.Identity.Contracts;
using AIOrchestratorService.Application.AI.Identity.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace AIOrchestratorService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/profiles")]
[Authorize(Policy = AuthConstants.Policies.AiScope)]
public class AIProfilesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListAIProfilesQuery(), ct), HttpContext.TraceIdentifier));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var data = await mediator.Send(new GetAIProfileQuery(id), ct);
        return data == null ? NotFound(ApiResponseFactory.Failure<AIProfileDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier))
            : Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [RequirePermission("AI.Profile.Manage")]
    public async Task<IActionResult> Upsert([FromBody] UpsertAIProfileRequest body, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new UpsertAIProfileCommand(body), ct), HttpContext.TraceIdentifier));

    [HttpGet("{id:guid}/permissions")]
    public async Task<IActionResult> Permissions(Guid id, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListProfilePermissionsQuery(id), ct), HttpContext.TraceIdentifier));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/policies")]
[Authorize(Policy = AuthConstants.Policies.AiScope)]
public class AIAutomationPoliciesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? profileId, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListAutomationPoliciesQuery(profileId), ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("AI.Policy.Manage")]
    public async Task<IActionResult> Upsert([FromBody] UpsertAutomationPolicyRequest body, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new UpsertAutomationPolicyCommand(body), ct), HttpContext.TraceIdentifier));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/prompts")]
[Authorize(Policy = AuthConstants.Policies.AiScope)]
public class AIPromptsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListPromptProfilesQuery(), ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("AI.Prompt.Manage")]
    public async Task<IActionResult> Upsert([FromBody] UpsertPromptRequest body, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new UpsertPromptCommand(body), ct), HttpContext.TraceIdentifier));

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] PromptEvaluateRequest body, CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new EvaluatePromptCommand(body), ct), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/rollback")]
    [RequirePermission("AI.Prompt.Manage")]
    public async Task<IActionResult> Rollback(Guid id, [FromQuery] int version, CancellationToken ct)
    {
        await mediator.Send(new RollbackPromptCommand(id, version), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/executions")]
[Authorize(Policy = AuthConstants.Policies.AiScope)]
public class AIExecutionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? profileId = null, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListExecutionsQuery(page, pageSize, profileId), ct), HttpContext.TraceIdentifier));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var data = await mediator.Send(new GetExecutionQuery(id), ct);
        return data == null ? NotFound() : Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/analytics")]
[Authorize(Policy = AuthConstants.Policies.AiScope)]
public class AIAnalyticsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("AI.Analytics.View")]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetAIAnalyticsQuery(), ct), HttpContext.TraceIdentifier));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/approvals")]
[Authorize(Policy = AuthConstants.Policies.AiScope)]
public class AIApprovalsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListPendingApprovalsQuery(), ct), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/grant")]
    public async Task<IActionResult> Grant(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userId, out var uid)) return Unauthorized();
        await mediator.Send(new GrantAIApprovalCommand(id, uid), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("internal/ai/profiles")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalAIProfilesController(IMediator mediator) : ControllerBase
{
    [HttpGet("by-persona/{personaKey}")]
    public async Task<IActionResult> ByPersona(string personaKey, CancellationToken ct)
    {
        var data = await mediator.Send(new GetProfileByCopilotPersonaQuery(personaKey), ct);
        return data == null ? NotFound() : Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }
}
