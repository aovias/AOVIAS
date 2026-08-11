using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Entities;

public class SecurityEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public SecurityEventSeverity Severity { get; set; }
    public Guid? UserId { get; set; }
    public Guid? SessionId { get; set; }
    public string? DeviceId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTime OccurredAt { get; set; }
}

public class UserRiskScore
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Score { get; set; }
    public RiskLevel Level { get; set; }
    public string FactorsJson { get; set; } = "[]";
    public DateTime CalculatedAt { get; set; }
}

public class SessionHistory
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public SessionHistoryAction Action { get; set; }
    public string? Detail { get; set; }
    public string? IPAddress { get; set; }
    public string? GeoLocation { get; set; }
    public int? RiskScore { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class SecurityIncident
{
    public Guid Id { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public SecurityIncidentStatus Status { get; set; }
    public Guid? UserId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? RelatedEventIdsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool IsDeleted { get; set; }
}
