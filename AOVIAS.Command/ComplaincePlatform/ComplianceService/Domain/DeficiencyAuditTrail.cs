using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Sprint 8 — deficiency operational timeline (mirrors finding audit trail pattern).</summary>
public class DeficiencyAuditTrail : BaseEntity
{
    public Guid DeficiencyId { get; set; }

    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string ActorType { get; set; } = "User";
    public string? DetailJson { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
