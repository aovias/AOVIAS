using GovernanceService.Application.Commands;
using GovernanceService.Application.Contracts;
using GovernanceService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace GovernanceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/responsibilities")]
[Authorize]
public class ResponsibilitiesController(IMediator mediator) : ControllerBase
{
    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<ResponsibilityDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var data = await mediator.Send(new ListResponsibilitiesQuery(page, pageSize), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ResponsibilityDto>>> Get(Guid id, CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetResponsibilityByIdQuery(id), ct);
        if (data == null) return NotFound(ApiResponseFactory.Failure<ResponsibilityDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ResponsibilityDto>>> Create([FromBody] CreateResponsibilityRequest request, CancellationToken ct = default)
    {
        var data = await mediator.Send(new CreateResponsibilityCommand(request, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateResponsibilityRequest request, CancellationToken ct = default)
    {
        await mediator.Send(new UpdateResponsibilityCommand(id, request), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new DeleteResponsibilityCommand(id, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<ApiResponse<GovernanceAssignmentDto>>> Assign(Guid id, [FromBody] AssignGovernanceRequest request, CancellationToken ct = default)
    {
        var data = await mediator.Send(new AssignGovernanceCommand(id, request, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/committees")]
[Authorize]
public class CommitteesController(IMediator mediator) : ControllerBase
{
    private Guid Actor() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CommitteeDto>>>> List(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListCommitteesQuery(), ct), HttpContext.TraceIdentifier));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CommitteeDto>>> Get(Guid id, CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetCommitteeByIdQuery(id), ct);
        if (data == null) return NotFound(ApiResponseFactory.Failure<CommitteeDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CommitteeDto>>> Create([FromBody] CreateCommitteeRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateCommitteeCommand(request, Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCommitteeRequest request, CancellationToken ct = default)
    {
        await mediator.Send(new UpdateCommitteeCommand(id, request, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddCommitteeMemberRequest request, CancellationToken ct = default)
    {
        await mediator.Send(new AddCommitteeMemberCommand(id, request, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/meetings")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CommitteeMeetingDto>>>> ListMeetings(Guid id, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListCommitteeMeetingsQuery(id), ct), HttpContext.TraceIdentifier));

    [HttpGet("{committeeId:guid}/meetings/{meetingId:guid}")]
    public async Task<ActionResult<ApiResponse<CommitteeMeetingDto>>> GetMeeting(Guid committeeId, Guid meetingId, CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetCommitteeMeetingQuery(committeeId, meetingId), ct);
        if (data == null) return NotFound(ApiResponseFactory.Failure<CommitteeMeetingDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/meetings")]
    public async Task<ActionResult<ApiResponse<CommitteeMeetingDto>>> ScheduleMeeting(Guid id, [FromBody] ScheduleMeetingRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ScheduleMeetingCommand(id, request, Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/complete")]
    public async Task<IActionResult> CompleteMeeting(Guid committeeId, Guid meetingId, [FromBody] CompleteMeetingRequest request, CancellationToken ct = default)
    {
        await mediator.Send(new CompleteMeetingCommand(committeeId, meetingId, request, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/attendance")]
    public async Task<IActionResult> RecordAttendance(Guid committeeId, Guid meetingId, [FromBody] RecordAttendanceRequest request, CancellationToken ct = default)
    {
        await mediator.Send(new RecordAttendanceCommand(committeeId, meetingId, request, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/approve-minutes")]
    public async Task<IActionResult> ApproveMinutes(Guid committeeId, Guid meetingId, CancellationToken ct = default)
    {
        await mediator.Send(new ApproveMinutesCommand(committeeId, meetingId, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/decisions")]
    public async Task<ActionResult<ApiResponse<CommitteeDecisionDto>>> RecordDecision(Guid committeeId, Guid meetingId, [FromBody] RecordDecisionRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new RecordDecisionCommand(committeeId, meetingId, request, Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/actions")]
    public async Task<ActionResult<ApiResponse<CommitteeActionItemDto>>> CreateActionItem(Guid committeeId, Guid meetingId, [FromBody] CreateActionItemRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateActionItemCommand(committeeId, meetingId, request, Actor()), ct), HttpContext.TraceIdentifier));

    [HttpPost("{committeeId:guid}/meetings/{meetingId:guid}/actions/{actionItemId:guid}/link-task")]
    public async Task<IActionResult> LinkActionTask(Guid committeeId, Guid meetingId, Guid actionItemId, [FromBody] LinkActionItemTaskRequest request, CancellationToken ct = default)
    {
        await mediator.Send(new LinkActionItemTaskCommand(committeeId, meetingId, actionItemId, request, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }

    [HttpPatch("{committeeId:guid}/meetings/{meetingId:guid}/actions/{actionItemId:guid}/status")]
    public async Task<IActionResult> UpdateActionStatus(Guid committeeId, Guid meetingId, Guid actionItemId, [FromBody] UpdateActionItemStatusRequest request, CancellationToken ct = default)
    {
        await mediator.Send(new UpdateActionItemStatusCommand(committeeId, meetingId, actionItemId, request, Actor()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/approval-chains")]
[Authorize]
public class ApprovalChainsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApprovalChainDto>>>> List(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListApprovalChainsQuery(), ct), HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ApprovalChainDto>>> Create([FromBody] CreateApprovalChainRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateApprovalChainCommand(request), ct), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new PublishApprovalChainCommand(id), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/approval")]
[Authorize]
public class ApprovalController(IMediator mediator) : ControllerBase
{
    [HttpPost("evaluate")]
    public async Task<ActionResult<ApiResponse<ApprovalDecisionDto>>> Evaluate([FromBody] ApprovalEvaluateRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new EvaluateApprovalCommand(request), ct), HttpContext.TraceIdentifier));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/escalation-chains")]
[Authorize]
public class EscalationChainsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EscalationChainDto>>>> List(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListEscalationChainsQuery(), ct), HttpContext.TraceIdentifier));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EscalationChainDto>>> Create([FromBody] CreateEscalationChainRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new CreateEscalationChainCommand(request), ct), HttpContext.TraceIdentifier));

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new PublishEscalationChainCommand(id), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/escalation")]
[Authorize]
public class EscalationController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EscalationChainDto>>> Get(Guid id, CancellationToken ct = default)
    {
        var data = await mediator.Send(new GetEscalationChainByIdQuery(id), ct);
        if (data == null) return NotFound(ApiResponseFactory.Failure<EscalationChainDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost("resolve")]
    public async Task<ActionResult<ApiResponse<EscalationResolutionDto>>> Resolve([FromBody] EscalationResolveRequest request, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ResolveEscalationCommand(request), ct), HttpContext.TraceIdentifier));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/governance")]
[Authorize]
public class GovernanceController(IMediator mediator) : ControllerBase
{
    [HttpGet("timeline")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GovernanceTimelineDto>>>> Timeline([FromQuery] string? resourceType, [FromQuery] Guid? resourceId, [FromQuery] Guid? organizationId, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetGovernanceTimelineQuery(resourceType, resourceId, organizationId), ct), HttpContext.TraceIdentifier));

    [HttpGet("analytics/summary")]
    public async Task<ActionResult<ApiResponse<GovernanceAnalyticsDto>>> Analytics(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetGovernanceAnalyticsQuery(), ct), HttpContext.TraceIdentifier));

    [HttpGet("analytics/kpis")]
    public async Task<ActionResult<ApiResponse<GovernanceKpisDto>>> Kpis(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetGovernanceKpisQuery(), ct), HttpContext.TraceIdentifier));

    [HttpGet("lookups/{category}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupValueDto>>>> Lookups(string category, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetGovernanceLookupsQuery(category), ct), HttpContext.TraceIdentifier));

    [HttpGet("agenda-templates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgendaTemplateDto>>>> AgendaTemplates(CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new ListAgendaTemplatesQuery(), ct), HttpContext.TraceIdentifier));

    [HttpGet("management-review/agenda")]
    public async Task<ActionResult<ApiResponse<ManagementReviewAgendaInputDto>>> ManagementReviewAgenda([FromQuery] Guid? committeeId, CancellationToken ct = default)
        => Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, await mediator.Send(new GetManagementReviewAgendaQuery(committeeId), ct), HttpContext.TraceIdentifier));
}
