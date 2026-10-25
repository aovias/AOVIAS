namespace IdentityService.Domain.Enums;

public enum UserAccountStatus
{
    PendingVerification = 0,
    Active = 1,
    Suspended = 2,
    Disabled = 3,
    Archived = 4,
    Deleted = 5
}
