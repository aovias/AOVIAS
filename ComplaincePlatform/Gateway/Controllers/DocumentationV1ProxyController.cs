using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Proxies Quality Documentation &amp; Policy Management (QDMS) endpoints to ComplianceService (Sprint 15).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/documentation")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class DocumentationV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;

    public DocumentationV1ProxyController(IProxyService proxy) => _proxy = proxy;

    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard(CancellationToken ct) => ProxyGet("dashboard", ct);

    [HttpGet("documents")]
    public Task<IActionResult> Documents(CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/documentation/documents{qs}", HttpMethod.Get, null, null, ct);
    }

    [HttpGet("documents/{id:guid}")]
    public Task<IActionResult> GetDocument(Guid id, CancellationToken ct) => ProxyGet($"documents/{id}", ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct) =>
        ProxyPost("", body, ct);

    [HttpPatch("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] object body, CancellationToken ct) =>
        ProxyPatch($"{id}", body, ct);

    [HttpPost("{id:guid}/versions")]
    public Task<IActionResult> CreateVersion(Guid id, [FromBody] object body, CancellationToken ct) =>
        ProxyPost($"{id}/versions", body, ct);

    [HttpGet("{id:guid}/versions")]
    public Task<IActionResult> GetVersions(Guid id, CancellationToken ct) => ProxyGet($"{id}/versions", ct);

    [HttpGet("{id:guid}/compare")]
    public Task<IActionResult> Compare(Guid id, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/documentation/{id}/compare{qs}", HttpMethod.Get, null, null, ct);
    }

    [HttpPost("{id:guid}/transition")]
    public Task<IActionResult> Transition(Guid id, [FromBody] object body, CancellationToken ct) =>
        ProxyPost($"{id}/transition", body, ct);

    [HttpPost("approve")]
    public Task<IActionResult> Approve([FromBody] object body, CancellationToken ct) =>
        ProxyPost("approve", body, ct);

    [HttpPost("publish")]
    public Task<IActionResult> Publish([FromBody] object body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/documentation/publish{qs}", HttpMethod.Post, body, null, ct);
    }

    [HttpPost("acknowledge")]
    public Task<IActionResult> Acknowledge([FromBody] object body, CancellationToken ct) =>
        ProxyPost("acknowledge", body, ct);

    [HttpPost("{id:guid}/reviews/complete")]
    public Task<IActionResult> CompleteReview(Guid id, [FromBody] object body, CancellationToken ct) =>
        ProxyPost($"{id}/reviews/complete", body, ct);

    [HttpGet("acknowledgements")]
    public Task<IActionResult> Acknowledgements(CancellationToken ct) => ProxyGet("acknowledgements", ct);

    [HttpGet("review-calendar")]
    public Task<IActionResult> ReviewCalendar(CancellationToken ct) => ProxyGet("review-calendar", ct);

    [HttpGet("templates")]
    public Task<IActionResult> Templates(CancellationToken ct) => ProxyGet("templates", ct);

    [HttpGet("analytics")]
    public Task<IActionResult> Analytics(CancellationToken ct) => ProxyGet("analytics", ct);

    [HttpPost("authoring")]
    public Task<IActionResult> CreateAuthoring([FromBody] object body, CancellationToken ct) =>
        ProxyPost("authoring", body, ct);

    [HttpGet("authoring/{id:guid}")]
    public Task<IActionResult> GetAuthoring(Guid id, CancellationToken ct) => ProxyGet($"authoring/{id}", ct);

    [HttpPatch("authoring/{id:guid}")]
    public Task<IActionResult> PatchAuthoring(Guid id, [FromBody] object body, CancellationToken ct) =>
        ProxyPatch($"authoring/{id}", body, ct);

    [HttpPost("compare")]
    public Task<IActionResult> CompareStructured([FromBody] object body, CancellationToken ct) =>
        ProxyPost("compare", body, ct);

    [HttpPost("draft-ai")]
    public Task<IActionResult> DraftAi([FromBody] object body, CancellationToken ct) =>
        ProxyPost("draft-ai", body, ct);

    [HttpPost("authoring/comments")]
    public Task<IActionResult> AddComment([FromBody] object body, CancellationToken ct) =>
        ProxyPost("authoring/comments", body, ct);

    [HttpPost("authoring/cross-references")]
    public Task<IActionResult> AddCrossReference([FromBody] object body, CancellationToken ct) =>
        ProxyPost("authoring/cross-references", body, ct);

    [HttpPost("export")]
    public Task<IActionResult> Export([FromBody] object body, CancellationToken ct) =>
        ProxyPost("export", body, ct);

    [HttpGet("graph")]
    public Task<IActionResult> Graph(CancellationToken ct) => ProxyGet("graph", ct);

    [HttpGet("health-dashboard")]
    public Task<IActionResult> HealthDashboard(CancellationToken ct) => ProxyGet("health-dashboard", ct);

    [HttpGet("intelligence/{id:guid}")]
    public Task<IActionResult> Intelligence(Guid id, CancellationToken ct) => ProxyGet($"intelligence/{id}", ct);

    [HttpGet("impact/{id:guid}")]
    public Task<IActionResult> Impact(Guid id, CancellationToken ct) => ProxyGet($"impact/{id}", ct);

    [HttpGet("dependencies/{id:guid}")]
    public Task<IActionResult> Dependencies(Guid id, CancellationToken ct) => ProxyGet($"dependencies/{id}", ct);

    [HttpGet("relationships/{id:guid}")]
    public Task<IActionResult> Relationships(Guid id, CancellationToken ct) => ProxyGet($"relationships/{id}", ct);

    [HttpPost("review-ai")]
    public Task<IActionResult> ReviewAi([FromBody] object body, CancellationToken ct) =>
        ProxyPost("review-ai", body, ct);

    [HttpGet("explorer")]
    public Task<IActionResult> Explorer(CancellationToken ct) => ProxyGet("explorer", ct);

    [HttpGet("search")]
    public Task<IActionResult> Search(CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/documentation/search{qs}", HttpMethod.Get, null, null, ct);
    }

    [HttpGet("cross-reference-suggestions")]
    public Task<IActionResult> CrossReferenceSuggestions(CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/documentation/cross-reference-suggestions{qs}", HttpMethod.Get, null, null, ct);
    }

    private Task<IActionResult> ProxyGet(string suffix, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/documentation/{suffix}", HttpMethod.Get, null, null, ct);
    }

    private Task<IActionResult> ProxyPost(string suffix, object body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var path = string.IsNullOrEmpty(suffix) ? $"/api/v{v}/documentation" : $"/api/v{v}/documentation/{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(path, HttpMethod.Post, body, null, ct);
    }

    private Task<IActionResult> ProxyPatch(string suffix, object body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/documentation/{suffix}", HttpMethod.Patch, body, null, ct);
    }
}
