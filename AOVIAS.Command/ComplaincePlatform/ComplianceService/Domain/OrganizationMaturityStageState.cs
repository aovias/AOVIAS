using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>
/// Persisted maturity stage progression for a compliance journey.
/// Updated by the automated stage unlocking engine — not assessment-only flags.
/// </summary>
public class OrganizationMaturityStageState : BaseEntity
{
    public Guid JourneyId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid FrameworkId { get; set; }
    public ObjectiveElementClassification Stage { get; set; }
    public MaturityStageLifecycleState State { get; set; }
    public decimal ProgressPercent { get; set; }
    public decimal ReadinessPercent { get; set; }
    public decimal UnlockReadinessPercent { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastEvaluatedAt { get; set; }
    /// <summary>JSON array of human-readable blocking reasons when locked.</summary>
    public string? BlockingReasonsJson { get; set; }
    /// <summary>JSON array of next-milestone recommendations.</summary>
    public string? RecommendationsJson { get; set; }
}
