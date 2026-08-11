using System.Collections.ObjectModel;
using Aovias.Runtime.SDK;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ComplianceServiceManager.ViewModels.Operations;

/// <summary>
/// View model for live operations powered by SignalR.
/// </summary>
public sealed partial class LiveOperationsViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IRuntimeHubClient runtimeHubClient;

    [ObservableProperty]
    private string connectionStatus = "Disconnected";

    [ObservableProperty]
    private string currentTask = "Waiting for events...";

    public ObservableCollection<string> RunningWorkflows { get; } = [];

    public ObservableCollection<string> EventTimeline { get; } = [];

    public ObservableCollection<string> ExecutionQueue { get; } = [];

    public LiveOperationsViewModel(IRuntimeHubClient runtimeHubClient)
    {
        this.runtimeHubClient = runtimeHubClient;
        this.runtimeHubClient.WorkflowProgressReceived += OnWorkflowProgressReceived;
        this.runtimeHubClient.WorkflowEventReceived += OnWorkflowEventReceived;
        this.runtimeHubClient.RuntimeHealthReceived += OnRuntimeHealthReceived;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            await runtimeHubClient.ConnectAsync();
            ConnectionStatus = "Connected";
            AppendTimeline("Connected to runtime SignalR hub.");
        }
        catch (Exception exception)
        {
            ConnectionStatus = "Connection failed";
            AppendTimeline(exception.Message);
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await runtimeHubClient.DisconnectAsync();
        ConnectionStatus = "Disconnected";
    }

    private void OnWorkflowProgressReceived(object? sender, WorkflowProgressMessage message)
    {
        CurrentTask = message.StepName is null ? message.Message : $"{message.StepName}: {message.Message}";
        AppendTimeline($"{message.Timestamp:HH:mm:ss} [{message.EventType}] {message.Message}");

        if (message.EventType is "WorkflowQueued" or "StepStarted")
        {
            if (!RunningWorkflows.Contains(message.WorkflowId))
            {
                RunningWorkflows.Add(message.WorkflowId);
            }
        }

        if (message.EventType is "WorkflowCompleted" or "WorkflowFailed")
        {
            RunningWorkflows.Remove(message.WorkflowId);
        }
    }

    private void OnWorkflowEventReceived(object? sender, WorkflowEventMessage message)
    {
        if (message.EventType == "WorkflowQueued")
        {
            ExecutionQueue.Add($"{message.Timestamp:HH:mm:ss} {message.WorkflowId}");
        }
    }

    private void OnRuntimeHealthReceived(object? sender, RuntimeHealthMessage message)
    {
        AppendTimeline($"{message.Timestamp:HH:mm:ss} Health: {message.Status} - {message.Message}");
    }

    private void AppendTimeline(string message)
    {
        EventTimeline.Insert(0, message);
        while (EventTimeline.Count > 200)
        {
            EventTimeline.RemoveAt(EventTimeline.Count - 1);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        runtimeHubClient.WorkflowProgressReceived -= OnWorkflowProgressReceived;
        runtimeHubClient.WorkflowEventReceived -= OnWorkflowEventReceived;
        runtimeHubClient.RuntimeHealthReceived -= OnRuntimeHealthReceived;
        await runtimeHubClient.DisconnectAsync();
        if (runtimeHubClient is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}
