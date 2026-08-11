namespace OrganizationService.Domain.Enums;

public static class MembershipStatuses
{
    public const string Draft = "Draft";
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Suspended = "Suspended";
    public const string Expired = "Expired";
    public const string Archived = "Archived";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Draft, Pending, Active, Suspended, Expired, Archived
    };
}
