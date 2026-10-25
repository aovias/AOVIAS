using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class IndicatorAuditTrail : BaseEntity
{
    public Guid IndicatorId { get; set; }
    public ClinicalIndicator? Indicator { get; set; }

    public string Action { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
