namespace Shared.Infrastructure.Auth;

/// <summary>CRP Phase 4 — fine-grained commercial administration permissions.</summary>
public static class CommercialPermissions
{
    public const string OrgsRead = "Commercial.Orgs.Read";
    public const string OrgsWrite = "Commercial.Orgs.Write";
    public const string OrgsDelete = "Commercial.Orgs.Delete";
    public const string OrgsPermanentDelete = "Commercial.Orgs.PermanentDelete";

    public const string UsersRead = "Commercial.Users.Read";
    public const string UsersWrite = "Commercial.Users.Write";
    public const string UsersUnlock = "Commercial.Users.Unlock";
    public const string UsersPassword = "Commercial.Users.Password";

    public const string TrialsRead = "Commercial.Trials.Read";
    public const string TrialsWrite = "Commercial.Trials.Write";
    public const string TrialsConvert = "Commercial.Trials.Convert";

    public const string LicensesRead = "Commercial.Licenses.Read";
    public const string LicensesWrite = "Commercial.Licenses.Write";
    public const string LicensesRevoke = "Commercial.Licenses.Revoke";

    public const string EmailSend = "Commercial.Email.Send";
    public const string SupportWorkspace = "Commercial.Support.Use";
    public const string CommercialOps = "Commercial.Ops.Write";
    public const string AuditRead = "Commercial.Audit.Read";
    public const string AuditExport = "Commercial.Audit.Export";
    public const string HealthRead = "Commercial.Health.Read";
    public const string Search = "Commercial.Search";

    public static bool RoleHasPermission(string? role, string permission)
    {
        if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        var r = role.Trim();
        if (r is ApplicationRoles.SUPER_ADMIN or ApplicationRoles.SYSTEM)
        {
            return true;
        }

        return Map.TryGetValue(r, out var set) && set.Contains(permission);
    }

    public static bool AnyRoleHasPermission(IEnumerable<string> roles, string permission)
        => roles.Any(r => RoleHasPermission(r, permission));

    private static readonly Dictionary<string, HashSet<string>> Map = new(StringComparer.Ordinal)
    {
        [ApplicationRoles.COMMERCIAL_ADMIN] = AllExcept(OrgsPermanentDelete),
        [ApplicationRoles.CSM] =
        [
            OrgsRead, UsersRead, UsersUnlock, UsersPassword, TrialsRead, TrialsWrite,
            LicensesRead, EmailSend, SupportWorkspace, AuditRead, HealthRead, Search
        ],
        [ApplicationRoles.SUPPORT_ENGINEER] =
        [
            OrgsRead, UsersRead, UsersWrite, UsersUnlock, UsersPassword,
            TrialsRead, LicensesRead, EmailSend, SupportWorkspace, AuditRead, HealthRead, Search
        ],
        [ApplicationRoles.LICENSE_ADMIN] =
        [
            OrgsRead, TrialsRead, TrialsWrite, TrialsConvert,
            LicensesRead, LicensesWrite, LicensesRevoke,
            SupportWorkspace, AuditRead, HealthRead, Search, CommercialOps
        ],
        [ApplicationRoles.SALES_OPS] =
        [
            OrgsRead, TrialsRead, TrialsWrite, TrialsConvert,
            LicensesRead, LicensesWrite, CommercialOps, SupportWorkspace, AuditRead, HealthRead, Search
        ],
        [ApplicationRoles.COMPLIANCE_OPS] =
        [
            OrgsRead, UsersRead, TrialsRead, LicensesRead, AuditRead, AuditExport, HealthRead, Search
        ],
        [ApplicationRoles.COMMERCIAL_AUDITOR] =
        [
            OrgsRead, UsersRead, TrialsRead, LicensesRead, AuditRead, AuditExport, HealthRead, Search
        ],
    };

    private static HashSet<string> AllExcept(params string[] exclude)
    {
        var all = new HashSet<string>(StringComparer.Ordinal)
        {
            OrgsRead, OrgsWrite, OrgsDelete, OrgsPermanentDelete,
            UsersRead, UsersWrite, UsersUnlock, UsersPassword,
            TrialsRead, TrialsWrite, TrialsConvert,
            LicensesRead, LicensesWrite, LicensesRevoke,
            EmailSend, SupportWorkspace, CommercialOps,
            AuditRead, AuditExport, HealthRead, Search
        };
        foreach (var e in exclude)
        {
            all.Remove(e);
        }

        return all;
    }
}
