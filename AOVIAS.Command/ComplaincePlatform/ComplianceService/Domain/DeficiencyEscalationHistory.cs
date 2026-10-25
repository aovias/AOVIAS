using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Sprint 10 — full escalation audit trail per deficiency.</summary>
public class DeficiencyEscalationHistory : BaseEntity
{
    public Guid DeficiencyId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public int Level { get; set; }
    public string? TargetUserId { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public EscalationState State { get; set; } = EscalationState.Pending;
    public DateTime TriggeredAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
}
