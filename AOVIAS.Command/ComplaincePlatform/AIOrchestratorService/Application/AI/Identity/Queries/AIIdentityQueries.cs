using AIOrchestratorService.Application.AI.Identity.Contracts;
using MediatR;

namespace AIOrchestratorService.Application.AI.Identity.Queries;

public sealed record ListAIProfilesQuery : IRequest<IReadOnlyList<AIProfileDto>>;
public sealed record GetAIProfileQuery(Guid Id) : IRequest<AIProfileDto?>;
public sealed record ListAutomationPoliciesQuery(Guid? ProfileId) : IRequest<IReadOnlyList<AIAutomationPolicyDto>>;
public sealed record ListPromptProfilesQuery : IRequest<IReadOnlyList<AIPromptProfileDto>>;
public sealed record ListExecutionsQuery(int Page, int PageSize, Guid? ProfileId) : IRequest<PagedResult<AIExecutionAuditDto>>;
public sealed record GetExecutionQuery(Guid Id) : IRequest<AIExecutionAuditDto?>;
public sealed record GetAIAnalyticsQuery : IRequest<AIAnalyticsDto>;
public sealed record ListPendingApprovalsQuery : IRequest<IReadOnlyList<AIApprovalDto>>;
public sealed record ListProfilePermissionsQuery(Guid ProfileId) : IRequest<IReadOnlyList<ProfilePermissionDto>>;
public sealed record GetProfileByCopilotPersonaQuery(string PersonaKey) : IRequest<AIProfileDto?>;
