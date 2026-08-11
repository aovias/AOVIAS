namespace ComplianceService.Domain.Enums;

public enum MeasurementUnit
{
    Percent = 1,
    Rate = 2,
    Count = 3,
    Ratio = 4,
    Days = 5,
    Score = 6,
    Hours = 7,
}

public enum CalculationMethod
{
    Manual = 1,
    Formula = 2,
    Imported = 3,
    Derived = 4,
}

public enum MeasurementFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    Yearly = 5,
}

public enum IndicatorStatus
{
    Draft = 1,
    Active = 2,
    Inactive = 3,
    Archived = 4,
}

public enum IndicatorRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

public enum ThresholdBand
{
    Green = 1,
    Yellow = 2,
    Red = 3,
    Critical = 4,
    Unknown = 5,
}

public enum TrendDirection
{
    Improving = 1,
    Stable = 2,
    Declining = 3,
    Volatile = 4,
    Unknown = 5,
}

public enum QualityFrameworkMapping
{
    NABH = 1,
    JCI = 2,
    DNV = 3,
    ISO = 4,
    Other = 99,
}
