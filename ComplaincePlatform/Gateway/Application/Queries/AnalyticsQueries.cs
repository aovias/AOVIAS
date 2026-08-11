using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record AnalyticsProxyQuery(string ApiVersion, string EndpointSuffix) : IRequest<IActionResult>;

public sealed record GetAnalyticsOverviewQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetAnalyticsExecutiveQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetAnalyticsOperationalQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetAnalyticsRcaQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetAnalyticsCapaQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetAnalyticsSlaQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetAnalyticsDepartmentsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetAnalyticsTrendsQuery(string ApiVersion, string? QueryString) : IRequest<IActionResult>;
public sealed record GetAnalyticsRoleEngagementQuery(string ApiVersion, string? QueryString) : IRequest<IActionResult>;
public sealed record GetAnalyticsGovernanceQuery(string ApiVersion) : IRequest<IActionResult>;
