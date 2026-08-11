using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/profiles")]
[Authorize]
public class AIProfilesAdminController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => mediator.Send(new ListAIProfilesQuery(V()), ct);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct) => mediator.Send(new GetAIProfileQuery(V(), id), ct);

    [HttpPost]
    [RequirePermission("AI.Profile.Manage")]
    public Task<IActionResult> Upsert([FromBody] object body, CancellationToken ct) => mediator.Send(new UpsertAIProfileQuery(V(), body), ct);

    [HttpGet("{id:guid}/permissions")]
    public Task<IActionResult> Permissions(Guid id, CancellationToken ct) => mediator.Send(new ListAIProfilePermissionsQuery(V(), id), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/policies")]
[Authorize]
public class AIAutomationPoliciesAdminController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List([FromQuery] Guid? profileId, CancellationToken ct) => mediator.Send(new ListAIAutomationPoliciesQuery(V(), profileId), ct);

    [HttpPost]
    [RequirePermission("AI.Policy.Manage")]
    public Task<IActionResult> Upsert([FromBody] object body, CancellationToken ct) => mediator.Send(new UpsertAIAutomationPolicyQuery(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/prompts")]
[Authorize]
public class AIPromptsAdminController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => mediator.Send(new ListAIPromptsQuery(V()), ct);

    [HttpPost]
    [RequirePermission("AI.Prompt.Manage")]
    public Task<IActionResult> Upsert([FromBody] object body, CancellationToken ct) => mediator.Send(new UpsertAIPromptQuery(V(), body), ct);

    [HttpPost("evaluate")]
    public Task<IActionResult> Evaluate([FromBody] object body, CancellationToken ct) => mediator.Send(new EvaluateAIPromptQuery(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/executions")]
[Authorize]
public class AIExecutionsAdminController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? profileId = null, CancellationToken ct = default)
        => mediator.Send(new ListAIExecutionsQuery(V(), page, pageSize, profileId), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/analytics")]
[Authorize]
public class AIAnalyticsAdminController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    [RequirePermission("AI.Analytics.View")]
    public Task<IActionResult> Get(CancellationToken ct) => mediator.Send(new GetAIAnalyticsQuery(V()), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai/approvals")]
[Authorize]
public class AIApprovalsAdminController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => mediator.Send(new ListAIApprovalsQuery(V()), ct);

    [HttpPost("{id:guid}/grant")]
    public Task<IActionResult> Grant(Guid id, CancellationToken ct) => mediator.Send(new GrantAIApprovalQuery(V(), id), ct);
}
