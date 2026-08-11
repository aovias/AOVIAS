using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>Proxies Sprint 17 Quality Intelligence APIs to ComplianceService.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/quality")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class QualityV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;

    public QualityV1ProxyController(IProxyService proxy) => _proxy = proxy;

    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard(CancellationToken ct) => ProxyGet("dashboard", ct);

    [HttpGet("indicators")]
    public Task<IActionResult> Indicators(CancellationToken ct) => ProxyGet("indicators", ct);

    [HttpGet("indicators/{id:guid}")]
    public Task<IActionResult> Indicator(Guid id, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/quality/indicators/{id}", HttpMethod.Get, null, null, ct);
    }

    [HttpPatch("thresholds")]
    public Task<IActionResult> Thresholds([FromBody] object body, CancellationToken ct) => ProxyPatch("thresholds", body, ct);

    [HttpGet("scorecards")]
    public Task<IActionResult> Scorecards(CancellationToken ct) => ProxyGet("scorecards", ct);

    [HttpGet("trends")]
    public Task<IActionResult> Trends(CancellationToken ct) => ProxyGet("trends", ct);

    [HttpGet("benchmarking")]
    public Task<IActionResult> Benchmarking(CancellationToken ct) => ProxyGet("benchmarking", ct);

    [HttpGet("executive")]
    public Task<IActionResult> Executive(CancellationToken ct) => ProxyGet("executive", ct);

    [HttpGet("analytics")]
    public Task<IActionResult> Analytics(CancellationToken ct) => ProxyGet("analytics", ct);

    private Task<IActionResult> ProxyGet(string path, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/quality/{path}{suffix}", HttpMethod.Get, null, null, ct);
    }

    private Task<IActionResult> ProxyPatch(string path, object? body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/quality/{path}", HttpMethod.Patch, body, null, ct);
    }

    private string QuerySuffix()
    {
        if (!Request.QueryString.HasValue || string.IsNullOrEmpty(Request.QueryString.Value)) return string.Empty;
        return Request.QueryString.Value.StartsWith('?') ? Request.QueryString.Value : "?" + Request.QueryString.Value;
    }
}
