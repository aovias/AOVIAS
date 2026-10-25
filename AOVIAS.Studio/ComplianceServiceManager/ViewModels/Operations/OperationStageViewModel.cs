using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ComplianceServiceManager.Operations;

namespace ComplianceServiceManager.ViewModels.Operations;

/// <summary>
/// One row in <c>Aovias.DesignSystem.Controls.OperationTimeline</c> — exposes exactly the duck-typed
/// property set that control's item template binds to (<c>Name</c>, <c>DisplayDetail</c>,
/// <c>StatusText</c>, <c>DurationDisplay</c>, <c>FailureMessage</c>, <c>Logs</c>), populated from a
/// live <see cref="OperationStageState"/> each time <see cref="OperationViewModel"/> observes a
/// change. The design system itself never references this type or the Operation Engine — see
/// <c>OperationTimeline</c>'s remarks.
/// </summary>
public sealed partial class OperationStageViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string? currentTask;

    [ObservableProperty]
    private string statusText = "Pending";

    [ObservableProperty]
    private string durationDisplay = string.Empty;

    [ObservableProperty]
    private string failureMessage = string.Empty;

    public string DisplayDetail => string.IsNullOrWhiteSpace(CurrentTask) ? Description : CurrentTask!;

    public ObservableCollection<string> Logs { get; } = [];

    partial void OnCurrentTaskChanged(string? value) => OnPropertyChanged(nameof(DisplayDetail));

    partial void OnDescriptionChanged(string value) => OnPropertyChanged(nameof(DisplayDetail));

    /// <summary>Refreshes this row from the engine's live state. Log lines are only ever appended,
    /// never replaced — matching <see cref="OperationStageState.Logs"/>'s own append-only contract,
    /// so re-rendering never loses or reorders anything already shown.</summary>
    internal void UpdateFrom(OperationStageState state)
    {
        Name = state.Definition.Name;
        Description = state.Definition.Description;
        CurrentTask = state.CurrentTask;
        StatusText = state.Status.ToString();
        DurationDisplay = FormatDuration(state.Duration);
        FailureMessage = state.Failure?.Cause ?? string.Empty;

        while (Logs.Count < state.Logs.Count)
        {
            var entry = state.Logs[Logs.Count];
            Logs.Add($"[{entry.Timestamp:HH:mm:ss}] {entry.Message}");
        }
    }

    private static string FormatDuration(TimeSpan? duration)
    {
        if (duration is null)
        {
            return string.Empty;
        }

        var value = duration.Value;
        return value.TotalMinutes >= 1 ? $"{(int)value.TotalMinutes}:{value.Seconds:D2}" : $"{value.TotalSeconds:F1}s";
    }
}
