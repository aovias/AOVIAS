namespace OrganizationService.Domain.Entities;

public enum LicenseType
{
    Trial = 0,
    Subscription = 1,
    Perpetual = 2,
    Offline = 3,
    /// <summary>CRP Phase 6 — enterprise commercial license.</summary>
    Enterprise = 4,
    /// <summary>CRP Phase 6 — time-boxed evaluation (non-trial catalog path).</summary>
    Evaluation = 5,
    /// <summary>CRP Phase 6 — developer / sandbox license.</summary>
    Developer = 6
}

/// <summary>
/// Operational license lifecycle. Mission aliases: Activated/Operational map to <see cref="Active"/>.
/// </summary>
public enum LicenseStatus
{
    Issued = 0,
    /// <summary>Operational (mission: Activated / Operational).</summary>
    Active = 1,
    Grace = 2,
    Expired = 3,
    Revoked = 4,
    /// <summary>CRP Phase 6 — manually suspended; not operational.</summary>
    Suspended = 5,
    /// <summary>CRP Phase 6 — archived for history; not operational.</summary>
    Archived = 6
}

/// <summary>Derived org commercial display status (tenancy stays on Organization; commercial view from LicenseKey).</summary>
public enum OrganizationCommercialDisplayStatus
{
    Trial = 0,
    Operational = 1,
    ReadOnly = 2,
    Suspended = 3,
    Archived = 4,
    Unknown = 5
}

/// <summary>Issued commercial license key bound to an organization (Phase 6 SSOT).</summary>
public sealed class LicenseKey
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    /// <summary>Optional link to catalogue OrganizationSubscription row (metadata only).</summary>
    public Guid? OrganizationSubscriptionId { get; set; }
    public Guid? SubscriptionPlanId { get; set; }
    public string KeyCode { get; set; } = string.Empty;
    public LicenseType LicenseType { get; set; } = LicenseType.Subscription;
    public LicenseStatus Status { get; set; } = LicenseStatus.Issued;
    public int MaxSeats { get; set; } = 10;
    public int UsedSeats { get; set; }
    public int GracePeriodDays { get; set; } = 14;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? GraceEndsAtUtc { get; set; }
    /// <summary>
    /// Entitlement payload: legacy string[] of codes, or array of
    /// { "code", "kind", "value", "expiresAtUtc?" } objects (CRP Phase 6).
    /// </summary>
    public string EntitlementsJson { get; set; } = "[]";
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    /// <summary>CRP Phase 4 — how many times this trial has been extended.</summary>
    public int ExtensionCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class LicenseActivation
{
    public Guid Id { get; set; }
    public Guid LicenseKeyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string MachineFingerprint { get; set; } = string.Empty;
    public string ActivationMode { get; set; } = "Online"; // Online | Offline
    public string? OfflinePayload { get; set; }
    public string? Signature { get; set; }
    public DateTime ActivatedAtUtc { get; set; }
    public DateTime? DeactivatedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ActivatedBy { get; set; }
}

public sealed class LicenseSeatAssignment
{
    public Guid Id { get; set; }
    public Guid LicenseKeyId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class LicenseHeartbeat
{
    public Guid Id { get; set; }
    public Guid LicenseKeyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string MachineFingerprint { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }
    public string? ProductVersion { get; set; }
    public string StatusSnapshot { get; set; } = string.Empty;
    public int SeatUsage { get; set; }
}
