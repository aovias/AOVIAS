namespace ComplianceService.Domain.Enums;

/// <summary>
/// Verification state of an obligation's compliance claim.
/// </summary>
public enum VerificationStatus
{
    NotVerified = 0,
    Pending = 1,
    Verified = 2,
    Rejected = 3
}
