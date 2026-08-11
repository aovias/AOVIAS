using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Audit trail for automated maturity stage progression decisions.
/// </summary>
public class MaturityStageUnlockAuditEntry : BaseEntity
{
    public Guid JourneyId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }
    public ObjectiveElementClassification Stage { get; set; }
    public MaturityStageLifecycleState PreviousState { get; set; }
    public MaturityStageLifecycleState NewState { get; set; }
    public string TriggerSource { get; set; } = string.Empty;
    public Guid? TriggeredByUserId { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public string? CriteriaPassedJson { get; set; }
    public string? CriteriaFailedJson { get; set; }
}
