namespace Shared.Infrastructure.Auth;

/// <summary>
/// Maps legacy / historical role strings to <see cref="ApplicationRoles"/> canonical values.
/// Used during token issuance and one-time DB migration.
/// </summary>
public static class RoleLegacyMap
{
    private static readonly Dictionary<string, string> LegacyToCanonical =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Identity / onboarding
            ["User"] = ApplicationRoles.USER,
            ["USER"] = ApplicationRoles.USER,
            ["GroupAdmin"] = ApplicationRoles.ORG_ADMIN,
            ["GROUP_ADMIN"] = ApplicationRoles.GROUP_ADMIN,
            // Compliance (pre-canonical)
            ["OrgAdmin"] = ApplicationRoles.ORG_ADMIN,
            // Gateway / Identity already uppercase
            [ApplicationRoles.SUPER_ADMIN] = ApplicationRoles.SUPER_ADMIN,
            [ApplicationRoles.REGULATORY_ADMIN] = ApplicationRoles.REGULATORY_ADMIN,
            [ApplicationRoles.COMPLIANCE_VIEWER] = ApplicationRoles.COMPLIANCE_VIEWER,
            [ApplicationRoles.ORG_ADMIN] = ApplicationRoles.ORG_ADMIN,
            [ApplicationRoles.AUTHORITY] = ApplicationRoles.AUTHORITY,
            [ApplicationRoles.REGULATOR] = ApplicationRoles.REGULATOR,
            [ApplicationRoles.MANAGER] = ApplicationRoles.MANAGER,
            [ApplicationRoles.DEPARTMENT_HEAD] = ApplicationRoles.DEPARTMENT_HEAD,
            ["Department Head"] = ApplicationRoles.DEPARTMENT_HEAD,
            ["DepartmentHead"] = ApplicationRoles.DEPARTMENT_HEAD,
            ["DeptHead"] = ApplicationRoles.DEPARTMENT_HEAD,
            [ApplicationRoles.SYSTEM] = ApplicationRoles.SYSTEM,
        };

    /// <summary>
    /// Returns the canonical role name for a role string from Identity or legacy payloads.
    /// Unknown values are returned trimmed as-is (caller may reject).
    /// </summary>
    public static string ToCanonical(string? roleFromStore)
    {
        if (string.IsNullOrWhiteSpace(roleFromStore))
            return ApplicationRoles.USER;

        var trimmed = roleFromStore.Trim();
        return LegacyToCanonical.TryGetValue(trimmed, out var canonical)
            ? canonical
            : trimmed;
    }

    /// <summary>
    /// Normalizes every role from Identity for JWT claims.
    /// </summary>
    public static IReadOnlyList<string> NormalizeRoleNames(IEnumerable<string>? roles)
    {
        if (roles == null)
            return Array.Empty<string>();

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in roles)
        {
            if (string.IsNullOrWhiteSpace(r))
                continue;
            var c = ToCanonical(r);
            if (!string.IsNullOrEmpty(c))
                set.Add(c);
        }

        return set.Count == 0
            ? new[] { ApplicationRoles.USER }
            : set.ToList();
    }
}
