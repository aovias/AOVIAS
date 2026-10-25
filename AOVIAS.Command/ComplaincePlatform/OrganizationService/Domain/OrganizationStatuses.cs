namespace OrganizationService.Domain;

/// <summary>Canonical organization commercial lifecycle statuses (CRP Phase 4).</summary>
public static class OrganizationStatuses
{
    public const string Active = "Active";
    public const string Suspended = "Suspended";
    public const string Archived = "Archived";
    public const string Deleted = "Deleted";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Active, Suspended, Archived, Deleted
    };
}
