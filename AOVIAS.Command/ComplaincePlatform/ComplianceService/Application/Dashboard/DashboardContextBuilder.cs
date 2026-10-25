using ComplianceService.Application.Contracts;
using ComplianceService.Application.RoleExperience;
using Platform.Dashboard.Models;
using Shared.Infrastructure.Auth;

namespace ComplianceService.Application.Dashboard;

/// <summary>
/// Builds <see cref="DashboardContext"/> from JWT claims and user context.
/// </summary>
public sealed class DashboardContextBuilder
{
    private readonly IUserContextService _userContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DashboardContextBuilder(
        IUserContextService userContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _userContext = userContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public DashboardContext Build(Guid? hospitalId, Guid? frameworkId, bool forceRefresh = false)
    {
        var resolvedHospitalId = hospitalId is { } hid && hid != Guid.Empty
            ? hid
            : _userContext.HospitalId is var ctxHid && ctxHid != Guid.Empty
                ? ctxHid
                : (Guid?)null;

        return new DashboardContext
        {
            OrganizationId = _userContext.OrganizationId,
            HospitalId = resolvedHospitalId,
            FrameworkId = frameworkId is { } fid && fid != Guid.Empty ? fid : null,
            UserId = _userContext.UserId,
            Permissions = ResolvePermissions(),
            ForceRefresh = forceRefresh,
        };
    }

    private HashSet<string> ResolvePermissions()
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return permissions;
        }

        foreach (var claim in user.Claims)
        {
            if (claim.Type is "scope" or "scp" or "permission")
            {
                foreach (var part in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    permissions.Add(part);
                }
            }
        }

        if (user.HasClaim(c => c.Type == "scope" && c.Value.Contains(AuthConstants.Scopes.ComplianceApi, StringComparison.OrdinalIgnoreCase))
            || user.HasClaim(c => c.Type == "scp" && c.Value.Contains(AuthConstants.Scopes.ComplianceApi, StringComparison.OrdinalIgnoreCase)))
        {
            permissions.Add(AuthConstants.Scopes.ComplianceApi);
        }

        // Users authenticated under ComplianceScope policy implicitly have compliance.api for provider filtering.
        permissions.Add(AuthConstants.Scopes.ComplianceApi);

        // Phase 3 Sprint 1 — inject resolved role-pack tag for widget/section filtering.
        var pack = RoleExperiencePackRegistry.Resolve(_userContext.Roles);
        permissions.Add(RoleExperiencePackRegistry.ToPermissionTag(pack.PackId));

        return permissions;
    }
}
