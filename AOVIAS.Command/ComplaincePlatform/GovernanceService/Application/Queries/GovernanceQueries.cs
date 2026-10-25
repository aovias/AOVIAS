using GovernanceService.Application.Contracts;
using GovernanceService.Application.Services;
using MediatR;

namespace GovernanceService.Application.Queries;

public record ListResponsibilitiesQuery(int Page, int PageSize) : IRequest<PagedResultDto<ResponsibilityDto>>;
public record GetResponsibilityByIdQuery(Guid Id) : IRequest<ResponsibilityDto?>;
public record ListCommitteesQuery() : IRequest<IReadOnlyList<CommitteeDto>>;
public record GetCommitteeByIdQuery(Guid Id) : IRequest<CommitteeDto?>;
public record ListApprovalChainsQuery() : IRequest<IReadOnlyList<ApprovalChainDto>>;
public record ListEscalationChainsQuery() : IRequest<IReadOnlyList<EscalationChainDto>>;
public record GetEscalationChainByIdQuery(Guid Id) : IRequest<EscalationChainDto?>;
public record GetGovernanceTimelineQuery(string? ResourceType, Guid? ResourceId, Guid? OrganizationId) : IRequest<IReadOnlyList<GovernanceTimelineDto>>;
public record GetGovernanceAnalyticsQuery() : IRequest<GovernanceAnalyticsDto>;
public record GetGovernanceKpisQuery() : IRequest<GovernanceKpisDto>;
public record GetGovernanceLookupsQuery(string Category) : IRequest<IReadOnlyList<LookupValueDto>>;
public record ListCommitteeMeetingsQuery(Guid CommitteeId) : IRequest<IReadOnlyList<CommitteeMeetingDto>>;
public record GetCommitteeMeetingQuery(Guid CommitteeId, Guid MeetingId) : IRequest<CommitteeMeetingDto?>;
public record ListAgendaTemplatesQuery() : IRequest<IReadOnlyList<AgendaTemplateDto>>;
public record GetManagementReviewAgendaQuery(Guid? CommitteeId) : IRequest<ManagementReviewAgendaInputDto>;

public class ListResponsibilitiesHandler(IResponsibilityAppService svc) : IRequestHandler<ListResponsibilitiesQuery, PagedResultDto<ResponsibilityDto>>
{
    public Task<PagedResultDto<ResponsibilityDto>> Handle(ListResponsibilitiesQuery request, CancellationToken ct) => svc.ListAsync(request.Page, request.PageSize, ct);
}
public class GetResponsibilityByIdHandler(IResponsibilityAppService svc) : IRequestHandler<GetResponsibilityByIdQuery, ResponsibilityDto?>
{
    public Task<ResponsibilityDto?> Handle(GetResponsibilityByIdQuery request, CancellationToken ct) => svc.GetByIdAsync(request.Id, ct);
}
public class ListCommitteesHandler(ICommitteeAppService svc) : IRequestHandler<ListCommitteesQuery, IReadOnlyList<CommitteeDto>>
{
    public Task<IReadOnlyList<CommitteeDto>> Handle(ListCommitteesQuery request, CancellationToken ct) => svc.ListAsync(ct);
}
public class GetCommitteeByIdHandler(ICommitteeAppService svc) : IRequestHandler<GetCommitteeByIdQuery, CommitteeDto?>
{
    public Task<CommitteeDto?> Handle(GetCommitteeByIdQuery request, CancellationToken ct) => svc.GetByIdAsync(request.Id, ct);
}
public class ListApprovalChainsHandler(IApprovalChainAppService svc) : IRequestHandler<ListApprovalChainsQuery, IReadOnlyList<ApprovalChainDto>>
{
    public Task<IReadOnlyList<ApprovalChainDto>> Handle(ListApprovalChainsQuery request, CancellationToken ct) => svc.ListAsync(ct);
}
public class ListEscalationChainsHandler(IEscalationChainAppService svc) : IRequestHandler<ListEscalationChainsQuery, IReadOnlyList<EscalationChainDto>>
{
    public Task<IReadOnlyList<EscalationChainDto>> Handle(ListEscalationChainsQuery request, CancellationToken ct) => svc.ListAsync(ct);
}
public class GetEscalationChainByIdHandler(IEscalationChainAppService svc) : IRequestHandler<GetEscalationChainByIdQuery, EscalationChainDto?>
{
    public Task<EscalationChainDto?> Handle(GetEscalationChainByIdQuery request, CancellationToken ct) => svc.GetByIdAsync(request.Id, ct);
}
public class GetGovernanceTimelineHandler(IGovernanceTimelineService svc) : IRequestHandler<GetGovernanceTimelineQuery, IReadOnlyList<GovernanceTimelineDto>>
{
    public Task<IReadOnlyList<GovernanceTimelineDto>> Handle(GetGovernanceTimelineQuery request, CancellationToken ct) => svc.SearchAsync(request.ResourceType, request.ResourceId, request.OrganizationId, ct);
}
public class GetGovernanceAnalyticsHandler(IGovernanceAnalyticsService svc) : IRequestHandler<GetGovernanceAnalyticsQuery, GovernanceAnalyticsDto>
{
    public Task<GovernanceAnalyticsDto> Handle(GetGovernanceAnalyticsQuery request, CancellationToken ct) => svc.GetSummaryAsync(ct);
}
public class GetGovernanceKpisHandler(IGovernanceKpiService svc) : IRequestHandler<GetGovernanceKpisQuery, GovernanceKpisDto>
{
    public Task<GovernanceKpisDto> Handle(GetGovernanceKpisQuery request, CancellationToken ct) => svc.GetKpisAsync(ct);
}
public class GetGovernanceLookupsHandler(LookupAppService svc) : IRequestHandler<GetGovernanceLookupsQuery, IReadOnlyList<LookupValueDto>>
{
    public Task<IReadOnlyList<LookupValueDto>> Handle(GetGovernanceLookupsQuery request, CancellationToken ct) => svc.GetByCategoryAsync(request.Category, ct);
}
public class ListCommitteeMeetingsHandler(ICommitteeAppService svc) : IRequestHandler<ListCommitteeMeetingsQuery, IReadOnlyList<CommitteeMeetingDto>>
{
    public Task<IReadOnlyList<CommitteeMeetingDto>> Handle(ListCommitteeMeetingsQuery request, CancellationToken ct)
        => svc.ListMeetingsAsync(request.CommitteeId, ct);
}
public class GetCommitteeMeetingHandler(ICommitteeAppService svc) : IRequestHandler<GetCommitteeMeetingQuery, CommitteeMeetingDto?>
{
    public Task<CommitteeMeetingDto?> Handle(GetCommitteeMeetingQuery request, CancellationToken ct)
        => svc.GetMeetingAsync(request.CommitteeId, request.MeetingId, ct);
}
public class ListAgendaTemplatesHandler(ICommitteeAppService svc) : IRequestHandler<ListAgendaTemplatesQuery, IReadOnlyList<AgendaTemplateDto>>
{
    public Task<IReadOnlyList<AgendaTemplateDto>> Handle(ListAgendaTemplatesQuery request, CancellationToken ct)
        => svc.ListAgendaTemplatesAsync(ct);
}
public class GetManagementReviewAgendaHandler(ICommitteeAppService svc) : IRequestHandler<GetManagementReviewAgendaQuery, ManagementReviewAgendaInputDto>
{
    public Task<ManagementReviewAgendaInputDto> Handle(GetManagementReviewAgendaQuery request, CancellationToken ct)
        => svc.GetManagementReviewAgendaAsync(request.CommitteeId, ct);
}
