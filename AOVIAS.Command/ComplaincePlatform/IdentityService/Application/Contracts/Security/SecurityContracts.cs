using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;

namespace IdentityService.Application.Contracts.Security;

public sealed class SecurityEventDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string Summary { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string? PayloadJson { get; init; }
}

public sealed class SecurityTimelineDto
{
    public Guid Id { get; init; }
    public string Source { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}

public sealed class IdentityAuditLogDto
{
    public Guid Id { get; init; }
    public DateTime OccurredAt { get; init; }
    public Guid? ActorUserId { get; init; }
    public Guid? TargetUserId { get; init; }
    public string EventCode { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? IPAddress { get; init; }
    public string? CorrelationId { get; init; }
    public string Outcome { get; init; } = "Success";
    public string? Reason { get; init; }
}

public interface IIdentityAuditReadService
{
    Task<IReadOnlyList<IdentityAuditLogDto>> ListAsync(DateTime? from, DateTime? to, int take = 200, CancellationToken ct = default);
}

public sealed class UserRiskScoreDto
{
    public Guid UserId { get; init; }
    public int Score { get; init; }
    public string Level { get; init; } = string.Empty;
    public string FactorsJson { get; init; } = "[]";
    public DateTime CalculatedAt { get; init; }
}

public sealed class TrustedDeviceDto
{
    public Guid Id { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public string? Nickname { get; init; }
    public string? Platform { get; init; }
    public string? Browser { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public string VerificationStatus { get; init; } = string.Empty;
    public DateTime TrustedUntil { get; init; }
    public DateTime LastSeenAt { get; init; }
}

public sealed class SecurityDashboardDto
{
    public int ActiveSessions { get; init; }
    public int LockedAccounts { get; init; }
    public int HighRiskUsers { get; init; }
    public int FailedLogins24h { get; init; }
    public int MfaEnabledUsers { get; init; }
    public int TotalUsers { get; init; }
    public int TrustedDevices { get; init; }
    public int OpenIncidents { get; init; }
}

public sealed class UpdateSecurityPolicyRequest
{
    public int? DeviceTrustDurationDays { get; init; }
    public int? RiskThresholdMedium { get; init; }
    public int? RiskThresholdHigh { get; init; }
    public int? ImpossibleTravelMaxKmPerHour { get; init; }
    public bool? RequireMfaOnUnknownDevice { get; init; }
    public int? SessionIdleTimeoutMinutes { get; init; }
    public bool? EmergencyAccessEnabled { get; init; }
    public int? MaxConcurrentSessions { get; init; }
}

public sealed class RevokeDeviceRequest
{
    public Guid DeviceRecordId { get; init; }
}

public sealed class RevokeSecuritySessionRequest
{
    public Guid SessionId { get; init; }
}

public sealed class AdaptiveAuthContext
{
    public Guid UserId { get; init; }
    public string? DeviceId { get; init; }
    public string? IPAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? GeoLocation { get; init; }
}

public interface ISecurityEventService
{
    Task RecordAsync(string eventType, SecurityEventSeverity severity, string summary, Guid? userId = null, Guid? sessionId = null, string? deviceId = null, string? ipAddress = null, object? payload = null, CancellationToken ct = default);
    Task<IReadOnlyList<SecurityEventDto>> ListAsync(Guid? userId, int page, int pageSize, CancellationToken ct = default);
    Task WriteOutboxAsync(string type, Guid aggregateId, object payload, CancellationToken ct = default);
}

public interface IRiskScoringService
{
    Task<UserRiskScoreDto> CalculateAndPersistAsync(Guid userId, CancellationToken ct = default);
    Task<UserRiskScoreDto?> GetLatestAsync(Guid userId, CancellationToken ct = default);
    RiskLevel ScoreToLevel(int score, UserSecurityPolicy policy);
}

public interface IThreatDetectionService
{
    Task EvaluateLoginAsync(Guid userId, string? ipAddress, string? geoLocation, Guid? sessionId, CancellationToken ct = default);
    Task EvaluateRefreshReuseAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
}

public interface ITrustedDeviceService
{
    Task RegisterOrUpdateAsync(Guid userId, string deviceId, string? userAgent, string? ipAddress, bool trustDevice, CancellationToken ct = default);
    Task<bool> IsTrustedAsync(Guid userId, string deviceId, CancellationToken ct = default);
    Task<IReadOnlyList<TrustedDeviceDto>> ListAsync(Guid userId, CancellationToken ct = default);
    Task RevokeAsync(Guid userId, Guid deviceRecordId, CancellationToken ct = default);
}

public interface IAdaptiveAuthenticationService
{
    Task<AdaptiveAuthDecision> EvaluateLoginAsync(AdaptiveAuthContext context, CancellationToken ct = default);
}

public interface ISecurityTimelineService
{
    Task<IReadOnlyList<SecurityTimelineDto>> GetTimelineAsync(Guid? userId, int take = 100, CancellationToken ct = default);
}

public interface ISecurityDashboardService
{
    Task<SecurityDashboardDto> GetSummaryAsync(CancellationToken ct = default);
}

public interface ISessionHistoryService
{
    Task AppendAsync(Guid sessionId, Guid userId, SessionHistoryAction action, string? detail = null, string? ip = null, string? geo = null, int? riskScore = null, CancellationToken ct = default);
}

public interface ISecurityPolicyAdminService
{
    Task<UserSecurityPolicy> UpdatePolicyAsync(UpdateSecurityPolicyRequest request, CancellationToken ct = default);
}
