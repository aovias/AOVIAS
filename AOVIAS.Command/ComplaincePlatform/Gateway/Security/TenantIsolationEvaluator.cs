namespace Gateway.Security;

/// <summary>
/// Pure helper that compares JWT tenant claims against request-supplied org/hospital identifiers.
/// Used by <see cref="Middleware.TenantIsolationMiddleware"/> and unit tests.
/// </summary>
public static class TenantIsolationEvaluator
{
    public static readonly string[] OrganizationKeyNames =
    [
        "organizationid",
        "organization_id",
        "orgid",
        "org_id",
        "tenantid",
        "tenant_id"
    ];

    public static readonly string[] HospitalKeyNames =
    [
        "hospitalid",
        "hospital_id"
    ];

    public sealed record Candidate(string Source, string Name, string Value);

    public sealed record Conflict(string Source, string Name, string RequestedValue, string ClaimValue, string ClaimType);

    /// <summary>
    /// Returns the first conflicting candidate, or null when the request is tenant-safe.
    /// Empty/missing JWT claims skip enforcement for that claim type (auth policies still apply).
    /// </summary>
    public static Conflict? FindConflict(
        Guid? jwtOrganizationId,
        Guid? jwtHospitalId,
        IEnumerable<Candidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.Value))
                continue;

            if (!Guid.TryParse(candidate.Value, out var requested) || requested == Guid.Empty)
                continue;

            var key = NormalizeKey(candidate.Name);

            if (jwtOrganizationId is { } orgId && IsOrganizationKey(key) && requested != orgId)
            {
                return new Conflict(
                    candidate.Source,
                    candidate.Name,
                    requested.ToString(),
                    orgId.ToString(),
                    "organization_id");
            }

            if (jwtHospitalId is { } hospitalId && IsHospitalKey(key) && requested != hospitalId)
            {
                return new Conflict(
                    candidate.Source,
                    candidate.Name,
                    requested.ToString(),
                    hospitalId.ToString(),
                    "hospital_id");
            }
        }

        return null;
    }

    public static bool IsOrganizationKey(string normalizedKey)
        => OrganizationKeyNames.Contains(normalizedKey, StringComparer.Ordinal)
           || normalizedKey.EndsWith("organizationid", StringComparison.Ordinal)
           || normalizedKey.EndsWith("orgid", StringComparison.Ordinal)
           || normalizedKey.EndsWith("tenantid", StringComparison.Ordinal);

    public static bool IsHospitalKey(string normalizedKey)
        => HospitalKeyNames.Contains(normalizedKey, StringComparer.Ordinal)
           || normalizedKey.EndsWith("hospitalid", StringComparison.Ordinal);

    public static string NormalizeKey(string name)
        => name.Trim().ToLowerInvariant().Replace("-", string.Empty).Replace("_", string.Empty);
}
