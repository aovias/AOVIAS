namespace ComplianceServiceManager.Models;

public sealed class RepairHistoryEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public required string ServiceName { get; init; }

    public required RepairStage Stage { get; init; }

    public required bool Success { get; init; }

    public required string Summary { get; init; }

    public required string SessionPath { get; init; }

    public required string CheckpointReference { get; init; }

    public required FailureCategory FailureCategory { get; init; }
}
