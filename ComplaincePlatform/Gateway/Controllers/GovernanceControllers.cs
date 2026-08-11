using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/governance")]
[Authorize]
public class GovernanceDashboardController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("timeline")]
    public Task<IActionResult> Timeline([FromQuery] string? resourceType, [FromQuery] Guid? resourceId, [FromQuery] Guid? organizationId, CancellationToken ct = default)
        => mediator.Send(new GetGovernanceTimelineQuery(V(), resourceType, resourceId, organizationId), ct);

    [HttpGet("analytics/summary")]
    public Task<IActionResult> Analytics(CancellationToken ct = default)
        => mediator.Send(new GetGovernanceAnalyticsQuery(V()), ct);

    [HttpGet("analytics/kpis")]
    public Task<IActionResult> Kpis(CancellationToken ct = default)
        => mediator.Send(new GetGovernanceKpisQuery(V()), ct);

    [HttpGet("lookups/{category}")]
    public Task<IActionResult> Lookups(string category, CancellationToken ct = default)
        => mediator.Send(new GetGovernanceLookupsQuery(V(), category), ct);

    [HttpGet("agenda-templates")]
    public Task<IActionResult> AgendaTemplates(CancellationToken ct = default)
        => mediator.Send(new ListAgendaTemplatesQuery(V()), ct);

    [HttpGet("management-review/agenda")]
    public Task<IActionResult> ManagementReviewAgenda([FromQuery] Guid? committeeId, CancellationToken ct = default)
        => mediator.Send(new GetManagementReviewAgendaQuery(V(), committeeId), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/committees")]
[Authorize]
public class GovernanceCommitteesController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct = default) => mediator.Send(new ListCommitteesQuery(V()), ct);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct = default) => mediator.Send(new GetCommitteeQuery(V(), id), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateCommitteeCommand(V(), body), ct);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new UpdateCommitteeCommand(V(), id, body), ct);

    [HttpPost("{id:guid}/members")]
    public Task<IActionResult> AddMember(Guid id, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new AddCommitteeMemberCommand(V(), id, body), ct);

    [HttpGet("{id:guid}/meetings")]
    public Task<IActionResult> ListMeetings(Guid id, CancellationToken ct = default)
        => mediator.Send(new ListCommitteeMeetingsQuery(V(), id), ct);

    [HttpGet("{committeeId:guid}/meetings/{meetingId:guid}")]
    public Task<IActionResult> GetMeeting(Guid committeeId, Guid meetingId, CancellationToken ct = default)
        => mediator.Send(new GetCommitteeMeetingQuery(V(), committeeId, meetingId), ct);

    [HttpPost("{id:guid}/meetings")]
    public Task<IActionResult> ScheduleMeeting(Guid id, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new ScheduleCommitteeMeetingCommand(V(), id, body), ct);

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/complete")]
    public Task<IActionResult> CompleteMeeting(Guid committeeId, Guid meetingId, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CompleteCommitteeMeetingCommand(V(), committeeId, meetingId, body), ct);

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/attendance")]
    public Task<IActionResult> RecordAttendance(Guid committeeId, Guid meetingId, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new RecordCommitteeAttendanceCommand(V(), committeeId, meetingId, body), ct);

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/approve-minutes")]
    public Task<IActionResult> ApproveMinutes(Guid committeeId, Guid meetingId, CancellationToken ct = default)
        => mediator.Send(new ApproveCommitteeMinutesCommand(V(), committeeId, meetingId), ct);

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/decisions")]
    public Task<IActionResult> RecordDecision(Guid committeeId, Guid meetingId, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new RecordCommitteeDecisionCommand(V(), committeeId, meetingId, body), ct);

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/actions")]
    public Task<IActionResult> CreateAction(Guid committeeId, Guid meetingId, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateCommitteeActionItemCommand(V(), committeeId, meetingId, body), ct);

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/actions/{actionItemId:guid}/link-task")]
    public Task<IActionResult> LinkActionTask(Guid committeeId, Guid meetingId, Guid actionItemId, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new LinkCommitteeActionTaskCommand(V(), committeeId, meetingId, actionItemId, body), ct);

    [HttpPatch("{committeeId:guid}/meetings/{meetingId:guid}/actions/{actionItemId:guid}/status")]
    public Task<IActionResult> UpdateActionStatus(Guid committeeId, Guid meetingId, Guid actionItemId, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new UpdateCommitteeActionStatusCommand(V(), committeeId, meetingId, actionItemId, body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/approval-chains")]
[Authorize]
public class GovernanceApprovalChainsController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct = default) => mediator.Send(new ListGovernanceApprovalChainsQuery(V()), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateApprovalChainCommand(V(), body), ct);

    [HttpPost("{id:guid}/publish")]
    public Task<IActionResult> Publish(Guid id, CancellationToken ct = default)
        => mediator.Send(new PublishApprovalChainCommand(V(), id), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/approval")]
[Authorize]
public class GovernanceApprovalController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpPost("evaluate")]
    public Task<IActionResult> Evaluate([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new EvaluateGovernanceApprovalCommand(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/escalation-chains")]
[Authorize]
public class GovernanceEscalationChainsController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct = default) => mediator.Send(new ListEscalationChainsQuery(V()), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateEscalationChainCommand(V(), body), ct);

    [HttpPost("{id:guid}/publish")]
    public Task<IActionResult> Publish(Guid id, CancellationToken ct = default)
        => mediator.Send(new PublishEscalationChainCommand(V(), id), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/escalation")]
[Authorize]
public class GovernanceEscalationController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct = default)
        => mediator.Send(new GetEscalationChainQuery(V(), id), ct);

    [HttpPost("resolve")]
    public Task<IActionResult> Resolve([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new ResolveEscalationCommand(V(), body), ct);
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/governance/responsibilities")]
[Authorize]
public class GovernanceResponsibilitiesController(IMediator mediator) : ControllerBase
{
    private string V() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet]
    public Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => mediator.Send(new ListGovernanceResponsibilitiesQuery(V(), page, pageSize), ct);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct = default)
        => mediator.Send(new GetGovernanceResponsibilityQuery(V(), id), ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new CreateGovernanceResponsibilityCommand(V(), body), ct);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new UpdateGovernanceResponsibilityCommand(V(), id, body), ct);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        => mediator.Send(new DeleteGovernanceResponsibilityCommand(V(), id), ct);

    [HttpPost("{id:guid}/assign")]
    public Task<IActionResult> Assign(Guid id, [FromBody] object body, CancellationToken ct = default)
        => mediator.Send(new AssignGovernanceResponsibilityCommand(V(), id, body), ct);
}
