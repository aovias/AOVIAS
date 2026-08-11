namespace ComplianceServiceManager.ViewModels.Overview;

/// <summary>
/// Presentation model for overview activity feed rows.
/// </summary>
public sealed class ActivityFeedItemViewModel
{
    public required string Timestamp { get; init; }

    public required string Title { get; init; }

    public required string Message { get; init; }

    public required string Category { get; init; }

    public Aovias.DesignSystem.Enums.StatusKind StatusKind { get; init; } = Aovias.DesignSystem.Enums.StatusKind.Information;
}
