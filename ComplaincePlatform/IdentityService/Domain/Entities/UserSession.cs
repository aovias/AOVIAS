using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Entities;

public class UserSession
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public Guid TokenFamilyId { get; set; }
    public string? DeviceId { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? GeoLocation { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public int RiskScore { get; set; }
    public bool IsSuspicious { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public SessionStatus Status { get; set; }
    public string? AccessTokenJti { get; set; }
}
