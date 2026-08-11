using System.Collections.ObjectModel;
using System.Windows.Threading;
using Aovias.Configuration.Settings;
using Aovias.Runtime.SDK;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Navigation;
using ComplianceServiceManager.ViewModels.AiCopilot;
using ComplianceServiceManager.ViewModels.Applications;
using ComplianceServiceManager.ViewModels.Development;
using ComplianceServiceManager.ViewModels.Infrastructure;
using ComplianceServiceManager.ViewModels.Observability;
using ComplianceServiceManager.ViewModels.Operations;
using ComplianceServiceManager.ViewModels.Overview;
using ComplianceServiceManager.ViewModels.Platform;
using ComplianceServiceManager.ViewModels.Publishing;
using ComplianceServiceManager.ViewModels.Releases;
using ComplianceServiceManager.ViewModels.Runtime;
using ComplianceServiceManager.ViewModels.Settings;
using ComplianceServiceManager.ViewModels.Shell;
using Microsoft.Extensions.Options;

namespace ComplianceServiceManager.ViewModels;

/// <summary>
/// AOVIAS Studio shell: data-driven, sectioned workspace navigation.
/// One workspace = one responsibility; sections group related workspaces
/// (Platform, Applications, Operations, Development, Administration).
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    // Workspace ids (also legacy quick-action names — WorkspaceNavigator routes by these).
    public const string WorkspaceOverview = "Overview";
    public const string WorkspaceEnvironments = "Environments";
    public const string WorkspaceBootstrap = "Bootstrap";
    public const string WorkspaceRuntimeAgents = "Runtime Agents";
    public const string WorkspaceInfrastructure = "Infrastructure";
    public const string WorkspaceCertificates = "Certificates";
    public const string WorkspaceReleaseLibrary = "Release Library";
    public const string WorkspacePublishing = "Publishing";
    public const string WorkspaceDeployments = "Deployments";
    public const string WorkspaceRollbackHistory = "Rollback & History";
    public const string WorkspaceRuntime = "Runtime";
    public const string WorkspaceDeploymentMonitor = "Deployment Monitor";
    public const string WorkspaceMonitoring = "Monitoring";
    public const string WorkspaceObservability = "Observability";
    public const string WorkspaceReconciliation = "Reconciliation";
    public const string WorkspaceAlerts = "Alerts";
    public const string WorkspaceAudit = "Audit";
    public const string WorkspaceLocalDevServices = "Local Dev Services";
    public const string WorkspaceAiCopilot = "AI Copilot";
    public const string WorkspaceSettings = "Settings";

    // Legacy workspace names still used by existing quick actions; routed to their successors.
    public const string WorkspaceReleaseCenter = "Release Center";
    public const string WorkspaceDeployment = "Deployment";
    public const string WorkspacePlatformDeployment = "Platform Deployment";
    public const string WorkspacePraman = "PRAMAN";

    private static readonly Dictionary<string, string> WorkspaceSubtitles = new(StringComparer.OrdinalIgnoreCase)
    {
        [WorkspaceOverview] = "AOVIAS Enterprise Application Platform",
        [WorkspaceEnvironments] = "Registered deployment environments, health and readiness",
        [WorkspaceBootstrap] = "Provision a clean VPS into a registered environment",
        [WorkspaceRuntimeAgents] = "Runtime agent fleet and local development agent",
        [WorkspaceInfrastructure] = "Environment infrastructure state",
        [WorkspaceCertificates] = "Platform certificate conventions and local HTTPS certificate",
        [WorkspaceReleaseLibrary] = "Published releases and their lifecycle",
        [WorkspacePublishing] = "Discovery, analysis, validation, packaging and release creation",
        [WorkspaceDeployments] = "One guided workflow — runtime, release, services, validation, deploy, verify",
        [WorkspaceRollbackHistory] = "Durable deployment history and operator-confirmed rollback",
        [WorkspaceRuntime] = "Connected runtime state",
        [WorkspaceDeploymentMonitor] = "Observe live sessions, history, retry and rollback — start deployments from Applications → Deploy",
        [WorkspaceMonitoring] = "Live platform operations",
        [WorkspaceObservability] = "Logs, metrics, traces and events",
        [WorkspaceReconciliation] = "Desired vs observed state drift detection",
        [WorkspaceAlerts] = "Warnings and errors across the session",
        [WorkspaceAudit] = "Attributable platform and repair operations",
        [WorkspaceLocalDevServices] = "Run and monitor local development solutions",
        [WorkspaceAiCopilot] = "AI-assisted operations",
        [WorkspaceSettings] = "Platform configuration"
    };

    private readonly Dictionary<string, (object ViewModel, Action? OnNavigated)> workspaces = new();
    private readonly Dictionary<string, NavigationItemViewModel> itemsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer clockTimer;
    private readonly IRuntimeAgentClient runtimeAgentClient;
    private readonly IOptionsMonitor<AoviasPlatformOptions> platformOptions;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;
    private readonly LocalDevServicesViewModel localDevServicesViewModel;

    [ObservableProperty]
    private object? currentWorkspaceViewModel;

    [ObservableProperty]
    private string selectedWorkspace = WorkspaceOverview;

    [ObservableProperty]
    private string agentConnectionStatus = "Checking…";

    public ObservableCollection<NavigationSectionViewModel> NavigationSections { get; } = [];

    public string ConnectedAgentDisplay => apiSettings.CurrentValue.BaseUrl;

    public string ActiveProfileDisplay => platformOptions.CurrentValue.ActiveProfile;

    public string CurrentWorkspaceTitle => SelectedWorkspace;

    public string CurrentWorkspaceSubtitle =>
        WorkspaceSubtitles.TryGetValue(SelectedWorkspace, out var subtitle) ? subtitle : string.Empty;

    public bool IsAgentConnected => AgentConnectionStatus == "Connected";

    public string CurrentUserName => Environment.UserName;

    public string AppVersionDisplay => "1.0.0";

    [ObservableProperty]
    private string clockDisplay = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt");

    /// <summary>Long-lived services manager; disposed by the application on exit.</summary>
    public MainViewModel ComplianceViewModel => localDevServicesViewModel.Services;

    public ShellViewModel(
        OverviewViewModel overviewViewModel,
        EnvironmentsViewModel environmentsViewModel,
        BootstrapViewModel bootstrapViewModel,
        RuntimeAgentsViewModel runtimeAgentsViewModel,
        InfrastructureViewModel infrastructureViewModel,
        CertificatesViewModel certificatesViewModel,
        ReleasesViewModel releasesViewModel,
        PublishingViewModel publishingViewModel,
        DeploymentsWorkspaceViewModel deploymentsWorkspaceViewModel,
        Deployment.PlatformDeploymentCenterViewModel platformDeploymentCenterViewModel,
        RollbackHistoryViewModel rollbackHistoryViewModel,
        RuntimeDashboardViewModel runtimeDashboardViewModel,
        LiveOperationsViewModel liveOperationsViewModel,
        ObservabilityViewModel observabilityViewModel,
        ReconciliationViewModel reconciliationViewModel,
        AlertsViewModel alertsViewModel,
        AuditViewModel auditViewModel,
        LocalDevServicesViewModel localDevServicesViewModel,
        AiCopilotViewModel aiCopilotViewModel,
        SettingsConfigurationViewModel settingsConfigurationViewModel,
        IRuntimeAgentClient runtimeAgentClient,
        WorkspaceNavigator workspaceNavigator,
        IOptionsMonitor<AoviasPlatformOptions> platformOptions,
        IOptionsMonitor<ApiSettings> apiSettings)
    {
        this.runtimeAgentClient = runtimeAgentClient;
        this.platformOptions = platformOptions;
        this.apiSettings = apiSettings;
        this.localDevServicesViewModel = localDevServicesViewModel;

        Register(WorkspaceOverview, overviewViewModel);
        Register(WorkspaceEnvironments, environmentsViewModel, () => environmentsViewModel.InitializeCommand.Execute(null));
        Register(WorkspaceBootstrap, bootstrapViewModel);
        Register(WorkspaceRuntimeAgents, runtimeAgentsViewModel, () => runtimeAgentsViewModel.InitializeCommand.Execute(null));
        Register(WorkspaceInfrastructure, infrastructureViewModel);
        Register(WorkspaceCertificates, certificatesViewModel);
        Register(WorkspaceReleaseLibrary, releasesViewModel, () => releasesViewModel.InitializeCommand.Execute(null));
        Register(WorkspacePublishing, publishingViewModel);
        Register(WorkspaceDeployments, deploymentsWorkspaceViewModel);
        Register(WorkspaceRollbackHistory, rollbackHistoryViewModel, () => rollbackHistoryViewModel.InitializeCommand.Execute(null));
        Register(WorkspaceRuntime, runtimeDashboardViewModel);
        // Deployment Workspace refactor: the former "Deployment Command Center" is now the
        // Deployment Monitor — same singleton view model the unified Deploy workflow drives, so
        // this surface observes exactly what that workflow is doing. Observation only; no
        // deployment initiation exists here.
        Register(WorkspaceDeploymentMonitor, platformDeploymentCenterViewModel);
        Register(WorkspaceMonitoring, liveOperationsViewModel);
        Register(WorkspaceObservability, observabilityViewModel);
        Register(WorkspaceReconciliation, reconciliationViewModel, () => reconciliationViewModel.InitializeCommand.Execute(null));
        Register(WorkspaceAlerts, alertsViewModel);
        Register(WorkspaceAudit, auditViewModel, () => auditViewModel.InitializeCommand.Execute(null));
        Register(WorkspaceLocalDevServices, localDevServicesViewModel);
        Register(WorkspaceAiCopilot, aiCopilotViewModel);
        Register(WorkspaceSettings, settingsConfigurationViewModel);

        BuildNavigation();

        workspaceNavigator.NavigationRequested += NavigateByName;
        apiSettings.OnChange(settings => _ = RefreshAgentConnectionAsync());

        clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        clockTimer.Tick += (_, _) => ClockDisplay = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt");
        clockTimer.Start();

        NavigateByName(WorkspaceOverview);
        _ = RefreshAgentConnectionAsync();
    }

    [RelayCommand]
    private void OpenAlerts() => NavigateByName(WorkspaceAlerts);

    [RelayCommand]
    private void RefreshCurrentWorkspace()
    {
        if (workspaces.TryGetValue(SelectedWorkspace, out var entry))
        {
            entry.OnNavigated?.Invoke();
        }

        _ = RefreshAgentConnectionAsync();
    }

    [RelayCommand]
    private async Task ConnectAsync() => await RefreshAgentConnectionAsync();

    private void Register(string id, object viewModel, Action? onNavigated = null)
        => workspaces[id] = (viewModel, onNavigated);

    private void BuildNavigation()
    {
        NavigationSections.Add(new NavigationSectionViewModel(string.Empty,
        [
            CreateItem(WorkspaceOverview, "Overview", "IconOverview")
        ]));

        NavigationSections.Add(new NavigationSectionViewModel("PLATFORM",
        [
            CreateItem(WorkspaceEnvironments, "Environments", "IconEnvironments"),
            CreateItem(WorkspaceBootstrap, "Bootstrap", "IconBootstrap"),
            CreateItem(WorkspaceRuntimeAgents, "Runtime Agents", "IconAgents"),
            CreateItem(WorkspaceInfrastructure, "Infrastructure", "IconInfrastructure"),
            CreateItem(WorkspaceCertificates, "Certificates", "IconCertificates")
        ]));

        NavigationSections.Add(new NavigationSectionViewModel("APPLICATIONS",
        [
            CreateItem(WorkspaceReleaseLibrary, "Release Library", "IconLibrary"),
            CreateItem(WorkspacePublishing, "Publishing", "IconPublishing"),
            CreateItem(WorkspaceDeployments, "Deploy", "IconDeployments"),
            CreateItem(WorkspaceRollbackHistory, "Rollback & History", "IconHistory")
        ]));

        NavigationSections.Add(new NavigationSectionViewModel("OPERATIONS",
        [
            CreateItem(WorkspaceRuntime, "Runtime", "IconRuntime"),
            CreateItem(WorkspaceDeploymentMonitor, "Deployment Monitor", "IconDeployments"),
            CreateItem(WorkspaceMonitoring, "Monitoring", "IconMonitoring"),
            CreateItem(WorkspaceObservability, "Observability", "IconObservability"),
            CreateItem(WorkspaceReconciliation, "Reconciliation", "IconReconciliation"),
            CreateItem(WorkspaceAlerts, "Alerts & Events", "IconAlerts"),
            CreateItem(WorkspaceAudit, "Audit Logs", "IconAudit")
        ]));

        var localDevItem = CreateItem(WorkspaceLocalDevServices, "Local Dev Services", "IconDevServices");
        localDevItem.Badge = "NEW";
        NavigationSections.Add(new NavigationSectionViewModel("LOCAL DEVELOPMENT",
        [
            localDevItem,
            CreateItem(WorkspaceAiCopilot, "AI Copilot", "IconAiCopilot")
        ]));

        NavigationSections.Add(new NavigationSectionViewModel("ADMINISTRATION",
        [
            CreateItem(WorkspaceSettings, "Settings", "IconSettings")
        ]));
    }

    private NavigationItemViewModel CreateItem(string id, string title, string iconKey)
    {
        var item = new NavigationItemViewModel(id, title, iconKey, navItem => NavigateByName(navItem.Id));
        itemsById[id] = item;
        return item;
    }

    private void NavigateByName(string workspace)
    {
        var id = MapLegacyName(workspace);
        if (!workspaces.TryGetValue(id, out var entry))
        {
            return;
        }

        SelectedWorkspace = id;
        CurrentWorkspaceViewModel = entry.ViewModel;
        OnPropertyChanged(nameof(CurrentWorkspaceTitle));
        OnPropertyChanged(nameof(CurrentWorkspaceSubtitle));
        foreach (var item in itemsById.Values)
        {
            item.IsSelected = string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase);
        }

        entry.OnNavigated?.Invoke();
    }

    private static string MapLegacyName(string workspace) => workspace switch
    {
        WorkspaceReleaseCenter => WorkspaceDeployments,
        WorkspaceDeployment => WorkspaceDeployments,
        // The former "Platform Deployment"/"Deployment Command Center" destination is now the
        // observe-only Deployment Monitor (Deployment Workspace refactor).
        WorkspacePlatformDeployment => WorkspaceDeploymentMonitor,
        WorkspacePraman => WorkspaceLocalDevServices,
        _ => workspace
    };

    private async Task RefreshAgentConnectionAsync()
    {
        try
        {
            var health = await runtimeAgentClient.GetRuntimeHealthAsync();
            AgentConnectionStatus = string.Equals(health.Status, "Healthy", StringComparison.OrdinalIgnoreCase)
                ? "Connected"
                : health.Status;
            OnPropertyChanged(nameof(ConnectedAgentDisplay));
        }
        catch (Exception exception)
        {
            AgentConnectionStatus = $"Offline ({exception.GetType().Name})";
        }
        finally
        {
            OnPropertyChanged(nameof(IsAgentConnected));
        }
    }
}
