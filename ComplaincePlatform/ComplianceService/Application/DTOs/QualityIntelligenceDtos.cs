namespace ComplianceService.Application.DTOs;

public class QualityDashboardDto
{
    public decimal OverallQualityScore { get; set; }
    public int IndicatorsWithinTarget { get; set; }
    public int WarningIndicators { get; set; }
    public int CriticalIndicators { get; set; }
    public int ImprovingTrends { get; set; }
    public int DecliningTrends { get; set; }
    public int ActiveIndicators { get; set; }
    public decimal PatientSafetyScore { get; set; }
    public decimal AccreditationRiskScore { get; set; }
    public decimal CapaEffectivenessScore { get; set; }
    public IReadOnlyList<QualityCountByLabelDto> ByCategory { get; set; } = Array.Empty<QualityCountByLabelDto>();
    public IReadOnlyList<QualityCountByLabelDto> ByDepartment { get; set; } = Array.Empty<QualityCountByLabelDto>();
    public IReadOnlyList<QualityCountByLabelDto> ByThresholdStatus { get; set; } = Array.Empty<QualityCountByLabelDto>();
}

public class QualityCountByLabelDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal? Value { get; set; }
}

public class ClinicalIndicatorListDto
{
    public IReadOnlyList<ClinicalIndicatorListItemDto> Items { get; set; } = Array.Empty<ClinicalIndicatorListItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ClinicalIndicatorListItemDto
{
    public Guid Id { get; set; }
    public string IndicatorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string MeasurementUnit { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public decimal? CurrentValue { get; set; }
    public decimal? TargetValue { get; set; }
    public string ThresholdStatus { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public DateTime? LastCalculatedAt { get; set; }
}

public class ClinicalIndicatorDetailDto : ClinicalIndicatorListItemDto
{
    public string Description { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public string? NumeratorDefinition { get; set; }
    public string? DenominatorDefinition { get; set; }
    public string? FormulaExpression { get; set; }
    public decimal? CurrentNumerator { get; set; }
    public decimal? CurrentDenominator { get; set; }
    public decimal? GreenThreshold { get; set; }
    public decimal? YellowThreshold { get; set; }
    public decimal? RedThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public bool LowerIsBetter { get; set; }
    public string FrameworkMapping { get; set; } = string.Empty;
    public string? FrameworkReference { get; set; }
    public Guid? ObjectiveElementId { get; set; }
    public string? DataSource { get; set; }
    public string? Comments { get; set; }
    public IReadOnlyList<IndicatorMeasurementDto> TrendHistory { get; set; } = Array.Empty<IndicatorMeasurementDto>();
    public IReadOnlyList<IndicatorThresholdHistoryDto> ThresholdHistory { get; set; } = Array.Empty<IndicatorThresholdHistoryDto>();
    public IReadOnlyList<IndicatorAuditTrailDto> LifecycleHistory { get; set; } = Array.Empty<IndicatorAuditTrailDto>();
    public IReadOnlyList<QualityLinkedEntityDto> RelatedIncidents { get; set; } = Array.Empty<QualityLinkedEntityDto>();
    public IReadOnlyList<QualityLinkedEntityDto> RelatedFindings { get; set; } = Array.Empty<QualityLinkedEntityDto>();
    public IReadOnlyList<QualityLinkedEntityDto> RelatedDeficiencies { get; set; } = Array.Empty<QualityLinkedEntityDto>();
    public IReadOnlyList<QualityLinkedEntityDto> RelatedCapas { get; set; } = Array.Empty<QualityLinkedEntityDto>();
    public IReadOnlyList<QualityLinkedEntityDto> RelatedDocuments { get; set; } = Array.Empty<QualityLinkedEntityDto>();
    public IReadOnlyList<QualityInvestigationRecommendationDto> InvestigationRecommendations { get; set; } = Array.Empty<QualityInvestigationRecommendationDto>();
    public QualityComplianceContextDto? ComplianceContext { get; set; }
}

public class IndicatorMeasurementDto
{
    public Guid Id { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public decimal? Numerator { get; set; }
    public decimal? Denominator { get; set; }
    public decimal Value { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class IndicatorThresholdHistoryDto
{
    public Guid Id { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? GreenThreshold { get; set; }
    public decimal? YellowThreshold { get; set; }
    public decimal? RedThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public bool LowerIsBetter { get; set; }
    public string? Reason { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime EffectiveFrom { get; set; }
}

public class IndicatorAuditTrailDto
{
    public string Action { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? ActorName { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class QualityLinkedEntityDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? SubLabel { get; set; }
    public string? DeepLink { get; set; }
}

public class QualityInvestigationRecommendationDto
{
    public string RecommendationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
    public bool IsAdvisory { get; set; } = true;
}

public class QualityComplianceContextDto
{
    public Guid? ObjectiveElementId { get; set; }
    public string? ObjectiveElementName { get; set; }
    public string? ClauseReference { get; set; }
    public decimal? ReadinessImpact { get; set; }
    public string? VerificationStatus { get; set; }
}

public class PatchIndicatorThresholdsRequestDto
{
    public Guid IndicatorId { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? GreenThreshold { get; set; }
    public decimal? YellowThreshold { get; set; }
    public decimal? RedThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public bool? LowerIsBetter { get; set; }
    public string? Reason { get; set; }
}

public class DepartmentScorecardDto
{
    public string Department { get; set; } = string.Empty;
    public decimal QualityScore { get; set; }
    public decimal ClinicalIndicatorScore { get; set; }
    public int IncidentRate { get; set; }
    public decimal CapaCompletionPercent { get; set; }
    public decimal SlaHealthPercent { get; set; }
    public decimal EvidenceCompletenessPercent { get; set; }
    public decimal DocumentationHealthPercent { get; set; }
    public int IndicatorsWithinTarget { get; set; }
    public int WarningIndicators { get; set; }
    public int CriticalIndicators { get; set; }
    public string ExecutiveSummary { get; set; } = string.Empty;
}

public class QualityTrendsDto
{
    public string Period { get; set; } = string.Empty;
    public IReadOnlyList<QualityTrendSeriesDto> Series { get; set; } = Array.Empty<QualityTrendSeriesDto>();
}

public class QualityTrendSeriesDto
{
    public Guid IndicatorId { get; set; }
    public string IndicatorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public IReadOnlyList<IndicatorMeasurementDto> Points { get; set; } = Array.Empty<IndicatorMeasurementDto>();
    public decimal? MovingAverage { get; set; }
}

public class QualityBenchmarkingDto
{
    public string ComparisonPeriod { get; set; } = string.Empty;
    public IReadOnlyList<QualityBenchmarkItemDto> Items { get; set; } = Array.Empty<QualityBenchmarkItemDto>();
    public IReadOnlyList<QualityDepartmentComparisonDto> DepartmentComparison { get; set; } = Array.Empty<QualityDepartmentComparisonDto>();
}

public class QualityBenchmarkItemDto
{
    public Guid IndicatorId { get; set; }
    public string IndicatorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? CurrentValue { get; set; }
    public decimal? PreviousValue { get; set; }
    public decimal? ChangePercent { get; set; }
    public decimal? TargetValue { get; set; }
    public string Trend { get; set; } = string.Empty;
}

public class QualityDepartmentComparisonDto
{
    public string Department { get; set; } = string.Empty;
    public decimal QualityScore { get; set; }
    public decimal AverageIndicatorValue { get; set; }
    public int IndicatorCount { get; set; }
}

public class QualityExecutiveDto
{
    public IReadOnlyList<QualityExecutiveRiskDto> TopRisks { get; set; } = Array.Empty<QualityExecutiveRiskDto>();
    public IReadOnlyList<QualityExecutiveImprovementDto> TopImprovements { get; set; } = Array.Empty<QualityExecutiveImprovementDto>();
    public IReadOnlyList<string> DepartmentsNeedingAttention { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> IndicatorsThreateningAccreditation { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> RecurringProblems { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ImprovingDepartments { get; set; } = Array.Empty<string>();
    public string MonthlyQualitySummary { get; set; } = string.Empty;
    public decimal OverallQualityScore { get; set; }
    public decimal AccreditationRiskScore { get; set; }
}

public class QualityExecutiveRiskDto
{
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
}

public class QualityExecutiveImprovementDto
{
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal ChangePercent { get; set; }
    public string? DeepLink { get; set; }
}

public class QualityAnalyticsDto
{
    public decimal OverallQualityScore { get; set; }
    public decimal AccreditationRiskScore { get; set; }
    public decimal JourneyReadiness { get; set; }
    public decimal CapaEffectiveness { get; set; }
    public IReadOnlyList<QualityCountByLabelDto> IndicatorHealth { get; set; } = Array.Empty<QualityCountByLabelDto>();
    public IReadOnlyList<QualityCountByLabelDto> DepartmentHeatmap { get; set; } = Array.Empty<QualityCountByLabelDto>();
    public IReadOnlyList<QualityTrendSeriesDto> QualityTrends { get; set; } = Array.Empty<QualityTrendSeriesDto>();
    public string AnalyticsSource { get; set; } = "AnalyticsHub+ClinicalIndicatorMetrics";
}
