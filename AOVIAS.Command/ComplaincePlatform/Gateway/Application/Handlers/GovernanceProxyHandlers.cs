using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public class GovernanceProxyHandlers
{
    public class ListGovernanceResponsibilitiesHandler(IProxyService proxy) : IRequestHandler<ListGovernanceResponsibilitiesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListGovernanceResponsibilitiesQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/responsibilities?page={request.Page}&pageSize={request.PageSize}", HttpMethod.Get, cancellationToken: ct);
    }

    public class GetGovernanceResponsibilityHandler(IProxyService proxy) : IRequestHandler<GetGovernanceResponsibilityQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetGovernanceResponsibilityQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/responsibilities/{request.Id}", HttpMethod.Get, cancellationToken: ct);
    }

    public class CreateGovernanceResponsibilityHandler(IProxyService proxy) : IRequestHandler<CreateGovernanceResponsibilityCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateGovernanceResponsibilityCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/responsibilities", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class UpdateGovernanceResponsibilityHandler(IProxyService proxy) : IRequestHandler<UpdateGovernanceResponsibilityCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateGovernanceResponsibilityCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/responsibilities/{request.Id}", HttpMethod.Put, request.RequestBody, cancellationToken: ct);
    }

    public class DeleteGovernanceResponsibilityHandler(IProxyService proxy) : IRequestHandler<DeleteGovernanceResponsibilityCommand, IActionResult>
    {
        public Task<IActionResult> Handle(DeleteGovernanceResponsibilityCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/responsibilities/{request.Id}", HttpMethod.Delete, cancellationToken: ct);
    }

    public class AssignGovernanceResponsibilityHandler(IProxyService proxy) : IRequestHandler<AssignGovernanceResponsibilityCommand, IActionResult>
    {
        public Task<IActionResult> Handle(AssignGovernanceResponsibilityCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/responsibilities/{request.ResponsibilityId}/assign", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class ListCommitteesHandler(IProxyService proxy) : IRequestHandler<ListCommitteesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListCommitteesQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees", HttpMethod.Get, cancellationToken: ct);
    }

    public class GetCommitteeHandler(IProxyService proxy) : IRequestHandler<GetCommitteeQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetCommitteeQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.Id}", HttpMethod.Get, cancellationToken: ct);
    }

    public class CreateCommitteeHandler(IProxyService proxy) : IRequestHandler<CreateCommitteeCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateCommitteeCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class UpdateCommitteeHandler(IProxyService proxy) : IRequestHandler<UpdateCommitteeCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateCommitteeCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.Id}", HttpMethod.Put, request.RequestBody, cancellationToken: ct);
    }

    public class AddCommitteeMemberHandler(IProxyService proxy) : IRequestHandler<AddCommitteeMemberCommand, IActionResult>
    {
        public Task<IActionResult> Handle(AddCommitteeMemberCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/members", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class ScheduleCommitteeMeetingHandler(IProxyService proxy) : IRequestHandler<ScheduleCommitteeMeetingCommand, IActionResult>
    {
        public Task<IActionResult> Handle(ScheduleCommitteeMeetingCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/meetings", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class CompleteCommitteeMeetingHandler(IProxyService proxy) : IRequestHandler<CompleteCommitteeMeetingCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CompleteCommitteeMeetingCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/meetings/{request.MeetingId}/complete", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class RecordCommitteeDecisionHandler(IProxyService proxy) : IRequestHandler<RecordCommitteeDecisionCommand, IActionResult>
    {
        public Task<IActionResult> Handle(RecordCommitteeDecisionCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/meetings/{request.MeetingId}/decisions", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class RecordCommitteeAttendanceHandler(IProxyService proxy) : IRequestHandler<RecordCommitteeAttendanceCommand, IActionResult>
    {
        public Task<IActionResult> Handle(RecordCommitteeAttendanceCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/meetings/{request.MeetingId}/attendance", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class ApproveCommitteeMinutesHandler(IProxyService proxy) : IRequestHandler<ApproveCommitteeMinutesCommand, IActionResult>
    {
        public Task<IActionResult> Handle(ApproveCommitteeMinutesCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/meetings/{request.MeetingId}/approve-minutes", HttpMethod.Post, cancellationToken: ct);
    }

    public class CreateCommitteeActionItemHandler(IProxyService proxy) : IRequestHandler<CreateCommitteeActionItemCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateCommitteeActionItemCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/meetings/{request.MeetingId}/actions", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class LinkCommitteeActionTaskHandler(IProxyService proxy) : IRequestHandler<LinkCommitteeActionTaskCommand, IActionResult>
    {
        public Task<IActionResult> Handle(LinkCommitteeActionTaskCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/meetings/{request.MeetingId}/actions/{request.ActionItemId}/link-task", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class UpdateCommitteeActionStatusHandler(IProxyService proxy) : IRequestHandler<UpdateCommitteeActionStatusCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateCommitteeActionStatusCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/meetings/{request.MeetingId}/actions/{request.ActionItemId}/status", HttpMethod.Patch, request.RequestBody, cancellationToken: ct);
    }

    public class ListCommitteeMeetingsHandler(IProxyService proxy) : IRequestHandler<ListCommitteeMeetingsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListCommitteeMeetingsQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/meetings", HttpMethod.Get, cancellationToken: ct);
    }

    public class GetCommitteeMeetingHandler(IProxyService proxy) : IRequestHandler<GetCommitteeMeetingQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetCommitteeMeetingQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/committees/{request.CommitteeId}/meetings/{request.MeetingId}", HttpMethod.Get, cancellationToken: ct);
    }

    public class GetGovernanceLookupsHandler(IProxyService proxy) : IRequestHandler<GetGovernanceLookupsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetGovernanceLookupsQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/governance/lookups/{Uri.EscapeDataString(request.Category)}", HttpMethod.Get, cancellationToken: ct);
    }

    public class ListAgendaTemplatesHandler(IProxyService proxy) : IRequestHandler<ListAgendaTemplatesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListAgendaTemplatesQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/governance/agenda-templates", HttpMethod.Get, cancellationToken: ct);
    }

    public class GetManagementReviewAgendaHandler(IProxyService proxy) : IRequestHandler<GetManagementReviewAgendaQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetManagementReviewAgendaQuery request, CancellationToken ct)
        {
            var q = $"/api/v{request.ApiVersion}/governance/management-review/agenda";
            if (request.CommitteeId.HasValue) q += $"?committeeId={request.CommitteeId}";
            return proxy.ProxyToGovernanceServiceAsync(q, HttpMethod.Get, cancellationToken: ct);
        }
    }

    public class GetGovernanceKpisHandler(IProxyService proxy) : IRequestHandler<GetGovernanceKpisQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetGovernanceKpisQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/governance/analytics/kpis", HttpMethod.Get, cancellationToken: ct);
    }

    public class ListApprovalChainsHandler(IProxyService proxy) : IRequestHandler<ListGovernanceApprovalChainsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListGovernanceApprovalChainsQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/approval-chains", HttpMethod.Get, cancellationToken: ct);
    }

    public class CreateApprovalChainHandler(IProxyService proxy) : IRequestHandler<CreateApprovalChainCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateApprovalChainCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/approval-chains", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class PublishApprovalChainHandler(IProxyService proxy) : IRequestHandler<PublishApprovalChainCommand, IActionResult>
    {
        public Task<IActionResult> Handle(PublishApprovalChainCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/approval-chains/{request.ChainId}/publish", HttpMethod.Post, cancellationToken: ct);
    }

    public class EvaluateApprovalHandler(IProxyService proxy) : IRequestHandler<EvaluateGovernanceApprovalCommand, IActionResult>
    {
        public Task<IActionResult> Handle(EvaluateGovernanceApprovalCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/approval/evaluate", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class ListEscalationChainsHandler(IProxyService proxy) : IRequestHandler<ListEscalationChainsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListEscalationChainsQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/escalation-chains", HttpMethod.Get, cancellationToken: ct);
    }

    public class GetEscalationChainHandler(IProxyService proxy) : IRequestHandler<GetEscalationChainQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetEscalationChainQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/escalation/{request.Id}", HttpMethod.Get, cancellationToken: ct);
    }

    public class CreateEscalationChainHandler(IProxyService proxy) : IRequestHandler<CreateEscalationChainCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateEscalationChainCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/escalation-chains", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class PublishEscalationChainHandler(IProxyService proxy) : IRequestHandler<PublishEscalationChainCommand, IActionResult>
    {
        public Task<IActionResult> Handle(PublishEscalationChainCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/escalation-chains/{request.ChainId}/publish", HttpMethod.Post, cancellationToken: ct);
    }

    public class ResolveEscalationHandler(IProxyService proxy) : IRequestHandler<ResolveEscalationCommand, IActionResult>
    {
        public Task<IActionResult> Handle(ResolveEscalationCommand request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/escalation/resolve", HttpMethod.Post, request.RequestBody, cancellationToken: ct);
    }

    public class GetGovernanceTimelineHandler(IProxyService proxy) : IRequestHandler<GetGovernanceTimelineQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetGovernanceTimelineQuery request, CancellationToken ct)
        {
            var q = $"/api/v{request.ApiVersion}/governance/timeline?resourceType={Uri.EscapeDataString(request.ResourceType ?? "")}";
            if (request.ResourceId.HasValue) q += $"&resourceId={request.ResourceId}";
            if (request.OrganizationId.HasValue) q += $"&organizationId={request.OrganizationId}";
            return proxy.ProxyToGovernanceServiceAsync(q, HttpMethod.Get, cancellationToken: ct);
        }
    }

    public class GetGovernanceAnalyticsHandler(IProxyService proxy) : IRequestHandler<GetGovernanceAnalyticsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetGovernanceAnalyticsQuery request, CancellationToken ct)
            => proxy.ProxyToGovernanceServiceAsync($"/api/v{request.ApiVersion}/governance/analytics/summary", HttpMethod.Get, cancellationToken: ct);
    }
}
