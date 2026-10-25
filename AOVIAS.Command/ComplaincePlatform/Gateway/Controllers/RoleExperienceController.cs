using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Proxies Role Experience / Persona Intelligence endpoints to ComplianceService (Phase 3 Sprint 1).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/role-experience")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class RoleExperienceController : ControllerBase
{
    private readonly IProxyService _proxy;

    public RoleExperienceController(IProxyService proxy) => _proxy = proxy;

    [HttpGet("home")]
    public Task<IActionResult> GetHome(CancellationToken cancellationToken)
    {
        var v = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return _proxy.ProxyToComplianceServiceAsync(
            $"/api/v{v}/role-experience/home{qs}",
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
    }
}
