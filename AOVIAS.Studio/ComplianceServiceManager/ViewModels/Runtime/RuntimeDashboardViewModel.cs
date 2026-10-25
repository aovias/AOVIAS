using System.Collections.ObjectModel;
using Aovias.Runtime.SDK;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Runtime;
using ComplianceServiceManager.Models.Runtime;

namespace ComplianceServiceManager.ViewModels.Runtime;

/// <summary>
/// Presents operational data for the AOVIAS Runtime workspace dashboard.
/// </summary>
public sealed partial class RuntimeDashboardViewModel : ObservableObject
{
    private readonly IRuntimeService runtimeService;
    private readonly IRuntimeHubClient runtimeHubClient;
    private readonly CancellationTokenSource shutdownToken = new();

    #region Observable Properties

    [ObservableProperty]
    private RuntimeInfo runtime = new();

    [ObservableProperty]
    private RuntimeStatistics statistics = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string lastUpdatedDisplay = string.Empty;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the runtime services displayed in the dashboard grid.
    /// </summary>
    public ObservableCollection<RuntimeService> Services { get; } = [];

    /// <summary>
    /// Gets the runtime events displayed in the event log.
    /// </summary>
    public ObservableCollection<RuntimeEvent> Events { get; } = [];

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeDashboardViewModel"/> class.
    /// </summary>
    /// <param name="runtimeService">The runtime data service.</param>
    public RuntimeDashboardViewModel(IRuntimeService runtimeService, IRuntimeHubClient runtimeHubClient)
    {
        this.runtimeService = runtimeService;
        this.runtimeHubClient = runtimeHubClient;
        this.runtimeHubClient.RuntimeHealthReceived += OnRuntimeHealthReceived;
        _ = InitializeAsync();
    }

    /// <summary>
    /// Loads the initial dashboard data when the workspace is first opened.
    /// </summary>
    public async Task InitializeAsync()
    {
        await runtimeHubClient.ConnectAsync(shutdownToken.Token);
        await RefreshAsync(shutdownToken.Token);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Refreshes all runtime dashboard data from the configured service.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken.Token, cancellationToken);

        await RunBusyAsync(async token =>
        {
            IsRefreshing = true;
            StatusMessage = "Refreshing runtime data...";

            var runtimeTask = runtimeService.GetRuntimeAsync(token);
            var servicesTask = runtimeService.GetRuntimeServicesAsync(token);
            var eventsTask = runtimeService.GetRuntimeEventsAsync(token);
            var statisticsTask = runtimeService.GetRuntimeStatisticsAsync(token);

            await Task.WhenAll(runtimeTask, servicesTask, eventsTask, statisticsTask);

            Runtime = await runtimeTask;
            Statistics = await statisticsTask;
            LastUpdatedDisplay = Runtime.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");

            Services.Clear();
            foreach (var service in await servicesTask)
            {
                Services.Add(service);
            }

            Events.Clear();
            foreach (var runtimeEvent in await eventsTask)
            {
                Events.Add(runtimeEvent);
            }

            StatusMessage = "Runtime dashboard synchronized.";
        }, linkedSource.Token);

        IsRefreshing = false;
    }

    /// <summary>
    /// Executes a mock service action for the selected runtime service.
    /// </summary>
    /// <param name="service">The selected runtime service.</param>
    [RelayCommand]
    private void ViewServiceDetails(RuntimeService? service)
    {
        if (service is null)
        {
            return;
        }

        StatusMessage = $"Viewing details for {service.Name} (mock action).";
    }

    /// <summary>
    /// Executes a mock restart action for the selected runtime service.
    /// </summary>
    /// <param name="service">The selected runtime service.</param>
    [RelayCommand]
    private void RestartService(RuntimeService? service)
    {
        if (service is null)
        {
            return;
        }

        StatusMessage = $"Restart requested for {service.Name} (mock action).";
    }

    #endregion

    private void OnRuntimeHealthReceived(object? sender, RuntimeHealthMessage message)
    {
        StatusMessage = $"Live: {message.Status} - {message.Message}";
        LastUpdatedDisplay = message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    }

    #region Helpers

    private async Task RunBusyAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusMessage = "Refresh cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to refresh runtime dashboard: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion
}
