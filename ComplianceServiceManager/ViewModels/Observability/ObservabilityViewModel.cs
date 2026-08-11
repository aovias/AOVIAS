using System.Collections.ObjectModel;
using Aovias.Runtime.Contracts.Observability;
using Aovias.Runtime.SDK;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Observability;

namespace ComplianceServiceManager.ViewModels.Observability;

/// <summary>
/// Command Center view model for the native AOVIAS observability platform.
/// </summary>
public sealed partial class ObservabilityViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IObservabilityService observabilityService;
    private readonly IRuntimeHubClient runtimeHubClient;

    [ObservableProperty]
    private string connectionStatus = "Disconnected";

    [ObservableProperty]
    private string searchTerm = string.Empty;

    [ObservableProperty]
    private string selectedTimelineCategory = "All";

    [ObservableProperty]
    private RuntimeStateDto? runtimeState;

    [ObservableProperty]
    private RuntimeMetricsSnapshotDto? latestMetrics;

    [ObservableProperty]
    private string statusMessage = "Connecting to observability stream...";

    public ObservableCollection<RuntimeTimelineEntryDto> TimelineEntries { get; } = [];

    public ObservableCollection<ContainerObservationDto> Containers { get; } = [];

    public ObservableCollection<NetworkObservationDto> Networks { get; } = [];

    public ObservableCollection<ReleaseObservationDto> Releases { get; } = [];

    public ObservableCollection<WorkflowObservationDto> Workflows { get; } = [];

    public ObservableCollection<string> Capabilities { get; } = [];

    public IReadOnlyList<string> TimelineCategories { get; } =
    [
        "All", "Runtime", "Docker", "Workflow", "Deployment", "Health", "Configuration", "SignalR"
    ];

    public ObservabilityViewModel(IObservabilityService observabilityService, IRuntimeHubClient runtimeHubClient)
    {
        this.observabilityService = observabilityService;
        this.runtimeHubClient = runtimeHubClient;
        runtimeHubClient.RuntimeStateReceived += OnRuntimeStateReceived;
        runtimeHubClient.RuntimeTimelineReceived += OnRuntimeTimelineReceived;
        runtimeHubClient.RuntimeMetricsReceived += OnRuntimeMetricsReceived;
        runtimeHubClient.ContainerEventReceived += OnContainerEventReceived;
        runtimeHubClient.WorkflowUpdateReceived += OnWorkflowUpdateReceived;
        runtimeHubClient.DeploymentUpdateReceived += OnDeploymentUpdateReceived;
        runtimeHubClient.RuntimeHealthReceived += OnRuntimeHealthReceived;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            await runtimeHubClient.ConnectAsync();
            ConnectionStatus = "Connected";
            StatusMessage = "Observability stream connected.";

            RuntimeState = await observabilityService.GetStateAsync();
            ApplyState(RuntimeState);
            var metrics = await observabilityService.GetMetricsAsync(1);
            LatestMetrics = metrics.FirstOrDefault();
        }
        catch (Exception exception)
        {
            ConnectionStatus = "Connection failed";
            StatusMessage = exception.Message;
        }
    }

    [RelayCommand]
    private async Task SearchTimelineAsync()
    {
        var category = SelectedTimelineCategory == "All" ? null : SelectedTimelineCategory;
        var entries = await observabilityService.GetTimelineAsync(
            string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
            category,
            100);

        TimelineEntries.Clear();
        foreach (var entry in entries)
        {
            TimelineEntries.Add(entry);
        }
    }

    private void OnRuntimeStateReceived(object? sender, RuntimeStateDto state)
    {
        RuntimeState = state;
        ApplyState(state);
    }

    private void OnRuntimeMetricsReceived(object? sender, RuntimeMetricsSnapshotDto metrics)
        => LatestMetrics = metrics;

    private void OnRuntimeTimelineReceived(object? sender, RuntimeTimelineEntryDto entry)
        => InsertTimeline(entry);

    private void OnContainerEventReceived(object? sender, RuntimeTimelineEntryDto entry)
        => InsertTimeline(entry);

    private void OnWorkflowUpdateReceived(object? sender, RuntimeTimelineEntryDto entry)
        => InsertTimeline(entry);

    private void OnDeploymentUpdateReceived(object? sender, RuntimeTimelineEntryDto entry)
        => InsertTimeline(entry);

    private void OnRuntimeHealthReceived(object? sender, RuntimeHealthMessage message)
    {
        StatusMessage = $"{message.Status}: {message.Message}";
    }

    private void ApplyState(RuntimeStateDto state)
    {
        Containers.Clear();
        foreach (var container in state.Containers)
        {
            Containers.Add(container);
        }

        Networks.Clear();
        foreach (var network in state.Networks)
        {
            Networks.Add(network);
        }

        Releases.Clear();
        foreach (var release in state.Releases)
        {
            Releases.Add(release);
        }

        Workflows.Clear();
        foreach (var workflow in state.Workflows)
        {
            Workflows.Add(workflow);
        }

        Capabilities.Clear();
        foreach (var capability in state.Capabilities)
        {
            Capabilities.Add(capability);
        }
    }

    private void InsertTimeline(RuntimeTimelineEntryDto entry)
    {
        if (SelectedTimelineCategory != "All" &&
            !entry.Category.Equals(SelectedTimelineCategory, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm) &&
            !entry.Message.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) &&
            !(entry.WorkflowId?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) &&
            !(entry.ReleaseId?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) &&
            !(entry.ContainerName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return;
        }

        TimelineEntries.Insert(0, entry);
        while (TimelineEntries.Count > 200)
        {
            TimelineEntries.RemoveAt(TimelineEntries.Count - 1);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        runtimeHubClient.RuntimeStateReceived -= OnRuntimeStateReceived;
        runtimeHubClient.RuntimeTimelineReceived -= OnRuntimeTimelineReceived;
        runtimeHubClient.RuntimeMetricsReceived -= OnRuntimeMetricsReceived;
        runtimeHubClient.ContainerEventReceived -= OnContainerEventReceived;
        runtimeHubClient.WorkflowUpdateReceived -= OnWorkflowUpdateReceived;
        runtimeHubClient.DeploymentUpdateReceived -= OnDeploymentUpdateReceived;
        runtimeHubClient.RuntimeHealthReceived -= OnRuntimeHealthReceived;
        await runtimeHubClient.DisconnectAsync();
        if (runtimeHubClient is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}
