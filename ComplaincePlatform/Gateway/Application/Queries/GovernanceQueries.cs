using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record ListGovernanceResponsibilitiesQuery(string ApiVersion, int Page, int PageSize) : IRequest<IActionResult>;
public record GetGovernanceResponsibilityQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public record ListCommitteesQuery(string ApiVersion) : IRequest<IActionResult>;
public record GetCommitteeQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public record ListGovernanceApprovalChainsQuery(string ApiVersion) : IRequest<IActionResult>;
public record ListEscalationChainsQuery(string ApiVersion) : IRequest<IActionResult>;
public record GetEscalationChainQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public record GetGovernanceTimelineQuery(string ApiVersion, string? ResourceType, Guid? ResourceId, Guid? OrganizationId) : IRequest<IActionResult>;
public record GetGovernanceAnalyticsQuery(string ApiVersion) : IRequest<IActionResult>;
public record GetGovernanceKpisQuery(string ApiVersion) : IRequest<IActionResult>;
public record GetGovernanceLookupsQuery(string ApiVersion, string Category) : IRequest<IActionResult>;
public record ListCommitteeMeetingsQuery(string ApiVersion, Guid CommitteeId) : IRequest<IActionResult>;
public record GetCommitteeMeetingQuery(string ApiVersion, Guid CommitteeId, Guid MeetingId) : IRequest<IActionResult>;
public record ListAgendaTemplatesQuery(string ApiVersion) : IRequest<IActionResult>;
public record GetManagementReviewAgendaQuery(string ApiVersion, Guid? CommitteeId) : IRequest<IActionResult>;
