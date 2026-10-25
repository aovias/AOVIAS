namespace Shared.Infrastructure.Auth;

/// <summary>
/// Canonical role names (UPPER_SNAKE_CASE). Identity <see cref="Microsoft.AspNetCore.Identity.IdentityRole"/> stores these as <c>Name</c>.
/// </summary>
public static class ApplicationRoles
{
    public const string USER = "USER";
    public const string ORG_ADMIN = "ORG_ADMIN";
    public const string GROUP_ADMIN = "GROUP_ADMIN";
    public const string SUPER_ADMIN = "SUPER_ADMIN";
    public const string REGULATORY_ADMIN = "REGULATORY_ADMIN";
    public const string COMPLIANCE_VIEWER = "COMPLIANCE_VIEWER";
    public const string AUTHORITY = "AUTHORITY";
    public const string REGULATOR = "REGULATOR";
    public const string MANAGER = "MANAGER";
    /// <summary>Department Head — distinct from <see cref="MANAGER"/> (Quality Manager).</summary>
    public const string DEPARTMENT_HEAD = "DEPARTMENT_HEAD";
    public const string SYSTEM = "SYSTEM";
    public const string EXTERNAL_AUDITOR = "EXTERNAL_AUDITOR";

    // CRP Phase 4 — Commercial Administration (AOVIAS internal staff; not customer signup roles)
    public const string COMMERCIAL_ADMIN = "COMMERCIAL_ADMIN";
    public const string CSM = "CSM";
    public const string SUPPORT_ENGINEER = "SUPPORT_ENGINEER";
    public const string LICENSE_ADMIN = "LICENSE_ADMIN";
    public const string SALES_OPS = "SALES_OPS";
    public const string COMPLIANCE_OPS = "COMPLIANCE_OPS";
    public const string COMMERCIAL_AUDITOR = "COMMERCIAL_AUDITOR";

    /// <summary>
    /// Roles that may be assigned through internal registration / onboarding APIs (subset of all product roles).
    /// </summary>
    public static readonly IReadOnlySet<string> AssignableRoles = new HashSet<string>(StringComparer.Ordinal)
    {
        USER,
        ORG_ADMIN,
        GROUP_ADMIN,
        SUPER_ADMIN,
        REGULATORY_ADMIN,
        COMPLIANCE_VIEWER,
        AUTHORITY,
        REGULATOR,
        MANAGER,
        DEPARTMENT_HEAD,
        EXTERNAL_AUDITOR,
    };

    /// <summary>Roles permitted to access the Commercial Administration Console (cross-tenant).</summary>
    public static readonly IReadOnlySet<string> CommercialAdminRoles = new HashSet<string>(StringComparer.Ordinal)
    {
        SUPER_ADMIN,
        SYSTEM,
        COMMERCIAL_ADMIN,
        CSM,
        SUPPORT_ENGINEER,
        LICENSE_ADMIN,
        SALES_OPS,
        COMPLIANCE_OPS,
        COMMERCIAL_AUDITOR,
    };

    /// <summary>
    /// All roles that must exist in Identity <c>AspNetRoles</c> (seeded at startup).
    /// </summary>
    public static readonly IReadOnlyList<string> AllSeededRoles = new[]
    {
        USER,
        ORG_ADMIN,
        GROUP_ADMIN,
        SUPER_ADMIN,
        REGULATORY_ADMIN,
        COMPLIANCE_VIEWER,
        AUTHORITY,
        REGULATOR,
        MANAGER,
        DEPARTMENT_HEAD,
        SYSTEM,
        EXTERNAL_AUDITOR,
        COMMERCIAL_ADMIN,
        CSM,
        SUPPORT_ENGINEER,
        LICENSE_ADMIN,
        SALES_OPS,
        COMPLIANCE_OPS,
        COMMERCIAL_AUDITOR,
    };

    /// <summary>
    /// Returns true if <paramref name="role"/> is a known assignable canonical role.
    /// </summary>
    public static bool IsAssignableRole(string? role)
    {
        return !string.IsNullOrWhiteSpace(role) && AssignableRoles.Contains(role.Trim());
    }

    /// <summary>
    /// Comma-separated lists for <c>[Authorize(Roles = ...)]</c> (requires compile-time constant expression).
    /// </summary>
    public static class Authorization
    {
        public const string RegulatoryOverviewReaders =
            SUPER_ADMIN + "," + REGULATORY_ADMIN + "," + COMPLIANCE_VIEWER + "," + ORG_ADMIN;

        public const string SuperAdminOrOrgAdmin =
            SUPER_ADMIN + "," + ORG_ADMIN;

        public const string SuperAdminRegulatoryOrgAdmin =
            SUPER_ADMIN + "," + REGULATORY_ADMIN + "," + ORG_ADMIN;

        public const string SuperAdminRegulatoryAdmin =
            SUPER_ADMIN + "," + REGULATORY_ADMIN;

        public const string ExternalAuditorOrAdmin =
            EXTERNAL_AUDITOR + "," + SUPER_ADMIN + "," + ORG_ADMIN;
    }
}
