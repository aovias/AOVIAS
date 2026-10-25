namespace ComplianceServiceManager.Models;

public sealed class RepairAnalyticsSnapshot
{
    public int TotalAttempts { get; init; }

    public int SuccessfulAttempts { get; init; }

    public int FailedAttempts { get; init; }

    public double SuccessRate { get; init; }

    public TimeSpan AverageRepairDuration { get; init; }

    public required IReadOnlyList<string> Recommendations { get; init; }
}
