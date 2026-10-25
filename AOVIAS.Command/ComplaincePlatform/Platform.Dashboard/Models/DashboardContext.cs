namespace Platform.Dashboard.Models;

/// <summary>
/// Tenant-scoped request context passed to dashboard providers for filtering and contribution.
/// </summary>
public sealed class DashboardContext
{
    public Guid OrganizationId { get; init; }

    public Guid? HospitalId { get; init; }

    public Guid? FrameworkId { get; init; }

    public Guid UserId { get; init; }

    /// <summary>
    /// Permission strings granted to the current user. Used by the registry for provider/widget filtering.
    /// </summary>
    public IReadOnlySet<string> Permissions { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// When set, only providers supporting these application identifiers are included.
    /// </summary>
    public IReadOnlySet<string>? EnabledApplications { get; init; }

    public string? OrganizationName { get; init; }

    public string? HospitalName { get; init; }

    /// <summary>
    /// When true, bypasses cached summary payload for this request.
    /// </summary>
    public bool ForceRefresh { get; init; }
}
