namespace IdentityService.Domain.Enums;

public enum SessionStatus
{
    Active = 0,
    Revoked = 1,
    Expired = 2,
    ReuseDetected = 3
}
