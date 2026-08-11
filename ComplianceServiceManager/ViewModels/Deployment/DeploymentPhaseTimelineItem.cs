using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Selectable deployment phase timeline row. Status glyphs follow the workspace vocabulary:
/// ○ waiting, ◐ running, ✔ completed, ⚠ warning, ✖ failed, ↻ retrying.
/// </summary>
public sealed partial class DeploymentPhaseTimelineItem : ObservableObject
{
    private string? lastTrackedStatus;

    public required string Name { get; init; }

    [ObservableProperty]
    private string status = "Waiting";

    [ObservableProperty]
    private string durationDisplay = "--:--";

    [ObservableProperty]
    private string startDisplay = "—";

    [ObservableProperty]
    private string endDisplay = "—";

    [ObservableProperty]
    private int retryCount;

    public ObservableCollection<string> Logs { get; } = [];

    public string StatusGlyph => Status switch
    {
        "Passed" or "Completed" => "✔",
        "Running" => "◐",
        "Failed" => "✖",
        "Warning" => "⚠",
        "Retrying" => "↻",
        _ => "○"
    };

    public string RetriesDisplay => RetryCount <= 0 ? "0 retries" : $"{RetryCount} retries";

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(StatusGlyph));

        // Only record timing transitions once per distinct status to avoid notification storms.
        if (string.Equals(lastTrackedStatus, value, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        lastTrackedStatus = value;
        var now = DateTimeOffset.Now.ToString("HH:mm:ss");
        if (string.Equals(value, "Running", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Retrying", StringComparison.OrdinalIgnoreCase))
        {
            if (StartDisplay is "—" or "")
            {
                StartDisplay = now;
            }

            if (string.Equals(value, "Retrying", StringComparison.OrdinalIgnoreCase))
            {
                RetryCount++;
            }
        }
        else if (string.Equals(value, "Passed", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(value, "Completed", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(value, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            EndDisplay = now;
        }
    }

    partial void OnRetryCountChanged(int value) => OnPropertyChanged(nameof(RetriesDisplay));
}
