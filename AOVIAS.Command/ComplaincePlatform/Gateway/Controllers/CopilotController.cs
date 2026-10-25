using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Gateway entry point for the Compliance Copilot (Sprint 13A + 13B + 13C).
/// Proxies Copilot queries and commands to ComplianceService.
/// Sprint 13C adds: POST drafts/generate, GET drafts/{id}/versions, POST drafts/{id}/regenerate.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/copilot")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class CopilotController : ControllerBase
{
    private readonly IMediator _mediator;

    public CopilotController(IMediator mediator) => _mediator = mediator;

    private string ApiVersion => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";

    // ── Recommendations ───────────────────────────────────────────────────────

    [HttpGet("recommendations")]
    public Task<IActionResult> GetRecommendations(CancellationToken cancellationToken)
    {
        var qs = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value : null;
        return _mediator.Send(new GetCopilotRecommendationsQuery(ApiVersion, qs), cancellationToken);
    }

    [HttpGet("recommendations/persona/{persona}")]
    public Task<IActionResult> GetRecommendationsByPersona(string persona, CancellationToken cancellationToken)
    {
        var qs = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value : null;
        return _mediator.Send(new GetCopilotRecommendationsByPersonaQuery(ApiVersion, persona, qs), cancellationToken);
    }

    [HttpGet("recommendations/{id:guid}")]
    public Task<IActionResult> GetRecommendation(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotRecommendationQuery(ApiVersion, id), cancellationToken);

    [HttpPost("recommendations/refresh")]
    public async Task<IActionResult> RefreshRecommendations(CancellationToken cancellationToken)
    {
        string? body = null;
        if (Request.ContentLength > 0)
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = await reader.ReadToEndAsync(cancellationToken);

        return await _mediator.Send(new RefreshCopilotRecommendationsCommand(ApiVersion, body), cancellationToken);
    }

    // ── Drafts ────────────────────────────────────────────────────────────────

    [HttpGet("drafts")]
    public Task<IActionResult> GetDrafts(CancellationToken cancellationToken)
    {
        var qs = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value : null;
        return _mediator.Send(new GetCopilotDraftsQuery(ApiVersion, qs), cancellationToken);
    }

    [HttpGet("drafts/{id:guid}")]
    public Task<IActionResult> GetDraft(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotDraftQuery(ApiVersion, id), cancellationToken);

    [HttpGet("drafts/{id:guid}/versions")]
    public Task<IActionResult> GetDraftVersions(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotDraftVersionsQuery(ApiVersion, id), cancellationToken);

    [HttpPost("drafts/generate")]
    public async Task<IActionResult> GenerateDraft(CancellationToken cancellationToken)
    {
        string? body = null;
        if (Request.ContentLength > 0)
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = await reader.ReadToEndAsync(cancellationToken);

        return await _mediator.Send(new GenerateCopilotDraftCommand(ApiVersion, body), cancellationToken);
    }

    [HttpPost("drafts/{id:guid}/regenerate")]
    public Task<IActionResult> RegenerateDraft(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new RegenerateCopilotDraftCommand(ApiVersion, id), cancellationToken);

    // ── Sprint 13D: Action Proposals ─────────────────────────────────────────

    [HttpGet("proposals")]
    public Task<IActionResult> GetProposals(CancellationToken cancellationToken)
    {
        var qs = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value : null;
        return _mediator.Send(new GetCopilotProposalsQuery(ApiVersion, qs), cancellationToken);
    }

    [HttpGet("proposals/pending")]
    public Task<IActionResult> GetPendingProposals(CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotPendingProposalsQuery(ApiVersion), cancellationToken);

    [HttpGet("proposals/{id:guid}")]
    public Task<IActionResult> GetProposal(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotProposalQuery(ApiVersion, id), cancellationToken);

    [HttpGet("proposals/{id:guid}/audit-trail")]
    public Task<IActionResult> GetProposalAuditTrail(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotProposalAuditTrailQuery(ApiVersion, id), cancellationToken);

    [HttpPost("proposals")]
    public async Task<IActionResult> CreateProposal(CancellationToken cancellationToken)
    {
        string? body = null;
        if (Request.ContentLength > 0)
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = await reader.ReadToEndAsync(cancellationToken);
        return await _mediator.Send(new CreateCopilotProposalCommand(ApiVersion, body), cancellationToken);
    }

    [HttpPost("proposals/from-recommendation/{id:guid}")]
    public async Task<IActionResult> GenerateProposalFromRecommendation(
        Guid id, CancellationToken cancellationToken)
    {
        string? body = null;
        if (Request.ContentLength > 0)
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = await reader.ReadToEndAsync(cancellationToken);
        return await _mediator.Send(
            new GenerateCopilotProposalFromRecommendationCommand(ApiVersion, id, body), cancellationToken);
    }

    [HttpPost("proposals/from-draft/{id:guid}")]
    public async Task<IActionResult> GenerateProposalFromDraft(
        Guid id, CancellationToken cancellationToken)
    {
        string? body = null;
        if (Request.ContentLength > 0)
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = await reader.ReadToEndAsync(cancellationToken);
        return await _mediator.Send(
            new GenerateCopilotProposalFromDraftCommand(ApiVersion, id, body), cancellationToken);
    }

    [HttpPost("proposals/{id:guid}/approve")]
    public async Task<IActionResult> ApproveProposal(Guid id, CancellationToken cancellationToken)
    {
        string? body = null;
        if (Request.ContentLength > 0)
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = await reader.ReadToEndAsync(cancellationToken);
        return await _mediator.Send(
            new ApproveCopilotProposalCommand(ApiVersion, id, body), cancellationToken);
    }

    [HttpPost("proposals/{id:guid}/reject")]
    public async Task<IActionResult> RejectProposal(Guid id, CancellationToken cancellationToken)
    {
        string? body = null;
        if (Request.ContentLength > 0)
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = await reader.ReadToEndAsync(cancellationToken);
        return await _mediator.Send(
            new RejectCopilotProposalCommand(ApiVersion, id, body), cancellationToken);
    }

    [HttpPost("proposals/{id:guid}/execute")]
    public Task<IActionResult> ExecuteProposal(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new ExecuteCopilotProposalCommand(ApiVersion, id), cancellationToken);

    // ── Personas ──────────────────────────────────────────────────────────────

    [HttpGet("personas")]
    public Task<IActionResult> GetPersonas(CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotPersonasQuery(ApiVersion), cancellationToken);

    [HttpGet("personas/active")]
    public Task<IActionResult> GetActivePersona(CancellationToken cancellationToken)
    {
        var qs = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value : null;
        return _mediator.Send(new GetCopilotActivePersonaQuery(ApiVersion, qs), cancellationToken);
    }

    // ── Sprint 13E: Persona Experience ────────────────────────────────────────

    [HttpGet("experience/catalog")]
    public Task<IActionResult> GetExperienceCatalog(CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotExperienceCatalogQuery(ApiVersion), cancellationToken);

    [HttpGet("experience/workspace")]
    public Task<IActionResult> GetExperienceWorkspace(CancellationToken cancellationToken)
    {
        var qs = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value : null;
        return _mediator.Send(new GetCopilotExperienceWorkspaceQuery(ApiVersion, qs), cancellationToken);
    }

    [HttpGet("activity")]
    public Task<IActionResult> GetActivity(CancellationToken cancellationToken)
    {
        var qs = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value : null;
        return _mediator.Send(new GetCopilotActivityQuery(ApiVersion, qs), cancellationToken);
    }

    [HttpGet("usage-metrics")]
    public Task<IActionResult> GetUsageMetrics(CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotUsageMetricsQuery(ApiVersion), cancellationToken);

    [HttpPost("activity")]
    public async Task<IActionResult> RecordActivity(CancellationToken cancellationToken)
    {
        using var reader = new System.IO.StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
            body = "{}";

        return await _mediator.Send(new RecordCopilotActivityCommand(ApiVersion, body), cancellationToken);
    }

    // ── Sprint 13F: Observability Analytics ───────────────────────────────────

    [HttpGet("analytics/overview")]
    public Task<IActionResult> GetAnalyticsOverview(CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotAnalyticsOverviewQuery(ApiVersion), cancellationToken);

    [HttpGet("analytics/recommendations")]
    public Task<IActionResult> GetAnalyticsRecommendations(CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotAnalyticsRecommendationsQuery(ApiVersion), cancellationToken);

    [HttpGet("analytics/drafts")]
    public Task<IActionResult> GetAnalyticsDrafts(CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotAnalyticsDraftsQuery(ApiVersion), cancellationToken);

    [HttpGet("analytics/proposals")]
    public Task<IActionResult> GetAnalyticsProposals(CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotAnalyticsProposalsQuery(ApiVersion), cancellationToken);

    [HttpGet("analytics/personas")]
    public Task<IActionResult> GetAnalyticsPersonas(CancellationToken cancellationToken) =>
        _mediator.Send(new GetCopilotAnalyticsPersonasQuery(ApiVersion), cancellationToken);

    [HttpGet("analytics/trends")]
    public Task<IActionResult> GetAnalyticsTrends(CancellationToken cancellationToken)
    {
        var qs = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value : null;
        return _mediator.Send(new GetCopilotAnalyticsTrendsQuery(ApiVersion, qs), cancellationToken);
    }

    [HttpGet("analytics/impact")]
    public Task<IActionResult> GetAnalyticsImpact(CancellationToken cancellationToken)
    {
        var qs = HttpContext.Request.QueryString.HasValue
            ? HttpContext.Request.QueryString.Value : null;
        return _mediator.Send(new GetCopilotAnalyticsImpactQuery(ApiVersion, qs), cancellationToken);
    }
}
