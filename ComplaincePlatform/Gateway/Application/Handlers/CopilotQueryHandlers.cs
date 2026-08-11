using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

/// <summary>Gateway proxy handlers for the Copilot API (Sprint 13A + 13B + 13C).</summary>

public sealed class GetCopilotRecommendationsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotRecommendationsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotRecommendationsQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/copilot/recommendations"
            : $"/api/v{request.ApiVersion}/copilot/recommendations?{request.QueryString.TrimStart('?')}";

        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetCopilotRecommendationsByPersonaQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotRecommendationsByPersonaQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotRecommendationsByPersonaQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/copilot/recommendations/persona/{request.Persona}"
            : $"/api/v{request.ApiVersion}/copilot/recommendations/persona/{request.Persona}?{request.QueryString.TrimStart('?')}";

        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class RefreshCopilotRecommendationsCommandHandler(IProxyService proxy)
    : IRequestHandler<RefreshCopilotRecommendationsCommand, IActionResult>
{
    public Task<IActionResult> Handle(RefreshCopilotRecommendationsCommand request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/recommendations/refresh",
            HttpMethod.Post,
            request.Body,
            null,
            cancellationToken);
}

public sealed class GetCopilotRecommendationQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotRecommendationQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotRecommendationQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/recommendations/{request.Id}",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetCopilotDraftsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotDraftsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotDraftsQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/copilot/drafts"
            : $"/api/v{request.ApiVersion}/copilot/drafts?{request.QueryString.TrimStart('?')}";

        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetCopilotDraftQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotDraftQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotDraftQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/drafts/{request.Id}",
            HttpMethod.Get, null, null, cancellationToken);
}

// ── Sprint 13C: draft generation + versioning ────────────────────────────────

public sealed class GetCopilotDraftVersionsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotDraftVersionsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotDraftVersionsQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/drafts/{request.Id}/versions",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GenerateCopilotDraftCommandHandler(IProxyService proxy)
    : IRequestHandler<GenerateCopilotDraftCommand, IActionResult>
{
    public Task<IActionResult> Handle(GenerateCopilotDraftCommand request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/drafts/generate",
            HttpMethod.Post,
            request.Body,
            null,
            cancellationToken);
}

public sealed class RegenerateCopilotDraftCommandHandler(IProxyService proxy)
    : IRequestHandler<RegenerateCopilotDraftCommand, IActionResult>
{
    public Task<IActionResult> Handle(RegenerateCopilotDraftCommand request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/drafts/{request.Id}/regenerate",
            HttpMethod.Post, null, null, cancellationToken);
}

// ── Sprint 13D: action proposals ─────────────────────────────────────────────

public sealed class GetCopilotProposalsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotProposalsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotProposalsQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/copilot/proposals"
            : $"/api/v{request.ApiVersion}/copilot/proposals?{request.QueryString.TrimStart('?')}";
        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetCopilotPendingProposalsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotPendingProposalsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotPendingProposalsQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/proposals/pending",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetCopilotProposalQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotProposalQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotProposalQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/proposals/{request.Id}",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetCopilotProposalAuditTrailQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotProposalAuditTrailQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotProposalAuditTrailQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/proposals/{request.Id}/audit-trail",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class CreateCopilotProposalCommandHandler(IProxyService proxy)
    : IRequestHandler<CreateCopilotProposalCommand, IActionResult>
{
    public Task<IActionResult> Handle(CreateCopilotProposalCommand request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/proposals",
            HttpMethod.Post, request.Body, null, cancellationToken);
}

public sealed class GenerateCopilotProposalFromRecommendationCommandHandler(IProxyService proxy)
    : IRequestHandler<GenerateCopilotProposalFromRecommendationCommand, IActionResult>
{
    public Task<IActionResult> Handle(GenerateCopilotProposalFromRecommendationCommand request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/proposals/from-recommendation/{request.RecommendationId}",
            HttpMethod.Post, request.Body, null, cancellationToken);
}

public sealed class GenerateCopilotProposalFromDraftCommandHandler(IProxyService proxy)
    : IRequestHandler<GenerateCopilotProposalFromDraftCommand, IActionResult>
{
    public Task<IActionResult> Handle(GenerateCopilotProposalFromDraftCommand request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/proposals/from-draft/{request.DraftId}",
            HttpMethod.Post, request.Body, null, cancellationToken);
}

