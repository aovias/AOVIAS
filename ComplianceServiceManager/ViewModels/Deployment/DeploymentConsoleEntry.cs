namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Operator-facing deployment console entry (Sprint 12 D8/D12).
/// </summary>
public sealed class DeploymentConsoleEntry
{
    public required DateTimeOffset Timestamp { get; init; }

    public string? ServiceId { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public required string Reason { get; init; }

    public required string RecoveryAction { get; init; }

    public string? HttpStatus { get; init; }

    public string Summary => string.IsNullOrWhiteSpace(ServiceId)
        ? $"{Title} — {Status}"
        : $"{ServiceId} — {Title} — {Status}";
}
