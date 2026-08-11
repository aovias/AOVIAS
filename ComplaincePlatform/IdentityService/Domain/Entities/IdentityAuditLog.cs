namespace IdentityService.Domain.Entities;

public class IdentityAuditLog
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? TargetUserId { get; set; }
    public string EventCode { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? OperatingSystem { get; set; }
    public string? DeviceId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Outcome { get; set; } = "Success";
    public string? Reason { get; set; }
}
