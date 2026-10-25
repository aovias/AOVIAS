using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Proxies Phase 4 Sprint 4 Enterprise Risk Register APIs to ComplianceService.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/risks")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class RisksV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;

    public RisksV1ProxyController(IProxyService proxy) => _proxy = proxy;

    [HttpGet("matrix")]
    public Task<IActionResult> Matrix(CancellationToken ct) => ProxyGet("matrix", ct);

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct) => ProxyGet("", ct);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/risks/{id}", HttpMethod.Get, null, null, ct);
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct) => ProxyPost("", body, ct);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] object body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/risks/{id}", HttpMethod.Put, body, null, ct);
    }

    [HttpPatch("{id:guid}")]
    public Task<IActionResult> Patch(Guid id, [FromBody] object body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/risks/{id}", HttpMethod.Patch, body, null, ct);
    }

    private Task<IActionResult> ProxyGet(string path, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = string.IsNullOrEmpty(path)
            ? $"/api/v{v}/risks{suffix}"
            : $"/api/v{v}/risks/{path}{suffix}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, ct);
    }

    private Task<IActionResult> ProxyPost(string path, object? body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var endpoint = string.IsNullOrEmpty(path)
            ? $"/api/v{v}/risks"
            : $"/api/v{v}/risks/{path}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, body, null, ct);
    }

    private string QuerySuffix()
    {
        if (!Request.QueryString.HasValue || string.IsNullOrEmpty(Request.QueryString.Value)) return string.Empty;
        return Request.QueryString.Value.StartsWith('?') ? Request.QueryString.Value : "?" + Request.QueryString.Value;
    }
}
