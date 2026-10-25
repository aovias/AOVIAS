using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Aovias.DesignSystem.Enums;
using Aovias.Runtime.Contracts.Observability;
using Aovias.Runtime.SDK;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;
using ComplianceServiceManager.Contracts.Observability;
using ComplianceServiceManager.Contracts.Runtime;
using Aovias.Release.Contracts.Enums;
using ComplianceServiceManager.Models.Runtime;
using ComplianceServiceManager.Navigation;
using ComplianceServiceManager.ViewModels;

namespace ComplianceServiceManager.ViewModels.Overview;

/// <summary>
/// Landing workspace for the AOVIAS Command enterprise experience.
/// </summary>
public sealed partial class OverviewViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IRuntimeService runtimeService;
    private readonly IObservabilityService observabilityService;
    private readonly IDeploymentService deploymentService;
    private readonly IRuntimeHubClient runtimeHubClient;
    private readonly IEnvironmentManager environmentManager;
    private readonly MainViewModel complianceViewModel;
    private readonly IWorkspaceNavigator workspaceNavigator;

    [ObservableProperty]
    private string statusMessage = "Loading platform overview...";

    [ObservableProperty]
    private string platformHealthStatus = "Checking";

    [ObservableProperty]
    private StatusKind platformHealthKind = StatusKind.Information;

    [ObservableProperty]
    private string runtimeHealthStatus = "Unknown";

    [ObservableProperty]
    private StatusKind runtimeHealthKind = StatusKind.Warning;

    [ObservableProperty]
    private string runtimeName = "Not connected";

    [ObservableProperty]
    private int runningDeploymentsCount;

    [ObservableProperty]
    private int alertCount;

    [ObservableProperty]
    private int runningWorkflowCount;

    [ObservableProperty]
    private string infrastructureStatus = "Unknown";

    [ObservableProperty]
    private StatusKind infrastructureKind = StatusKind.Information;

    [ObservableProperty]
    private string complianceSummary = "No services discovered";

    [ObservableProperty]
    private StatusKind complianceKind = StatusKind.Compliance;

    [ObservableProperty]
    private double cpuUsagePercent;

    [ObservableProperty]
    private double memoryUsagePercent;

    [ObservableProperty]
    private int environmentsCount;

    [ObservableProperty]
    private int connectedAgentsCount;

    [ObservableProperty]
    private string agentVersion = "–";

    [ObservableProperty]
    private string agentHost = "–";

    [ObservableProperty]
    private string agentOs = "–";

    [ObservableProperty]
    private string agentDocker = "–";

    [ObservableProperty]
    private string agentLastSeen = "Never";

    [ObservableProperty]
    private string selectedActivityFilter = "All";

    public IReadOnlyList<string> ActivityFilters { get; } = ["All", "Deployments", "Reconciliation", "System"];

    public ObservableCollection<ActivityFeedItemViewModel> ActivityFeed { get; } = [];

    /// <summary>Local dev services manager surfaced on the dashboard summary card.</summary>
    public MainViewModel Services => complianceViewModel;

    public OverviewViewModel(
        IRuntimeService runtimeService,
        IObservabilityService observabilityService,
        IDeploymentService deploymentService,
        IRuntimeHubClient runtimeHubClient,
        IEnvironmentManager environmentManager,
        MainViewModel complianceViewModel,
        IWorkspaceNavigator workspaceNavigator)
    {
        this.runtimeService = runtimeService;
        this.observabilityService = observabilityService;
        this.deploymentService = deploymentService;
        this.runtimeHubClient = runtimeHubClient;
        this.environmentManager = environmentManager;
        this.complianceViewModel = complianceViewModel;
        this.workspaceNavigator = workspaceNavigator;

        runtimeHubClient.RuntimeTimelineReceived += OnRuntimeTimelineReceived;
        runtimeHubClient.RuntimeHealthReceived += OnRuntimeHealthReceived;
        runtimeHubClient.DeploymentUpdateReceived += OnDeploymentUpdateReceived;

        ActivityView = CollectionViewSource.GetDefaultView(ActivityFeed);
        ActivityView.Filter = FilterActivity;
        ActivityFeed.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasActivity));
    }

    public ICollectionView ActivityView { get; }

    partial void OnSelectedActivityFilterChanged(string value) => ActivityView.Refresh();

    private bool FilterActivity(object candidate)
    {
        if (candidate is not ActivityFeedItemViewModel item)
        {
            return false;
        }

        return SelectedActivityFilter switch
        {
            "Deployments" => item.Category.Contains("Deploy", StringComparison.OrdinalIgnoreCase)
                || item.Category.Contains("Workflow", StringComparison.OrdinalIgnoreCase),
            "Reconciliation" => item.Category.Contains("Reconcil", StringComparison.OrdinalIgnoreCase)
                || item.Category.Contains("Drift", StringComparison.OrdinalIgnoreCase),
            "System" => !item.Category.Contains("Deploy", StringComparison.OrdinalIgnoreCase)
                && !item.Category.Contains("Workflow", StringComparison.OrdinalIgnoreCase)
                && !item.Category.Contains("Reconcil", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            await runtimeHubClient.ConnectAsync();
            await RefreshAsync();
            StatusMessage = "Platform overview is live.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Overview unavailable: {exception.Message}";
            PlatformHealthKind = StatusKind.Critical;
            PlatformHealthStatus = "Degraded";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            var runtime = await runtimeService.GetRuntimeAsync();
            RuntimeName = runtime.Name;
            RuntimeHealthStatus = runtime.Status;
            RuntimeHealthKind = MapHealth(runtime.Health);

            var agentHealthy = runtime.Health is RuntimeHealth.Healthy or RuntimeHealth.Running or RuntimeHealth.Valid;
            ConnectedAgentsCount = agentHealthy ? 1 : 0;
            AgentVersion = string.IsNullOrWhiteSpace(runtime.Version) ? "–" : runtime.Version;
            AgentHost = string.IsNullOrWhiteSpace(runtime.Hostname) ? "–" : runtime.Hostname;
            AgentOs = string.IsNullOrWhiteSpace(runtime.OperatingSystem) ? "–" : runtime.OperatingSystem;
            AgentDocker = string.IsNullOrWhiteSpace(runtime.DockerNetwork) ? "–" : runtime.DockerNetwork;
            AgentLastSeen = agentHealthy ? runtime.LastUpdated.ToLocalTime().ToString("g") : "Never";

            try
            {
                EnvironmentsCount = (await environmentManager.GetEnvironmentsAsync()).Count;
            }
            catch
            {
                // Environment registry unavailable — leave the last known count.
            }

            var state = await observabilityService.GetStateAsync();
            CpuUsagePercent = state.CpuUsagePercent;
            MemoryUsagePercent = state.MemoryUsagePercent;
            RunningWorkflowCount = state.RunningWorkflowCount;
            RunningDeploymentsCount = state.DeploymentQueueCount + state.ActiveReleaseCount;
            InfrastructureStatus = $"{state.RunningContainerCount} containers · {state.NetworkCount} networks · {state.VolumeCount} volumes";
            var stateHealthy = string.Equals(state.HealthStatus, "Healthy", StringComparison.OrdinalIgnoreCase);
            InfrastructureKind = stateHealthy ? StatusKind.Healthy : StatusKind.Warning;

            var releases = await deploymentService.GetReleasesAsync();
            RunningDeploymentsCount = Math.Max(
                RunningDeploymentsCount,
                releases.Count(release => release.State is ReleaseState.Deploying or ReleaseState.Activated or ReleaseState.HealthCheck));

            AlertCount = complianceViewModel.ErrorServicesCount + complianceViewModel.FailedRepairCount;
            ComplianceSummary = $"{complianceViewModel.RunningServicesCount}/{complianceViewModel.ServiceCount} services running";
            ComplianceKind = AlertCount > 0 ? StatusKind.Warning : StatusKind.Healthy;

            var runtimeHealthy = runtime.Health is RuntimeHealth.Healthy or RuntimeHealth.Running or RuntimeHealth.Valid;
            PlatformHealthStatus = runtimeHealthy && stateHealthy ? "Healthy" : "Needs attention";
            PlatformHealthKind = runtimeHealthy && stateHealthy ? StatusKind.Healthy : StatusKind.Warning;

            var timeline = await observabilityService.GetTimelineAsync(limit: 12);
            ActivityFeed.Clear();
            foreach (var entry in timeline)
            {
                ActivityFeed.Add(new ActivityFeedItemViewModel
                {
                    Timestamp = entry.Timestamp.LocalDateTime.ToString("HH:mm"),
                    Title = entry.EventType,
                    Message = entry.Message,
                    Category = entry.Category,
                    StatusKind = MapCategory(entry.Category)
                });
            }
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    [RelayCommand]
    private void OpenReleaseCenter() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceReleaseCenter);

    [RelayCommand]
    private void OpenPublishing() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspacePublishing);

    [RelayCommand]
    private void OpenRuntime() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceRuntime);

    [RelayCommand]
    private void OpenObservability() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceObservability);

    [RelayCommand]
    private void OpenCompliance() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspacePraman);

    [RelayCommand]
    private void OpenSettings() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceSettings);

    [RelayCommand]
    private void OpenBootstrap() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceBootstrap);

    [RelayCommand]
    private void OpenEnvironments() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceEnvironments);

    [RelayCommand]
    private void OpenDeployments() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceDeployments);

    [RelayCommand]
    private void OpenReleaseLibrary() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceReleaseLibrary);

    [RelayCommand]
    private void OpenLocalDevServices() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceLocalDevServices);

    [RelayCommand]
    private void OpenRuntimeAgents() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceRuntimeAgents);

    [RelayCommand]
    private void SelectActivityFilter(string filter) => SelectedActivityFilter = filter;

    public bool HasActivity => ActivityFeed.Count > 0;

    private void OnRuntimeTimelineReceived(object? sender, RuntimeTimelineEntryDto message)
    {
        ActivityFeed.Insert(0, new ActivityFeedItemViewModel
        {
            Timestamp = message.Timestamp.LocalDateTime.ToString("HH:mm"),
            Title = message.EventType,
            Message = message.Message,
            Category = message.Category,
            StatusKind = MapCategory(message.Category)
        });

        while (ActivityFeed.Count > 20)
        {
            ActivityFeed.RemoveAt(ActivityFeed.Count - 1);
        }
    }

    private void OnRuntimeHealthReceived(object? sender, RuntimeHealthMessage message)
    {
        RuntimeHealthStatus = message.Status;
        var healthy = message.Status.Contains("healthy", StringComparison.OrdinalIgnoreCase);
        RuntimeHealthKind = healthy ? StatusKind.Healthy : StatusKind.Warning;
        PlatformHealthStatus = healthy ? "Healthy" : "Needs attention";
        PlatformHealthKind = RuntimeHealthKind;
    }

    private void OnDeploymentUpdateReceived(object? sender, RuntimeTimelineEntryDto message)
    {
        ActivityFeed.Insert(0, new ActivityFeedItemViewModel
        {
            Timestamp = message.Timestamp.LocalDateTime.ToString("HH:mm"),
            Title = message.EventType,
            Message = message.Message,
            Category = "Deployment",
            StatusKind = StatusKind.Deployment
        });
    }

    private static StatusKind MapHealth(RuntimeHealth health) => health switch
    {
        RuntimeHealth.Healthy or RuntimeHealth.Running or RuntimeHealth.Valid => StatusKind.Healthy,
        RuntimeHealth.Warning or RuntimeHealth.Degraded => StatusKind.Warning,
        _ => StatusKind.Critical
    };

    private static StatusKind MapCategory(string category) => category switch
    {
        "Health" => StatusKind.Healthy,
        "Deployment" => StatusKind.Deployment,
        "Workflow" => StatusKind.Deployment,
        "Docker" => StatusKind.Information,
        "Configuration" => StatusKind.Warning,
        "Compliance" => StatusKind.Compliance,
        _ => StatusKind.Information
    };

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        runtimeHubClient.RuntimeTimelineReceived -= OnRuntimeTimelineReceived;
        runtimeHubClient.RuntimeHealthReceived -= OnRuntimeHealthReceived;
        runtimeHubClient.DeploymentUpdateReceived -= OnDeploymentUpdateReceived;
        return ValueTask.CompletedTask;
    }
}
