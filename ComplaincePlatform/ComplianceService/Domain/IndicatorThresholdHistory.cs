using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

/// <summary>Immutable threshold configuration history — never overwrite prior rows.</summary>
public class IndicatorThresholdHistory : BaseEntity
{
    public Guid IndicatorId { get; set; }
    public ClinicalIndicator? Indicator { get; set; }

    public decimal? TargetValue { get; set; }
    public decimal? GreenThreshold { get; set; }
    public decimal? YellowThreshold { get; set; }
    public decimal? RedThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public bool LowerIsBetter { get; set; }

    public string? Reason { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
}
