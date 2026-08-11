using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public sealed class GetAnalyticsOverviewQueryHandler(IProxyService proxy)
    : IRequestHandler<GetAnalyticsOverviewQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetAnalyticsOverviewQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/analytics/overview",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetAnalyticsExecutiveQueryHandler(IProxyService proxy)
    : IRequestHandler<GetAnalyticsExecutiveQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetAnalyticsExecutiveQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/analytics/executive",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetAnalyticsOperationalQueryHandler(IProxyService proxy)
    : IRequestHandler<GetAnalyticsOperationalQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetAnalyticsOperationalQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/analytics/operational",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetAnalyticsRcaQueryHandler(IProxyService proxy)
    : IRequestHandler<GetAnalyticsRcaQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetAnalyticsRcaQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/analytics/rca",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetAnalyticsCapaQueryHandler(IProxyService proxy)
    : IRequestHandler<GetAnalyticsCapaQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetAnalyticsCapaQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/analytics/capa",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetAnalyticsSlaQueryHandler(IProxyService proxy)
    : IRequestHandler<GetAnalyticsSlaQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetAnalyticsSlaQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/analytics/sla",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetAnalyticsDepartmentsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetAnalyticsDepartmentsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetAnalyticsDepartmentsQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/analytics/departments",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetAnalyticsTrendsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetAnalyticsTrendsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetAnalyticsTrendsQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/analytics/trends"
            : $"/api/v{request.ApiVersion}/analytics/trends?{request.QueryString.TrimStart('?')}";

        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetAnalyticsRoleEngagementQueryHandler(IProxyService proxy)
    : IRequestHandler<GetAnalyticsRoleEngagementQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetAnalyticsRoleEngagementQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/analytics/role-engagement"
            : $"/api/v{request.ApiVersion}/analytics/role-engagement?{request.QueryString.TrimStart('?')}";

        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}
