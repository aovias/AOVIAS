using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public record CreateGovernanceResponsibilityCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record UpdateGovernanceResponsibilityCommand(string ApiVersion, Guid Id, object RequestBody) : IRequest<IActionResult>;
public record DeleteGovernanceResponsibilityCommand(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public record AssignGovernanceResponsibilityCommand(string ApiVersion, Guid ResponsibilityId, object RequestBody) : IRequest<IActionResult>;
public record CreateCommitteeCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record UpdateCommitteeCommand(string ApiVersion, Guid Id, object RequestBody) : IRequest<IActionResult>;
public record AddCommitteeMemberCommand(string ApiVersion, Guid CommitteeId, object RequestBody) : IRequest<IActionResult>;
public record ScheduleCommitteeMeetingCommand(string ApiVersion, Guid CommitteeId, object RequestBody) : IRequest<IActionResult>;
public record CompleteCommitteeMeetingCommand(string ApiVersion, Guid CommitteeId, Guid MeetingId, object RequestBody) : IRequest<IActionResult>;
public record RecordCommitteeDecisionCommand(string ApiVersion, Guid CommitteeId, Guid MeetingId, object RequestBody) : IRequest<IActionResult>;
public record RecordCommitteeAttendanceCommand(string ApiVersion, Guid CommitteeId, Guid MeetingId, object RequestBody) : IRequest<IActionResult>;
public record ApproveCommitteeMinutesCommand(string ApiVersion, Guid CommitteeId, Guid MeetingId) : IRequest<IActionResult>;
public record CreateCommitteeActionItemCommand(string ApiVersion, Guid CommitteeId, Guid MeetingId, object RequestBody) : IRequest<IActionResult>;
public record LinkCommitteeActionTaskCommand(string ApiVersion, Guid CommitteeId, Guid MeetingId, Guid ActionItemId, object RequestBody) : IRequest<IActionResult>;
public record UpdateCommitteeActionStatusCommand(string ApiVersion, Guid CommitteeId, Guid MeetingId, Guid ActionItemId, object RequestBody) : IRequest<IActionResult>;
public record CreateApprovalChainCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record PublishApprovalChainCommand(string ApiVersion, Guid ChainId) : IRequest<IActionResult>;
public record EvaluateGovernanceApprovalCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record CreateEscalationChainCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public record PublishEscalationChainCommand(string ApiVersion, Guid ChainId) : IRequest<IActionResult>;
public record ResolveEscalationCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
