using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

/// <summary>
/// Proxies the compliance maturity dashboard request to ComplianceService.
/// The JWT hospital_id and organization_id claims are forwarded automatically
/// by <see cref="IProxyService"/>.
/// </summary>
public sealed class GetComplianceMaturityDashboardQueryHandler
    : IRequestHandler<GetComplianceMaturityDashboardQuery, IActionResult>
{
    private readonly IProxyService _proxy;

    public GetComplianceMaturityDashboardQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(
        GetComplianceMaturityDashboardQuery request,
        CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            endpoint: $"/api/v{request.ApiVersion}/compliance/maturity-dashboard",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            additionalHeaders: null,
            cancellationToken: cancellationToken);
}
