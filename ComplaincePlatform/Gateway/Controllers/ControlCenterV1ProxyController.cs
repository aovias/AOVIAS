using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Proxies the Compliance Command Center (Control Center) read endpoints to ComplianceService.
/// Mirrors the dashboard path convention: client calls
/// <c>/api/v1/control-center/compliance/{section}</c> and the Gateway forwards 1:1.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-center/compliance")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class ControlCenterV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;
    private readonly ILogger<ControlCenterV1ProxyController> _logger;

    public ControlCenterV1ProxyController(IProxyService proxy, ILogger<ControlCenterV1ProxyController> logger)
    {
        _proxy = proxy;
        _logger = logger;
    }

    [HttpGet("{**section}")]
    public Task<IActionResult> Get(string section, CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var suffix = QuerySuffix();
        var endpoint = $"/api/v{v}/control-center/compliance/{section}{suffix}";
        _logger.LogDebug("Proxy GET {Endpoint}", endpoint);
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
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
