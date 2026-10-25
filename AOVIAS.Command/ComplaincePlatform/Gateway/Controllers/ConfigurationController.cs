using Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Execution;

namespace Gateway.Controllers;

/// <summary>
/// Enterprise Configuration API — client-facing surface (Stage 5B.2).
///
/// <para>
/// The organization is taken from the caller's validated token and appended here; it is never
/// accepted from the client. Configuration determines what a tenant sees and what values it may
/// submit, so letting a caller name the organization would let it read another tenant's
/// configuration (Wave 2.2B, risk R-02).
/// </para>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/configuration")]
[Authorize]
public sealed class ConfigurationController : ControllerBase
{
    private readonly IProxyService _proxy;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(IProxyService proxy, ILogger<ConfigurationController> logger)
    {
        _proxy = proxy;
        _logger = logger;
    }

    /// <summary>GET /api/v1/configuration/categories</summary>
    [HttpGet("categories")]
    public Task<IActionResult> GetCategories(CancellationToken ct)
        => _proxy.ProxyToOrganizationServiceAsync(
            "/internal/v1/configuration/categories", HttpMethod.Get, null, ct);

    /// <summary>
    /// GET /api/v1/configuration/categories/{code}
    ///
    /// Resolved configuration for the caller's organization: platform defaults overlaid with that
    /// tenant's overrides.
    /// </summary>
    [HttpGet("categories/{code}")]
    public Task<IActionResult> GetCategory(string code, CancellationToken ct)
    {
        var organizationId = TenantAuthority.OrganizationFromClaims(HttpContext);
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("Configuration request for '{Code}' denied: no organization claim.", code);
            return Task.FromResult<IActionResult>(Unauthorized());
        }

        return _proxy.ProxyToOrganizationServiceAsync(
            $"/internal/v1/configuration/categories/{Uri.EscapeDataString(code)}?organizationId={organizationId}",
            HttpMethod.Get, null, ct);
    }

    /// <summary>
    /// PUT /api/v1/configuration/categories/{code}/values
    ///
    /// Creates or updates the caller organization's override. Requires an administrator role: a
    /// configuration change alters behaviour for every user in the tenant.
    /// </summary>
    [HttpPut("categories/{code}/values")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
    public Task<IActionResult> UpsertOverride(
        string code,
        [FromBody] object body,
        CancellationToken ct)
    {
        var organizationId = TenantAuthority.OrganizationFromClaims(HttpContext);
        if (organizationId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(Unauthorized());
        }

        _logger.LogInformation(
            "Configuration override applied to '{Code}' for organization {OrganizationId} by {Caller}.",
            code, organizationId, User.Identity?.Name ?? "(unknown)");

        return _proxy.ProxyToOrganizationServiceAsync(
            $"/internal/v1/configuration/categories/{Uri.EscapeDataString(code)}/values?organizationId={organizationId}",
            HttpMethod.Put, body, ct);
    }

    /// <summary>
    /// DELETE /api/v1/configuration/categories/{code}/values/{valueCode}
    ///
    /// Restores the platform default for one value.
    /// </summary>
    [HttpDelete("categories/{code}/values/{valueCode}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN")]
    public Task<IActionResult> RemoveOverride(string code, string valueCode, CancellationToken ct)
    {
        var organizationId = TenantAuthority.OrganizationFromClaims(HttpContext);
        if (organizationId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(Unauthorized());
        }

        _logger.LogInformation(
            "Configuration override removed from '{Code}/{ValueCode}' for organization {OrganizationId} by {Caller}.",
            code, valueCode, organizationId, User.Identity?.Name ?? "(unknown)");

        return _proxy.ProxyToOrganizationServiceAsync(
            $"/internal/v1/configuration/categories/{Uri.EscapeDataString(code)}/values/{Uri.EscapeDataString(valueCode)}?organizationId={organizationId}",
            HttpMethod.Delete, null, ct);
    }
}
