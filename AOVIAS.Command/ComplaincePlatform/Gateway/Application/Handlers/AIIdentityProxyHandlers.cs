using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public class AIIdentityProxyHandlers
{
    public class ListAIProfilesHandler(IProxyService proxy) : IRequestHandler<ListAIProfilesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListAIProfilesQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/profiles", HttpMethod.Get, cancellationToken: ct);
    }

    public class GetAIProfileHandler(IProxyService proxy) : IRequestHandler<GetAIProfileQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetAIProfileQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/profiles/{request.Id}", HttpMethod.Get, cancellationToken: ct);
    }

    public class UpsertAIProfileHandler(IProxyService proxy) : IRequestHandler<UpsertAIProfileQuery, IActionResult>
    {
        public Task<IActionResult> Handle(UpsertAIProfileQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/profiles", HttpMethod.Post, request.Body, cancellationToken: ct);
    }

    public class ListAIAutomationPoliciesHandler(IProxyService proxy) : IRequestHandler<ListAIAutomationPoliciesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListAIAutomationPoliciesQuery request, CancellationToken ct)
        {
            var qs = request.ProfileId.HasValue ? $"?profileId={request.ProfileId}" : "";
            return proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/policies{qs}", HttpMethod.Get, cancellationToken: ct);
        }
    }

    public class UpsertAIAutomationPolicyHandler(IProxyService proxy) : IRequestHandler<UpsertAIAutomationPolicyQuery, IActionResult>
    {
        public Task<IActionResult> Handle(UpsertAIAutomationPolicyQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/policies", HttpMethod.Post, request.Body, cancellationToken: ct);
    }

    public class ListAIPromptsHandler(IProxyService proxy) : IRequestHandler<ListAIPromptsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListAIPromptsQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/prompts", HttpMethod.Get, cancellationToken: ct);
    }

    public class UpsertAIPromptHandler(IProxyService proxy) : IRequestHandler<UpsertAIPromptQuery, IActionResult>
    {
        public Task<IActionResult> Handle(UpsertAIPromptQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/prompts", HttpMethod.Post, request.Body, cancellationToken: ct);
    }

    public class EvaluateAIPromptHandler(IProxyService proxy) : IRequestHandler<EvaluateAIPromptQuery, IActionResult>
    {
        public Task<IActionResult> Handle(EvaluateAIPromptQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/prompts/evaluate", HttpMethod.Post, request.Body, cancellationToken: ct);
    }

    public class ListAIExecutionsHandler(IProxyService proxy) : IRequestHandler<ListAIExecutionsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListAIExecutionsQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/executions?page={request.Page}&pageSize={request.PageSize}" + (request.ProfileId.HasValue ? $"&profileId={request.ProfileId}" : ""), HttpMethod.Get, cancellationToken: ct);
    }

    public class GetAIAnalyticsHandler(IProxyService proxy) : IRequestHandler<GetAIAnalyticsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetAIAnalyticsQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/analytics", HttpMethod.Get, cancellationToken: ct);
    }

    public class ListAIApprovalsHandler(IProxyService proxy) : IRequestHandler<ListAIApprovalsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListAIApprovalsQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/approvals", HttpMethod.Get, cancellationToken: ct);
    }

    public class GrantAIApprovalHandler(IProxyService proxy) : IRequestHandler<GrantAIApprovalQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GrantAIApprovalQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/approvals/{request.Id}/grant", HttpMethod.Post, cancellationToken: ct);
    }

    public class ListAIProfilePermissionsHandler(IProxyService proxy) : IRequestHandler<ListAIProfilePermissionsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListAIProfilePermissionsQuery request, CancellationToken ct)
            => proxy.ProxyToAIServiceAsync($"/api/v{request.ApiVersion}/ai/profiles/{request.ProfileId}/permissions", HttpMethod.Get, cancellationToken: ct);
    }
}
