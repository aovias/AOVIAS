namespace ComplianceService.Infrastructure.Configs;

public sealed class MigrationSafetyOptions
{
    public const string SectionName = "MigrationSafety";

    public bool UseUnifiedFinding { get; set; } = false;
    public bool EnableShadowMode { get; set; } = false;
    public bool EnableScoringParity { get; set; } = true;

    public decimal ScoreDeviationThresholdPercent { get; set; } = 2.0m;
    public decimal MismatchRateThresholdPercent { get; set; } = 1.0m;
}
