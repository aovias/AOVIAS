using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class EscalationLevel : BaseEntity
{
    public Guid EscalationPolicyId { get; set; }
    public int Level { get; set; }
    public EscalationTargetType TargetType { get; set; }
    public string? TargetDesignation { get; set; }
    public string? TargetUserId { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
    public int TriggerAtSlaPercent { get; set; } = 100;

    public EscalationPolicy? Policy { get; set; }
}
