namespace ComplianceServiceManager.Models;

public sealed class FixExecutionResult
{
    public bool Success { get; init; }

    public required string Summary { get; init; }

    public required string CheckpointReference { get; init; }

    public required IReadOnlyList<string> ModifiedFiles { get; init; }

    public required IReadOnlyList<string> Actions { get; init; }
}
