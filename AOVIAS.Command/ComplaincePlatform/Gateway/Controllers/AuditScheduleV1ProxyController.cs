using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-schedule")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class AuditScheduleV1ProxyController : ControllerBase
{
    private readonly IProxyService _proxy;

    public AuditScheduleV1ProxyController(IProxyService proxy) => _proxy = proxy;

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/audit-schedule{qs}", HttpMethod.Get, null, null, ct);
    }

    [HttpGet("calendar")]
    public Task<IActionResult> Calendar(CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/audit-schedule/calendar{qs}", HttpMethod.Get, null, null, ct);
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] object body, CancellationToken ct)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _proxy.ProxyToComplianceServiceAsync($"/api/v{v}/audit-schedule", HttpMethod.Post, body, null, ct);
    }
}
