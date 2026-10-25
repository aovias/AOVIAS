using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Execution;

/// <summary>Outcome of resolving which tenant a request may act on.</summary>
public readonly record struct TenantResolution(Guid OrganizationId, bool IsAuthorized, string? Reason)
{
    public static TenantResolution Allow(Guid organizationId) => new(organizationId, true, null);
    public static TenantResolution Deny(string reason) => new(Guid.Empty, false, reason);
}

/// <summary>
/// Decides which organization a request is permitted to act on.
///
/// <para>
/// **The problem this solves.** 63 Gateway endpoints accept <c>organizationId</c> as a query
/// parameter. For a dozen of them — the AOVIAS staff consoles — that is correct and intended. For
/// the rest it means a caller can name someone else's tenant, and 24 of those sit behind nothing
/// more than a bare <c>[Authorize]</c> plus a permission check. Permission was verified; tenant
/// was not. See the Wave 2.2A Tenant Isolation Assessment, risk R-02.
/// </para>
///
/// <para>
/// **The rule.** The token decides the tenant. A client-supplied value is accepted only when the
/// endpoint is explicitly cross-tenant *and* the caller holds a cross-tenant role. Anything else
/// is a spoofing attempt and is logged as one.
/// </para>
/// </summary>
public static class TenantAuthority
{
    /// <summary>Roles permitted to act across tenants (AOVIAS staff, not customer roles).</summary>
    private static readonly string[] CrossTenantRoles =
    {
        "SUPER_ADMIN", "SYSTEM", "COMMERCIAL_ADMIN", "CSM",
        "SUPPORT_ENGINEER", "LICENSE_ADMIN", "SALES_OPS",
        "COMPLIANCE_OPS", "COMMERCIAL_AUDITOR",
    };

    /// <summary>
    /// Resolve the organization a tenant-scoped endpoint may act on.
    ///
    /// <paramref name="requestedOrganizationId"/> is treated as a *claim by the caller*, not an
    /// instruction: when it disagrees with the token it is rejected, and when it is absent the
    /// token's value is used. Callers must return the denial rather than proceeding.
    /// </summary>
    public static TenantResolution ResolveForTenantScopedRequest(
        HttpContext http,
        Guid requestedOrganizationId,
        ILogger? logger = null)
    {
        var authenticated = OrganizationFromClaims(http);

        if (authenticated == Guid.Empty)
        {
            logger?.LogWarning(
                "Tenant resolution denied on {Path}: no organization claim on the token.",
                http.Request.Path);
            return TenantResolution.Deny("No organization context on the presented token.");
        }

        if (requestedOrganizationId == Guid.Empty || requestedOrganizationId == authenticated)
        {
            return TenantResolution.Allow(authenticated);
        }

        // A caller naming a different tenant is a security event, not a validation error — it is
        // logged with both ids so the attempt is attributable.
        if (HasCrossTenantRole(http))
        {
            logger?.LogInformation(
                "Cross-tenant access on {Path} by privileged caller: requested {Requested}, token {Authenticated}.",
                http.Request.Path, requestedOrganizationId, authenticated);
            return TenantResolution.Allow(requestedOrganizationId);
        }

        logger?.LogWarning(
            "TENANT SPOOFING REJECTED on {Path}: caller in organization {Authenticated} requested {Requested}.",
            http.Request.Path, authenticated, requestedOrganizationId);
        return TenantResolution.Deny("The requested organization does not match the authenticated tenant.");
    }

    /// <summary>
    /// Resolve for an endpoint that is cross-tenant *by design* (staff consoles). Still requires a
    /// cross-tenant role — the endpoint being privileged does not make the caller privileged.
    /// </summary>
    public static TenantResolution ResolveForCrossTenantRequest(
        HttpContext http,
        Guid requestedOrganizationId,
        ILogger? logger = null)
    {
        if (!HasCrossTenantRole(http))
        {
            logger?.LogWarning(
                "Cross-tenant endpoint {Path} denied: caller holds no cross-tenant role.",
                http.Request.Path);
            return TenantResolution.Deny("Cross-tenant access requires a platform administration role.");
        }

        return requestedOrganizationId != Guid.Empty
            ? TenantResolution.Allow(requestedOrganizationId)
            : TenantResolution.Allow(OrganizationFromClaims(http));
    }

    public static Guid OrganizationFromClaims(HttpContext http)
    {
        var raw = http.User?.FindFirst("organization_id")?.Value
                  ?? http.User?.FindFirst("organizationId")?.Value
                  ?? http.User?.FindFirst("org_id")?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private static bool HasCrossTenantRole(HttpContext http) =>
        http.User is not null && CrossTenantRoles.Any(http.User.IsInRole);
}
