using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Entities;

public class TrustedDevice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Platform { get; set; }
    public string? Browser { get; set; }
    public string? UserAgent { get; set; }
    public string? Fingerprint { get; set; }
    public string? OperatingSystem { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public DeviceVerificationStatus VerificationStatus { get; set; } = DeviceVerificationStatus.Verified;
    public DateTime TrustedUntil { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsDeleted { get; set; }
}
