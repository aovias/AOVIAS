using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>Proxies Sprint 17A Enterprise Compliance Graph APIs to ComplianceService.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/graph")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class ComplianceGraphV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;

    public ComplianceGraphV1ProxyController(IProxyService proxy) => _proxy = proxy;

    [HttpGet("entity/{id:guid}")]
    public Task<IActionResult> Entity(Guid id, CancellationToken ct) => ProxyGet($"entity/{id}", ct);

    [HttpGet("relationships/{id:guid}")]
    public Task<IActionResult> Relationships(Guid id, CancellationToken ct) => ProxyGet($"relationships/{id}", ct);

    [HttpGet("timeline/{id:guid}")]
    public Task<IActionResult> Timeline(Guid id, CancellationToken ct) => ProxyGet($"timeline/{id}", ct);

    [HttpGet("impact/{id:guid}")]
    public Task<IActionResult> Impact(Guid id, CancellationToken ct) => ProxyGet($"impact/{id}", ct);

    [HttpGet("dependencies/{id:guid}")]
    public Task<IActionResult> Dependencies(Guid id, CancellationToken ct) => ProxyGet($"dependencies/{id}", ct);

    [HttpGet("search")]
    public Task<IActionResult> Search(CancellationToken ct) => ProxyGet("search", ct);

    [HttpGet("navigation/{id:guid}")]
    public Task<IActionResult> Navigation(Guid id, CancellationToken ct) => ProxyGet($"navigation/{id}", ct);

    [HttpGet("traceability/{id:guid}")]
    public Task<IActionResult> Traceability(Guid id, CancellationToken ct) => ProxyGet($"traceability/{id}", ct);

    [HttpGet("executive")]
    public Task<IActionResult> Executive(CancellationToken ct) => ProxyGet("executive", ct);

    [HttpGet("analytics")]
    public Task<IActionResult> Analytics(CancellationToken ct) => ProxyGet("analytics", ct);

    [HttpGet("visual")]
    public Task<IActionResult> Visual(CancellationToken ct) => ProxyGet("visual", ct);

    private Task<IActionResult> ProxyGet(string path, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/graph/{path}{suffix}", HttpMethod.Get, null, null, ct);
    }

    private string QuerySuffix()
    {
        if (!Request.QueryString.HasValue || string.IsNullOrEmpty(Request.QueryString.Value)) return string.Empty;
        return Request.QueryString.Value.StartsWith('?') ? Request.QueryString.Value : "?" + Request.QueryString.Value;
    }
}
