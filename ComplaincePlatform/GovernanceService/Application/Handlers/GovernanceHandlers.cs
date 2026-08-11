using GovernanceService.Application.Contracts;
using GovernanceService.Application.Services;
using MediatR;

namespace GovernanceService.Application.Commands;

public record CreateResponsibilityCommand(CreateResponsibilityRequest Request, Guid ActorUserId) : IRequest<ResponsibilityDto>;
public record UpdateResponsibilityCommand(Guid Id, UpdateResponsibilityRequest Request) : IRequest<Unit>;
public record DeleteResponsibilityCommand(Guid Id, Guid ActorUserId) : IRequest<Unit>;
public record AssignGovernanceCommand(Guid ResponsibilityId, AssignGovernanceRequest Request, Guid ActorUserId) : IRequest<GovernanceAssignmentDto>;
public record CreateCommitteeCommand(CreateCommitteeRequest Request, Guid ActorUserId) : IRequest<CommitteeDto>;
public record UpdateCommitteeCommand(Guid CommitteeId, UpdateCommitteeRequest Request, Guid ActorUserId) : IRequest<Unit>;
public record AddCommitteeMemberCommand(Guid CommitteeId, AddCommitteeMemberRequest Request, Guid ActorUserId) : IRequest<Unit>;
public record ScheduleMeetingCommand(Guid CommitteeId, ScheduleMeetingRequest Request, Guid ActorUserId) : IRequest<CommitteeMeetingDto>;
public record CompleteMeetingCommand(Guid CommitteeId, Guid MeetingId, CompleteMeetingRequest Request, Guid ActorUserId) : IRequest<Unit>;
public record RecordDecisionCommand(Guid CommitteeId, Guid MeetingId, RecordDecisionRequest Request, Guid ActorUserId) : IRequest<CommitteeDecisionDto>;
public record RecordAttendanceCommand(Guid CommitteeId, Guid MeetingId, RecordAttendanceRequest Request, Guid ActorUserId) : IRequest<Unit>;
public record ApproveMinutesCommand(Guid CommitteeId, Guid MeetingId, Guid ActorUserId) : IRequest<Unit>;
public record CreateActionItemCommand(Guid CommitteeId, Guid MeetingId, CreateActionItemRequest Request, Guid ActorUserId) : IRequest<CommitteeActionItemDto>;
public record LinkActionItemTaskCommand(Guid CommitteeId, Guid MeetingId, Guid ActionItemId, LinkActionItemTaskRequest Request, Guid ActorUserId) : IRequest<Unit>;
public record UpdateActionItemStatusCommand(Guid CommitteeId, Guid MeetingId, Guid ActionItemId, UpdateActionItemStatusRequest Request, Guid ActorUserId) : IRequest<Unit>;
public record CreateApprovalChainCommand(CreateApprovalChainRequest Request) : IRequest<ApprovalChainDto>;
public record PublishApprovalChainCommand(Guid ChainId) : IRequest<Unit>;
public record CreateEscalationChainCommand(CreateEscalationChainRequest Request) : IRequest<EscalationChainDto>;
public record PublishEscalationChainCommand(Guid ChainId) : IRequest<Unit>;
public record EvaluateApprovalCommand(ApprovalEvaluateRequest Request) : IRequest<ApprovalDecisionDto>;
public record ResolveEscalationCommand(EscalationResolveRequest Request) : IRequest<EscalationResolutionDto>;

