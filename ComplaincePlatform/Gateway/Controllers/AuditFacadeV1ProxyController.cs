using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Thin proxy for Compliance audit generate/get facade (replaces legacy AuditService for AI/clients).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class AuditFacadeV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;

    public AuditFacadeV1ProxyController(IProxyService proxy) => _proxy = proxy;

    [HttpPost("generate")]
    public Task<IActionResult> Generate([FromBody] object body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/audit/generate", HttpMethod.Post, body, null, ct);
    }

    [HttpGet("{sessionId:guid}")]
    public Task<IActionResult> Get(Guid sessionId, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/audit/{sessionId}", HttpMethod.Get, null, null, ct);
    }
}
