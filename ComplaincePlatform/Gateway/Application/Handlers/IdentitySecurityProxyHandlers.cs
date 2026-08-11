using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public class IdentitySecurityProxyHandlers
{
    public class ListSecurityEventsHandler(IProxyService proxy) : IRequestHandler<ListSecurityEventsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListSecurityEventsQuery request, CancellationToken cancellationToken)
        {
            var qs = $"?page={request.Page}&pageSize={request.PageSize}";
            if (request.UserId.HasValue) qs += $"&userId={request.UserId}";
            return proxy.ProxyToIdentityServiceAsync($"/api/v{request.ApiVersion}/security/events{qs}", HttpMethod.Get, cancellationToken: cancellationToken);
        }
    }

    public class GetSecurityDashboardHandler(IProxyService proxy) : IRequestHandler<GetSecurityDashboardQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetSecurityDashboardQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToIdentityServiceAsync($"/api/v{request.ApiVersion}/security/dashboard", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class GetUserRiskHandler(IProxyService proxy) : IRequestHandler<GetUserRiskQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetUserRiskQuery request, CancellationToken cancellationToken)
        {
            var qs = request.UserId.HasValue ? $"?userId={request.UserId}" : string.Empty;
            return proxy.ProxyToIdentityServiceAsync($"/api/v{request.ApiVersion}/security/risk{qs}", HttpMethod.Get, cancellationToken: cancellationToken);
        }
    }

    public class ListTrustedDevicesHandler(IProxyService proxy) : IRequestHandler<ListTrustedDevicesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListTrustedDevicesQuery request, CancellationToken cancellationToken)
        {
            var qs = request.UserId.HasValue ? $"?userId={request.UserId}" : string.Empty;
            return proxy.ProxyToIdentityServiceAsync($"/api/v{request.ApiVersion}/security/devices{qs}", HttpMethod.Get, cancellationToken: cancellationToken);
        }
    }

    public class GetSecurityTimelineHandler(IProxyService proxy) : IRequestHandler<GetSecurityTimelineQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetSecurityTimelineQuery request, CancellationToken cancellationToken)
        {
            var qs = $"?take={request.Take}";
            if (request.UserId.HasValue) qs += $"&userId={request.UserId}";
            return proxy.ProxyToIdentityServiceAsync($"/api/v{request.ApiVersion}/security/timeline{qs}", HttpMethod.Get, cancellationToken: cancellationToken);
        }
    }

    public class UpdateSecurityPolicyHandler(IProxyService proxy) : IRequestHandler<UpdateSecurityPolicyCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateSecurityPolicyCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToIdentityServiceAsync($"/api/v{request.ApiVersion}/security/policies", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class RevokeTrustedDeviceHandler(IProxyService proxy) : IRequestHandler<RevokeTrustedDeviceCommand, IActionResult>
    {
        public Task<IActionResult> Handle(RevokeTrustedDeviceCommand request, CancellationToken cancellationToken)
        {
            var qs = request.UserId.HasValue ? $"?userId={request.UserId}" : string.Empty;
            return proxy.ProxyToIdentityServiceAsync($"/api/v{request.ApiVersion}/security/device/revoke{qs}", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
        }
    }

    public class RevokeSecuritySessionHandler(IProxyService proxy) : IRequestHandler<RevokeSecuritySessionCommand, IActionResult>
    {
        public Task<IActionResult> Handle(RevokeSecuritySessionCommand request, CancellationToken cancellationToken)
        {
            var qs = request.UserId.HasValue ? $"?userId={request.UserId}" : string.Empty;
            return proxy.ProxyToIdentityServiceAsync($"/api/v{request.ApiVersion}/security/session/revoke{qs}", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
        }
    }
}
