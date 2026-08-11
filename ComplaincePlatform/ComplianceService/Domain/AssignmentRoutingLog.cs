using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Sprint 9B — analytics foundation for routing decisions.</summary>
public class AssignmentRoutingLog : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public AssignmentRoutingRuleType RuleApplied { get; set; }
    public string? RecommendedUserId { get; set; }
    public string? AppliedUserId { get; set; }
    public AssignmentConfidence Confidence { get; set; }
    public string RoutingReason { get; set; } = string.Empty;
    public bool WasAutoAssigned { get; set; }
    public bool WasOverridden { get; set; }
}
