using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Proxies Sprint 16 Incident Management &amp; Patient Safety APIs to ComplianceService.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/incidents")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class IncidentsV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;

    public IncidentsV1ProxyController(IProxyService proxy) => _proxy = proxy;

    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard(CancellationToken ct) => ProxyGet("dashboard", ct);

    [HttpGet("analytics")]
    public Task<IActionResult> Analytics(CancellationToken ct) => ProxyGet("analytics", ct);

    [HttpGet("feedback")]
    public Task<IActionResult> Feedback(CancellationToken ct) => ProxyGet("feedback", ct);

    [HttpPost("feedback")]
    public Task<IActionResult> CreateFeedback([FromBody] object body, CancellationToken ct) => ProxyPost("feedback", body, ct);

    [HttpGet("lessons")]
    public Task<IActionResult> Lessons(CancellationToken ct) => ProxyGet("lessons", ct);

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => ProxyGet("", ct);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/incidents/{id}", HttpMethod.Get, null, null, ct);
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct) => ProxyPost("", body, ct);

    [HttpPatch("{id:guid}")]
    public Task<IActionResult> Patch(Guid id, [FromBody] object body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/incidents/{id}", HttpMethod.Patch, body, null, ct);
    }

    [HttpPost("investigation")]
    public Task<IActionResult> Investigation([FromBody] object body, CancellationToken ct) => ProxyPost("investigation", body, ct);

    [HttpPost("rca")]
    public Task<IActionResult> Rca([FromBody] object body, CancellationToken ct) => ProxyPost("rca", body, ct);

    [HttpPost("promote-finding")]
    public Task<IActionResult> PromoteFinding([FromBody] object body, CancellationToken ct) => ProxyPost("promote-finding", body, ct);

    [HttpPost("promote-deficiency")]
    public Task<IActionResult> PromoteDeficiency([FromBody] object body, CancellationToken ct) => ProxyPost("promote-deficiency", body, ct);

    [HttpPost("lesson")]
    public Task<IActionResult> Lesson([FromBody] object body, CancellationToken ct) => ProxyPost("lesson", body, ct);

    private Task<IActionResult> ProxyGet(string path, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = string.IsNullOrEmpty(path)
            ? $"/api/v{v}/incidents{suffix}"
            : $"/api/v{v}/incidents/{path}{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, ct);
    }

    private Task<IActionResult> ProxyPost(string path, object? body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var endpoint = string.IsNullOrEmpty(path)
            ? $"/api/v{v}/incidents"
            : $"/api/v{v}/incidents/{path}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, body, null, ct);
    }

    private string QuerySuffix()
    {
        if (!Request.QueryString.HasValue || string.IsNullOrEmpty(Request.QueryString.Value)) return string.Empty;
        return Request.QueryString.Value.StartsWith('?') ? Request.QueryString.Value : "?" + Request.QueryString.Value;
    }
}
