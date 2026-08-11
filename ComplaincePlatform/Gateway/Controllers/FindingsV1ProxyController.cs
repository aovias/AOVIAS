using Gateway.DTOs;
using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Proxies organization-scoped compliance findings (managed registry) to ComplianceService.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/findings")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class FindingsV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;
    private readonly ILogger<FindingsV1ProxyController> _logger;

    public FindingsV1ProxyController(IProxyService proxy, ILogger<FindingsV1ProxyController> logger)
    {
        _proxy = proxy;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/findings{suffix}";
        _logger.LogDebug("Proxy GET {Endpoint}", endpoint);
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var endpoint = $"/api/v{v}/findings/{id}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var endpoint = $"/api/v{v}/findings";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, body, null, cancellationToken);
    }

    [HttpGet("workspace/dashboard")]
    public Task<IActionResult> WorkspaceDashboard(CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/workspace/dashboard", HttpMethod.Get, null, null, cancellationToken);
    }

    [HttpGet("workspace")]
    public Task<IActionResult> Workspace(CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/workspace{suffix}", HttpMethod.Get, null, null, cancellationToken);
    }

    [HttpGet("workspace/{id:guid}")]
    public Task<IActionResult> WorkspaceDetail(Guid id, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/workspace/{id}", HttpMethod.Get, null, null, cancellationToken);
    }

    [HttpGet("{id:guid}/eligibility")]
    public Task<IActionResult> GetEligibility(Guid id, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/{id}/eligibility", HttpMethod.Get, null, null, cancellationToken);
    }

    [HttpGet("{id:guid}/promotion-status")]
    public Task<IActionResult> GetPromotionStatus(Guid id, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/{id}/promotion-status", HttpMethod.Get, null, null, cancellationToken);
    }

    [HttpPost("{id:guid}/promote")]
    public Task<IActionResult> PromoteToDeficiency(Guid id, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/{id}/promote", HttpMethod.Post, null, null, cancellationToken);
    }

    [HttpGet("workspace/compliance-context")]
    public Task<IActionResult> ComplianceContext(CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/workspace/compliance-context{suffix}", HttpMethod.Get, null, null, cancellationToken);
    }

    [HttpPatch("{id:guid}/status")]
    public Task<IActionResult> PatchStatus(Guid id, [FromBody] object body, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/{id}/status", HttpMethod.Patch, body, null, cancellationToken);
    }

    /// <summary>DEF-M3-001 — classifies an existing finding against an objective element.</summary>
    [HttpPatch("{id:guid}/compliance-context")]
    public Task<IActionResult> PatchComplianceContext(Guid id, [FromBody] object body, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/{id}/compliance-context", HttpMethod.Patch, body, null, cancellationToken);
    }

    [HttpPost("{id:guid}/comments")]
    public Task<IActionResult> AddComment(Guid id, [FromBody] object body, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/{id}/comments", HttpMethod.Post, body, null, cancellationToken);
    }

    [HttpPost("{id:guid}/attachments")]
    public Task<IActionResult> AddAttachment(Guid id, [FromBody] object body, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/findings/{id}/attachments", HttpMethod.Post, body, null, cancellationToken);
    }

    private string QuerySuffix()
    {
        if (!Request.QueryString.HasValue || string.IsNullOrEmpty(Request.QueryString.Value))
        {
            return string.Empty;
        }

        return Request.QueryString.Value.StartsWith('?')
            ? Request.QueryString.Value
            : "?" + Request.QueryString.Value;
    }
}
