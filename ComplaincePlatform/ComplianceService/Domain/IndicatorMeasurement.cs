using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain;

public class IndicatorMeasurement : BaseEntity
{
    public Guid IndicatorId { get; set; }
    public ClinicalIndicator? Indicator { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public MeasurementFrequency Frequency { get; set; }

    public decimal? Numerator { get; set; }
    public decimal? Denominator { get; set; }
    public decimal Value { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public string? Source { get; set; }
    public string? Notes { get; set; }
}
