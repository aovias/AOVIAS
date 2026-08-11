using Aovias.DesignSystem.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.ViewModels.ReleaseCenter;

/// <summary>
/// Live deployment timeline row for SignalR-driven console output.
/// </summary>
public sealed class DeploymentTimelineItem
{
    public required string Timestamp { get; init; }

    public required string Title { get; init; }

    public required string Message { get; init; }

    public bool IsCurrent { get; init; }
}

/// <summary>
/// Visual deployment plan step for Release Center.
/// </summary>
public sealed partial class DeploymentPlanStepItem : ObservableObject
{
    public required string Name { get; init; }

    [ObservableProperty]
    private string status = "Pending";

    [ObservableProperty]
    private string? estimatedDuration;

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(StatusKind));
    }

    public StatusKind StatusKind => Status switch
    {
        "Completed" or "Success" => StatusKind.Healthy,
        "Running" => StatusKind.Information,
        "Warning" => StatusKind.Warning,
        "Failed" => StatusKind.Critical,
        _ => StatusKind.Neutral
    };
}
