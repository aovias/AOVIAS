using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Operations;

namespace ComplianceServiceManager.ViewModels.Operations;

/// <summary>
/// Reusable presentation-layer wrapper around one <see cref="OperationInstance"/>: everything
/// <c>OperationProgressHeader</c>, <c>OperationTimeline</c>, <c>OperationFailureCard</c>, and
/// <c>OperationLogPane</c> bind to. Any workspace that runs a long-running operation through
/// <see cref="OperationEngine"/> — Bootstrap today, Publish/Deploy/Rollback/Reconcile later —
/// embeds one of these; none of that logic is Bootstrap-specific.
///
/// This is the only layer in the Operation Engine stack that touches WPF (<see cref="DispatcherTimer"/>,
/// the UI-thread marshaling in <see cref="OnInstanceChanged"/>) — the engine core
/// (<c>ComplianceServiceManager.Operations</c>) has no WPF reference at all, matching the "Dispatcher
/// only inside the presentation layer" rule.
/// </summary>
public sealed partial class OperationViewModel : ObservableObject
{
    private readonly OperationEngine engine;
    private readonly DispatcherTimer elapsedTimer;
    private OperationInstance? instance;

    public OperationViewModel(OperationEngine engine)
    {
        this.engine = engine;
        CombinedLogView = CollectionViewSource.GetDefaultView(CombinedLog);
        CombinedLogView.Filter = FilterLog;

        elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        elapsedTimer.Tick += (_, _) => RefreshElapsedOnly();
    }

    [ObservableProperty]
    private int overallProgressPercent;

    [ObservableProperty]
    private string statusText = "Idle";

    [ObservableProperty]
    private string currentStageName = string.Empty;

    [ObservableProperty]
    private string currentTask = string.Empty;

    [ObservableProperty]
    private string elapsedDisplay = "0:00";

    [ObservableProperty]
    private string estimatedRemainingDisplay = string.Empty;

    [ObservableProperty]
    private bool hasFailure;

    [ObservableProperty]
    private string failureProblem = string.Empty;

    [ObservableProperty]
    private string failureCause = string.Empty;

    [ObservableProperty]
    private string failureRecommendedAction = string.Empty;

    [ObservableProperty]
    private string failureDetails = string.Empty;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private bool isTerminal;

    [ObservableProperty]
    private bool canRetry;

    [ObservableProperty]
    private bool canCancel;

    [ObservableProperty]
    private bool hasWarnings;

    [ObservableProperty]
    private string consoleSearchText = string.Empty;

    [ObservableProperty]
    private bool autoScrollConsole = true;

    public ObservableCollection<OperationStageViewModel> Stages { get; } = [];

    public ObservableCollection<string> CombinedLog { get; } = [];

    public ICollectionView CombinedLogView { get; }

    /// <summary>Raised once the attached instance reaches a terminal state — the wizard/caller uses
    /// this to switch to its success screen (or stay on the failure card) rather than polling.</summary>
    public event Action? Completed;

    partial void OnConsoleSearchTextChanged(string value) => CombinedLogView.Refresh();

    private bool FilterLog(object candidate)
        => candidate is string line
            && (string.IsNullOrWhiteSpace(ConsoleSearchText) || line.Contains(ConsoleSearchText, StringComparison.OrdinalIgnoreCase));

    /// <summary>Attaches to a freshly-started (or retried) <see cref="OperationInstance"/>, resetting all display state.</summary>
    public void Attach(OperationInstance newInstance)
    {
        Detach();
        instance = newInstance;

        Stages.Clear();
        foreach (var _ in newInstance.Descriptor.Stages)
        {
            Stages.Add(new OperationStageViewModel());
        }

        CombinedLog.Clear();
        HasFailure = false;
        newInstance.Changed += OnInstanceChanged;
        elapsedTimer.Start();
        RefreshFrom(newInstance);
    }

    private void Detach()
    {
        if (instance is not null)
        {
            instance.Changed -= OnInstanceChanged;
        }

        elapsedTimer.Stop();
    }

    private void OnInstanceChanged(OperationInstance changed) => RunOnUiThread(() => RefreshFrom(changed));

    private void RefreshElapsedOnly()
    {
        if (instance is not null)
        {
            ElapsedDisplay = FormatTimeSpan(instance.Elapsed);
        }
    }

    private void RefreshFrom(OperationInstance current)
    {
        OverallProgressPercent = (int)Math.Round(current.OverallProgress * 100);
        StatusText = current.Status.ToString();
        CurrentStageName = current.Stages.FirstOrDefault(stage => stage.Definition.Id == current.CurrentStageId)?.Definition.Name
            ?? (current.Status == OperationStatus.Running ? string.Empty : current.Status.ToString());
        CurrentTask = current.CurrentTask ?? string.Empty;
        ElapsedDisplay = FormatTimeSpan(current.Elapsed);
        EstimatedRemainingDisplay = current.EstimatedRemaining is { } remaining ? FormatTimeSpan(remaining) : string.Empty;
        HasWarnings = current.HasWarnings;

        IsRunning = current.Status == OperationStatus.Running;
        IsTerminal = !IsRunning;
        CanCancel = IsRunning && current.Descriptor.SupportsCancel;
        CanRetry = current.Status == OperationStatus.Failed && current.Descriptor.SupportsRetry;

        for (var index = 0; index < current.Stages.Count && index < Stages.Count; index++)
        {
            Stages[index].UpdateFrom(current.Stages[index]);
        }

        var allLogEntries = current.Stages.SelectMany(stage => stage.Logs).ToList();
        while (CombinedLog.Count < allLogEntries.Count)
        {
            var entry = allLogEntries[CombinedLog.Count];
            CombinedLog.Add($"[{entry.Timestamp:HH:mm:ss}] [{entry.StageName}] {entry.Message}");
        }

        HasFailure = current.Status == OperationStatus.Failed && current.Failure is not null;
        if (current.Failure is not null)
        {
            FailureProblem = current.Failure.Problem;
            FailureCause = current.Failure.Cause;
            FailureRecommendedAction = current.Failure.RecommendedAction;
            FailureDetails = current.Failure.Details;
        }

        if (IsTerminal)
        {
            elapsedTimer.Stop();
            Completed?.Invoke();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (instance is not null)
        {
            engine.Cancel(instance);
        }
    }

    [RelayCommand]
    private void Retry()
    {
        if (instance is not null)
        {
            Attach(engine.Retry(instance));
        }
    }

    [RelayCommand]
    private void ClearConsole() => CombinedLog.Clear();

    private static string FormatTimeSpan(TimeSpan span)
        => span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}"
            : $"{span.Minutes}:{span.Seconds:D2}";

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }
}
