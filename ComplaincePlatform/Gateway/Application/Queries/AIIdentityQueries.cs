using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record ListAIProfilesQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetAIProfileQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record UpsertAIProfileQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record ListAIAutomationPoliciesQuery(string ApiVersion, Guid? ProfileId) : IRequest<IActionResult>;
public sealed record UpsertAIAutomationPolicyQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record ListAIPromptsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record UpsertAIPromptQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record EvaluateAIPromptQuery(string ApiVersion, object Body) : IRequest<IActionResult>;
public sealed record ListAIExecutionsQuery(string ApiVersion, int Page, int PageSize, Guid? ProfileId) : IRequest<IActionResult>;
public sealed record GetAIAnalyticsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record ListAIApprovalsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GrantAIApprovalQuery(string ApiVersion, Guid Id) : IRequest<IActionResult>;
public sealed record ListAIProfilePermissionsQuery(string ApiVersion, Guid ProfileId) : IRequest<IActionResult>;