public class CreateResponsibilityHandler(IResponsibilityAppService svc) : IRequestHandler<CreateResponsibilityCommand, ResponsibilityDto>
{
    public Task<ResponsibilityDto> Handle(CreateResponsibilityCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, request.ActorUserId, ct);
}
public class UpdateResponsibilityHandler(IResponsibilityAppService svc) : IRequestHandler<UpdateResponsibilityCommand, Unit>
{
    public async Task<Unit> Handle(UpdateResponsibilityCommand request, CancellationToken ct) { await svc.UpdateAsync(request.Id, request.Request, ct); return Unit.Value; }
}
public class DeleteResponsibilityHandler(IResponsibilityAppService svc) : IRequestHandler<DeleteResponsibilityCommand, Unit>
{
    public async Task<Unit> Handle(DeleteResponsibilityCommand request, CancellationToken ct) { await svc.DeleteAsync(request.Id, request.ActorUserId, ct); return Unit.Value; }
}
public class AssignGovernanceHandler(IResponsibilityAppService svc) : IRequestHandler<AssignGovernanceCommand, GovernanceAssignmentDto>
{
    public Task<GovernanceAssignmentDto> Handle(AssignGovernanceCommand request, CancellationToken ct) => svc.AssignAsync(request.ResponsibilityId, request.Request, request.ActorUserId, ct);
}
public class CreateCommitteeHandler(ICommitteeAppService svc) : IRequestHandler<CreateCommitteeCommand, CommitteeDto>
{
    public Task<CommitteeDto> Handle(CreateCommitteeCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, request.ActorUserId, ct);
}
public class UpdateCommitteeHandler(ICommitteeAppService svc) : IRequestHandler<UpdateCommitteeCommand, Unit>
{
    public async Task<Unit> Handle(UpdateCommitteeCommand request, CancellationToken ct) { await svc.UpdateAsync(request.CommitteeId, request.Request, request.ActorUserId, ct); return Unit.Value; }
}
public class AddCommitteeMemberHandler(ICommitteeAppService svc) : IRequestHandler<AddCommitteeMemberCommand, Unit>
{
    public async Task<Unit> Handle(AddCommitteeMemberCommand request, CancellationToken ct) { await svc.AddMemberAsync(request.CommitteeId, request.Request, request.ActorUserId, ct); return Unit.Value; }
}
public class ScheduleMeetingHandler(ICommitteeAppService svc) : IRequestHandler<ScheduleMeetingCommand, CommitteeMeetingDto>
{
    public Task<CommitteeMeetingDto> Handle(ScheduleMeetingCommand request, CancellationToken ct)
        => svc.ScheduleMeetingAsync(request.CommitteeId, request.Request, request.ActorUserId, ct);
}
public class CompleteMeetingHandler(ICommitteeAppService svc) : IRequestHandler<CompleteMeetingCommand, Unit>
{
    public async Task<Unit> Handle(CompleteMeetingCommand request, CancellationToken ct) { await svc.CompleteMeetingAsync(request.CommitteeId, request.MeetingId, request.Request, request.ActorUserId, ct); return Unit.Value; }
}
public class RecordDecisionHandler(ICommitteeAppService svc) : IRequestHandler<RecordDecisionCommand, CommitteeDecisionDto>
{
    public Task<CommitteeDecisionDto> Handle(RecordDecisionCommand request, CancellationToken ct)
        => svc.RecordDecisionAsync(request.CommitteeId, request.MeetingId, request.Request, request.ActorUserId, ct);
}
public class RecordAttendanceHandler(ICommitteeAppService svc) : IRequestHandler<RecordAttendanceCommand, Unit>
{
    public async Task<Unit> Handle(RecordAttendanceCommand request, CancellationToken ct)
    {
        await svc.RecordAttendanceAsync(request.CommitteeId, request.MeetingId, request.Request, request.ActorUserId, ct);
        return Unit.Value;
    }
}
public class ApproveMinutesHandler(ICommitteeAppService svc) : IRequestHandler<ApproveMinutesCommand, Unit>
{
    public async Task<Unit> Handle(ApproveMinutesCommand request, CancellationToken ct)
    {
        await svc.ApproveMinutesAsync(request.CommitteeId, request.MeetingId, request.ActorUserId, ct);
        return Unit.Value;
    }
}
public class CreateActionItemHandler(ICommitteeAppService svc) : IRequestHandler<CreateActionItemCommand, CommitteeActionItemDto>
{
    public Task<CommitteeActionItemDto> Handle(CreateActionItemCommand request, CancellationToken ct)
        => svc.CreateActionItemAsync(request.CommitteeId, request.MeetingId, request.Request, request.ActorUserId, ct);
}
public class LinkActionItemTaskHandler(ICommitteeAppService svc) : IRequestHandler<LinkActionItemTaskCommand, Unit>
{
    public async Task<Unit> Handle(LinkActionItemTaskCommand request, CancellationToken ct)
    {
        await svc.LinkActionItemTaskAsync(request.CommitteeId, request.MeetingId, request.ActionItemId, request.Request, request.ActorUserId, ct);
        return Unit.Value;
    }
}
public class UpdateActionItemStatusHandler(ICommitteeAppService svc) : IRequestHandler<UpdateActionItemStatusCommand, Unit>
{
    public async Task<Unit> Handle(UpdateActionItemStatusCommand request, CancellationToken ct)
    {
        await svc.UpdateActionItemStatusAsync(request.CommitteeId, request.MeetingId, request.ActionItemId, request.Request, request.ActorUserId, ct);
        return Unit.Value;
    }
}
public class CreateApprovalChainHandler(IApprovalChainAppService svc) : IRequestHandler<CreateApprovalChainCommand, ApprovalChainDto>
{
    public Task<ApprovalChainDto> Handle(CreateApprovalChainCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, ct);
}
public class PublishApprovalChainHandler(IApprovalChainAppService svc) : IRequestHandler<PublishApprovalChainCommand, Unit>
{
    public async Task<Unit> Handle(PublishApprovalChainCommand request, CancellationToken ct) { await svc.PublishAsync(request.ChainId, ct); return Unit.Value; }
}
public class CreateEscalationChainHandler(IEscalationChainAppService svc) : IRequestHandler<CreateEscalationChainCommand, EscalationChainDto>
{
    public Task<EscalationChainDto> Handle(CreateEscalationChainCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, ct);
}
public class PublishEscalationChainHandler(IEscalationChainAppService svc) : IRequestHandler<PublishEscalationChainCommand, Unit>
{
    public async Task<Unit> Handle(PublishEscalationChainCommand request, CancellationToken ct) { await svc.PublishAsync(request.ChainId, ct); return Unit.Value; }
}
public class EvaluateApprovalHandler(IApprovalWorkflowEvaluatorService svc) : IRequestHandler<EvaluateApprovalCommand, ApprovalDecisionDto>
{
    public Task<ApprovalDecisionDto> Handle(EvaluateApprovalCommand request, CancellationToken ct) => svc.EvaluateAsync(request.Request, ct);
}
public class ResolveEscalationHandler(IEscalationResolverService svc) : IRequestHandler<ResolveEscalationCommand, EscalationResolutionDto>
{
    public Task<EscalationResolutionDto> Handle(ResolveEscalationCommand request, CancellationToken ct) => svc.ResolveAsync(request.Request, ct);
}