public sealed class ApproveCopilotProposalCommandHandler(IProxyService proxy)
    : IRequestHandler<ApproveCopilotProposalCommand, IActionResult>
{
    public Task<IActionResult> Handle(ApproveCopilotProposalCommand request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/proposals/{request.Id}/approve",
            HttpMethod.Post, request.Body, null, cancellationToken);
}

public sealed class RejectCopilotProposalCommandHandler(IProxyService proxy)
    : IRequestHandler<RejectCopilotProposalCommand, IActionResult>
{
    public Task<IActionResult> Handle(RejectCopilotProposalCommand request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/proposals/{request.Id}/reject",
            HttpMethod.Post, request.Body, null, cancellationToken);
}

public sealed class ExecuteCopilotProposalCommandHandler(IProxyService proxy)
    : IRequestHandler<ExecuteCopilotProposalCommand, IActionResult>
{
    public Task<IActionResult> Handle(ExecuteCopilotProposalCommand request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/proposals/{request.Id}/execute",
            HttpMethod.Post, null, null, cancellationToken);
}

// ── Personas ──────────────────────────────────────────────────────────────────

public sealed class GetCopilotPersonasQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotPersonasQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotPersonasQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/personas",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetCopilotActivePersonaQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotActivePersonaQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotActivePersonaQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/copilot/personas/active"
            : $"/api/v{request.ApiVersion}/copilot/personas/active?{request.QueryString.TrimStart('?')}";

        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

// ── Sprint 13E: persona experience layer ─────────────────────────────────────

public sealed class GetCopilotExperienceCatalogQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotExperienceCatalogQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotExperienceCatalogQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/experience/catalog",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetCopilotExperienceWorkspaceQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotExperienceWorkspaceQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotExperienceWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/copilot/experience/workspace"
            : $"/api/v{request.ApiVersion}/copilot/experience/workspace?{request.QueryString.TrimStart('?')}";

        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetCopilotActivityQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotActivityQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotActivityQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/copilot/activity"
            : $"/api/v{request.ApiVersion}/copilot/activity?{request.QueryString.TrimStart('?')}";

        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetCopilotUsageMetricsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotUsageMetricsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotUsageMetricsQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/usage-metrics",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class RecordCopilotActivityCommandHandler(IProxyService proxy)
    : IRequestHandler<RecordCopilotActivityCommand, IActionResult>
{
    public Task<IActionResult> Handle(RecordCopilotActivityCommand request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/activity",
            HttpMethod.Post, request.Body, null, cancellationToken);
}

// ── Sprint 13F: observability analytics ─────────────────────────────────────

public sealed class GetCopilotAnalyticsOverviewQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotAnalyticsOverviewQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotAnalyticsOverviewQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/analytics/overview",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetCopilotAnalyticsRecommendationsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotAnalyticsRecommendationsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotAnalyticsRecommendationsQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/analytics/recommendations",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetCopilotAnalyticsDraftsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotAnalyticsDraftsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotAnalyticsDraftsQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/analytics/drafts",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetCopilotAnalyticsProposalsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotAnalyticsProposalsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotAnalyticsProposalsQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/analytics/proposals",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetCopilotAnalyticsPersonasQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotAnalyticsPersonasQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotAnalyticsPersonasQuery request, CancellationToken cancellationToken) =>
        proxy.ProxyToComplianceServiceAsync(
            $"/api/v{request.ApiVersion}/copilot/analytics/personas",
            HttpMethod.Get, null, null, cancellationToken);
}

public sealed class GetCopilotAnalyticsTrendsQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotAnalyticsTrendsQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotAnalyticsTrendsQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/copilot/analytics/trends"
            : $"/api/v{request.ApiVersion}/copilot/analytics/trends?{request.QueryString.TrimStart('?')}";
        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetCopilotAnalyticsImpactQueryHandler(IProxyService proxy)
    : IRequestHandler<GetCopilotAnalyticsImpactQuery, IActionResult>
{
    public Task<IActionResult> Handle(GetCopilotAnalyticsImpactQuery request, CancellationToken cancellationToken)
    {
        var endpoint = string.IsNullOrWhiteSpace(request.QueryString)
            ? $"/api/v{request.ApiVersion}/copilot/analytics/impact"
            : $"/api/v{request.ApiVersion}/copilot/analytics/impact?{request.QueryString.TrimStart('?')}";
        return proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}
