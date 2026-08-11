namespace IdentityService.Domain.Entities;

/// <summary>Platform-wide security policy defaults (single row or named policy).</summary>
public class UserSecurityPolicy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "Default";
    public int PasswordMinLength { get; set; } = 8;
    public bool PasswordRequireDigit { get; set; } = true;
    public bool PasswordRequireUppercase { get; set; } = true;
    public bool PasswordRequireLowercase { get; set; } = true;
    public bool PasswordRequireNonAlphanumeric { get; set; } = true;
    public int PasswordHistoryCount { get; set; } = 5;
    public int? PasswordMaxAgeDays { get; set; }
    public int LockoutMaxFailedAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenAbsoluteExpirationDays { get; set; } = 7;
    public int RefreshTokenSlidingExpirationMinutes { get; set; } = 1440;
    public int MaxConcurrentSessions { get; set; } = 10;
    public bool RequireMfaForAdminRoles { get; set; }
    public bool RequireMfaOnUnknownDevice { get; set; }
    public int DeviceTrustDurationDays { get; set; } = 30;
    public int RiskThresholdMedium { get; set; } = 40;
    public int RiskThresholdHigh { get; set; } = 70;
    public int ImpossibleTravelMaxKmPerHour { get; set; } = 800;
    public int? SessionIdleTimeoutMinutes { get; set; }
    public bool EmergencyAccessEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
