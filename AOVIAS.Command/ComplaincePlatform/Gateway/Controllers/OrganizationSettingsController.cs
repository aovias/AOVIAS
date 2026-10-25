using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Gateway endpoints for organization-wide platform settings (M7.1).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class OrganizationSettingsController(IMediator mediator) : ControllerBase
{
    private string ApiVersionValue() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    private Guid? ResolveOrganizationId()
    {
        if (User.IsInRole(ApplicationRoles.SUPER_ADMIN))
        {
            var routeOrg = HttpContext.GetRouteData()?.Values["orgId"]?.ToString();
            if (Guid.TryParse(routeOrg, out var routeId)) return routeId;
        }

        var claim = User.FindFirst("organization_id")?.Value;
        return Guid.TryParse(claim, out var orgId) ? orgId : null;
    }

    [HttpGet("organization")]
    public async Task<IActionResult> GetOrganizationSettings(CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Forbid();
        return await mediator.Send(new GetOrganizationSettingsProxyQuery(ApiVersionValue(), orgId.Value), ct);
    }

    [HttpPut("organization")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
    public async Task<IActionResult> UpdateOrganizationSettings([FromBody] object body, CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Forbid();
        return await mediator.Send(new UpdateOrganizationSettingsProxyCommand(ApiVersionValue(), orgId.Value, body), ct);
    }

    [HttpPost("organization/logo")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Forbid();
        if (file == null || file.Length == 0) return BadRequest();
        return await mediator.Send(new UploadOrganizationLogoProxyCommand(ApiVersionValue(), orgId.Value, file), ct);
    }

    [HttpDelete("organization/logo")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
    public async Task<IActionResult> RemoveLogo(CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Forbid();
        return await mediator.Send(new RemoveOrganizationLogoProxyCommand(ApiVersionValue(), orgId.Value), ct);
    }

    [HttpGet("organization/logo")]
    public async Task<IActionResult> GetLogo(CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Forbid();
        return await mediator.Send(new GetOrganizationLogoProxyQuery(ApiVersionValue(), orgId.Value), ct);
    }

    [HttpGet("organization/branding")]
    public async Task<IActionResult> GetBranding(CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Forbid();
        return await mediator.Send(new GetOrganizationBrandingProxyQuery(ApiVersionValue(), orgId.Value), ct);
    }

    [HttpPut("organization/branding")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
    public async Task<IActionResult> UpdateBranding([FromBody] object body, CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Forbid();
        return await mediator.Send(new UpdateOrganizationBrandingProxyCommand(ApiVersionValue(), orgId.Value, body), ct);
    }

    [HttpPost("organization/branding/assets/{assetKey}")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadBrandingAsset(string assetKey, IFormFile file, CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Forbid();
        if (file == null || file.Length == 0) return BadRequest();
        return await mediator.Send(new UploadBrandingAssetProxyCommand(ApiVersionValue(), orgId.Value, assetKey, file), ct);
    }

    [HttpDelete("organization/branding/assets/{assetKey}")]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
    public async Task<IActionResult> RemoveBrandingAsset(string assetKey, CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Forbid();
        return await mediator.Send(new RemoveBrandingAssetProxyCommand(ApiVersionValue(), orgId.Value, assetKey), ct);
    }

    [HttpGet("organization/branding/assets/{assetKey}")]
    public async Task<IActionResult> GetBrandingAsset(string assetKey, CancellationToken ct)
    {
        var orgId = ResolveOrganizationId();
        if (orgId == null) return Forbid();
        return await mediator.Send(new GetOrganizationBrandingAssetProxyQuery(ApiVersionValue(), orgId.Value, assetKey), ct);
    }
}
