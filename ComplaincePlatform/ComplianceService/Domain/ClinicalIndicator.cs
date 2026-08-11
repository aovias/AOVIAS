using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Sprint 17 — Clinical indicator aggregate (continuous quality measurement).
/// Indicators are measurements, not incidents; threshold breaches are advisory only.
/// </summary>
public class ClinicalIndicator : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? HospitalId { get; set; }

    public string IndicatorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public IndicatorCategory Category { get; set; } = IndicatorCategory.Other;
    public FindingDepartment Department { get; set; } = FindingDepartment.Clinical;

    public MeasurementUnit MeasurementUnit { get; set; } = MeasurementUnit.Percent;
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Formula;
    public string? NumeratorDefinition { get; set; }
    public string? DenominatorDefinition { get; set; }
    public string? FormulaExpression { get; set; }

    public decimal? CurrentNumerator { get; set; }
    public decimal? CurrentDenominator { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? TargetValue { get; set; }

    public decimal? GreenThreshold { get; set; }
    public decimal? YellowThreshold { get; set; }
    public decimal? RedThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    /// <summary>When true, lower values are better (e.g. infection rate).</summary>
    public bool LowerIsBetter { get; set; }

    public TrendDirection Trend { get; set; } = TrendDirection.Unknown;
    public MeasurementFrequency Frequency { get; set; } = MeasurementFrequency.Monthly;

    public Guid? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }

    public QualityFrameworkMapping FrameworkMapping { get; set; } = QualityFrameworkMapping.NABH;
    public string? FrameworkReference { get; set; }
    public Guid? ObjectiveElementId { get; set; }

    public IndicatorStatus Status { get; set; } = IndicatorStatus.Active;
    public IndicatorRiskLevel RiskLevel { get; set; } = IndicatorRiskLevel.Medium;
    public ThresholdBand ThresholdStatus { get; set; } = ThresholdBand.Unknown;

    public string? DataSource { get; set; }
    public DateTime? LastCalculatedAt { get; set; }
    public string? Comments { get; set; }

    public string? RelatedDocumentationJson { get; set; }
    public string? RelatedIncidentCategoriesJson { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? LastUpdatedByUserId { get; set; }

    public ICollection<IndicatorMeasurement> Measurements { get; set; } = new List<IndicatorMeasurement>();
    public ICollection<IndicatorThresholdHistory> ThresholdHistory { get; set; } = new List<IndicatorThresholdHistory>();
    public ICollection<IndicatorAuditTrail> AuditTrail { get; set; } = new List<IndicatorAuditTrail>();
}
