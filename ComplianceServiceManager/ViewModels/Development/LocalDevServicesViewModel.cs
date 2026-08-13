using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Models.AgentSessions;
using ComplianceServiceManager.Models.LocalPullRequests;
using ComplianceServiceManager.Models.LocalResources;
using ComplianceServiceManager.Models.ResourceIntelligence;
using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.PortRegistry;
using ComplianceServiceManager.Models.SharedRuntime;
using ComplianceServiceManager.Models.WorkspaceRuntime;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.AgentSessions;
using ComplianceServiceManager.Services.LocalDev;
using ComplianceServiceManager.Services.LocalPullRequests;
using ComplianceServiceManager.Services.LocalResources;
using ComplianceServiceManager.Services.ResourceIntelligence;
using ComplianceServiceManager.Services.RuntimeProfiles;
using ComplianceServiceManager.Services.ServiceInstances;
using ComplianceServiceManager.Services.PortRegistry;
using ComplianceServiceManager.Services.SharedRuntime;
using ComplianceServiceManager.Services.WorkspaceRuntime;
using ComplianceServiceManager.Services.Workspaces;
using ComplianceServiceManager.Views.Development;

namespace ComplianceServiceManager.ViewModels.Development;

/// <summary>
/// Development → Local Development hub. Hosts D1–D5 and S3 Local Dev capabilities.
/// </summary>
public sealed partial class LocalDevServicesViewModel : ObservableObject
{
    private readonly SolutionFolderRegistry folderRegistry;
    private readonly IDevelopmentWorkspaceService workspaceService;
    private readonly IRuntimeProfileOrchestrator profileOrchestrator;
    private readonly ServiceManagerService serviceManager;
    private readonly ILocalResourceMonitoringService resourceMonitoring;
    private readonly ILocalPullRequestService pullRequestService;
    private readonly IAgentSessionService agentSessionService;
    private readonly IResourceIntelligenceService resourceIntelligence;
    private readonly IServiceInstanceService serviceInstanceService;
    private readonly IPortRegistryService portRegistryService;
    private readonly ISharedRuntimeService sharedRuntimeService;
    private readonly IWorkspaceRuntimeService workspaceRuntimeService;
    private readonly IWorkspaceRuntimeTruth workspaceRuntimeTruth;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string workspaceStatusMessage = string.Empty;

    [ObservableProperty]
    private string profileStatusMessage = string.Empty;

    [ObservableProperty]
    private string pullRequestStatusMessage = string.Empty;

    [ObservableProperty]
    private string agentSessionStatusMessage = string.Empty;

    [ObservableProperty]
    private string resourceIntelligenceStatusMessage = string.Empty;

    [ObservableProperty]
    private string serviceInstanceStatusMessage = string.Empty;

    [ObservableProperty]
    private string portRegistryStatusMessage = string.Empty;

    [ObservableProperty]
    private string sharedRuntimeStatusMessage = string.Empty;

    [ObservableProperty]
    private string workspaceRuntimeStatusMessage = string.Empty;

    [ObservableProperty]
    private string? selectedFolder;

    [ObservableProperty]
    private bool isRefreshingWorkspaces;

    [ObservableProperty]
    private bool isProfileBusy;

    [ObservableProperty]
    private bool isPullRequestBusy;

    [ObservableProperty]
    private bool isAgentSessionBusy;

    [ObservableProperty]
    private RuntimeProfileItem? selectedProfile;

    [ObservableProperty]
    private LocalPullRequestItem? selectedPullRequest;

    [ObservableProperty]
    private AgentSessionItem? selectedAgentSession;

    [ObservableProperty]
    private ServiceInfoModel? selectedDiscoveredService;

    [ObservableProperty]
    private PortAllocationItem? selectedPortAllocation;

    [ObservableProperty]
    private ServiceInstanceItem? selectedServiceInstance;

    [ObservableProperty]
    private string reviewComment = string.Empty;

    [ObservableProperty]
    private bool canMergeSelectedPullRequest;

    /// <summary>Active Local Development tab (0=Overview … 4=Monitoring). Preserved across refreshes.</summary>
    [ObservableProperty]
    private int selectedLocalDevTabIndex;

    public const int OverviewTabIndex = 0;
    public const int GitAndPrTabIndex = 1;
    public const int AgentSessionsTabIndex = 2;
    public const int ServicesTabIndex = 3;
    public const int MonitoringTabIndex = 4;

    public static IReadOnlyList<string> LocalDevelopmentTabHeaders { get; } =
    [
        "Overview",
        "Git & PR",
        "Agent Sessions",
        "Services",
        "Monitoring"
    ];

    public int OverviewActiveAgentCount =>
        AgentSessions.Count(session =>
            session.Status is AgentSessionStatus.Active or AgentSessionStatus.Paused or AgentSessionStatus.Blocked or AgentSessionStatus.Stale);

    public string OverviewRuntimeProfileDisplay
    {
        get
        {
            var running = RuntimeProfiles.FirstOrDefault(profile =>
                profile.Status is RuntimeProfileStatus.Running or RuntimeProfileStatus.PartiallyRunning or RuntimeProfileStatus.Starting);
            if (running is not null)
            {
                return running.Name;
            }

            return SelectedProfile?.Name ?? "—";
        }
    }

    public string OverviewSharedRuntimeStatus => SharedRuntime.StatusDisplay;

    public string OverviewSharedRuntimeHealthy => SharedRuntime.HealthyDisplay;

    public string OverviewSharedRuntimeOwned => SharedRuntime.OwnedCount.ToString();

    public string OverviewSharedRuntimePorts => SharedRuntime.PortsDisplay;

    public string OverviewCursorRuntimeStatus => CursorRuntime.StatusDisplay;

    public string OverviewCursorRuntimeDetail =>
        $"{CursorRuntime.IsolatedCount} isolated · {CursorRuntime.ReuseSharedCount} shared";

    public string OverviewClaudeRuntimeStatus => ClaudeRuntime.StatusDisplay;

    public string OverviewClaudeRuntimeDetail =>
        $"{ClaudeRuntime.IsolatedCount} isolated · {ClaudeRuntime.ReuseSharedCount} shared";

    public int OverviewRunningServiceCount => Services.RunningServicesCount;

    public int OverviewStoppedServiceCount =>
        Math.Max(0, Services.ServiceCount - Services.RunningServicesCount);

    public string OverviewRunningNames
    {
        get
        {
            var names = EnumerateDiscoveredServices()
                .Where(service => service.Status == ServiceStatus.Running)
                .Select(service => service.Name)
                .Take(8)
                .ToList();
            return names.Count == 0 ? "None" : string.Join(", ", names);
        }
    }

    public string OverviewStoppedNames
    {
        get
        {
            var names = EnumerateDiscoveredServices()
                .Where(service => service.Status != ServiceStatus.Running)
                .Select(service => service.Name)
                .Take(8)
                .ToList();
            return names.Count == 0 ? "None" : string.Join(", ", names);
        }
    }

    public string OverviewActiveAgentsDisplay
    {
        get
        {
            var names = AgentSessions
                .Where(session => session.Status is AgentSessionStatus.Active or AgentSessionStatus.Paused)
                .Select(session => session.AgentDisplay)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return names.Count == 0 ? "None" : string.Join(", ", names);
        }
    }

    public string OverviewResourcePressure => ResourceIntelligence.OverallPressureDisplay;

    public MainViewModel Services { get; }

    public LocalResourceMonitorItem LocalResources { get; } = new();

    public ResourceIntelligenceItem ResourceIntelligence { get; } = new();

    public SharedRuntimeItem SharedRuntime { get; } = new();

    public WorkspaceRuntimeItem CursorRuntime { get; } = new()
    {
        WorkspaceId = "cursor",
        WorkspaceDisplay = "Cursor",
        AgentDisplay = "Cursor"
    };

    public WorkspaceRuntimeItem ClaudeRuntime { get; } = new()
    {
        WorkspaceId = "claude",
        WorkspaceDisplay = "Claude",
        AgentDisplay = "Claude"
    };

    public ObservableCollection<WorkspaceRuntimeItem> WorkspaceRuntimes { get; } = [];

    public ObservableCollection<string> SolutionFolders { get; } = [];

    public ObservableCollection<DevelopmentWorkspaceItem> Workspaces { get; } = [];

    public ObservableCollection<RuntimeProfileItem> RuntimeProfiles { get; } = [];

    public ObservableCollection<LocalServiceResourceRow> ServiceResourceRows { get; } = [];

    public ObservableCollection<LocalPullRequestItem> LocalPullRequests { get; } = [];

    public ObservableCollection<AgentSessionItem> AgentSessions { get; } = [];

    public ObservableCollection<ServiceInstanceItem> SelectedServiceInstances { get; } = [];

    public ObservableCollection<PortAllocationItem> PortAllocations { get; } = [];

    public LocalDevServicesViewModel(
        SolutionFolderRegistry folderRegistry,
        MainViewModel servicesViewModel,
        IDevelopmentWorkspaceService workspaceService,
        IRuntimeProfileOrchestrator profileOrchestrator,
        ServiceManagerService serviceManager,
        ILocalResourceMonitoringService resourceMonitoring,
        ILocalPullRequestService pullRequestService,
        IAgentSessionService agentSessionService,
        IResourceIntelligenceService resourceIntelligence,
        IServiceInstanceService serviceInstanceService,
        IPortRegistryService portRegistryService,
        ISharedRuntimeService sharedRuntimeService,
        IWorkspaceRuntimeService workspaceRuntimeService,
        IWorkspaceRuntimeTruth workspaceRuntimeTruth)
    {
        this.folderRegistry = folderRegistry;
        this.workspaceService = workspaceService;
        this.profileOrchestrator = profileOrchestrator;
        this.serviceManager = serviceManager;
        this.resourceMonitoring = resourceMonitoring;
        this.pullRequestService = pullRequestService;
        this.agentSessionService = agentSessionService;
        this.resourceIntelligence = resourceIntelligence;
        this.serviceInstanceService = serviceInstanceService;
        this.portRegistryService = portRegistryService;
        this.sharedRuntimeService = sharedRuntimeService;
        this.workspaceRuntimeService = workspaceRuntimeService;
        this.workspaceRuntimeTruth = workspaceRuntimeTruth;
        Services = servicesViewModel;
        WorkspaceRuntimes.Add(CursorRuntime);
        WorkspaceRuntimes.Add(ClaudeRuntime);
        SyncFolders();
        folderRegistry.FoldersChanged += SyncFolders;
        profileOrchestrator.ProfilesChanged += SyncProfiles;
        SyncProfiles();
        pullRequestService.PullRequestsChanged += SyncPullRequests;
        SyncPullRequests();
        agentSessionService.SessionsChanged += SyncAgentSessions;
        SyncAgentSessions();
        serviceInstanceService.InstancesChanged += SyncSelectedServiceInstances;
        portRegistryService.AllocationsChanged += SyncPortAllocations;
        sharedRuntimeService.StateChanged += SyncSharedRuntime;
        workspaceRuntimeService.StateChanged += SyncWorkspaceRuntimes;
        serviceManager.Services.CollectionChanged += OnDiscoveredServicesChanged;
        resourceMonitoring.SnapshotUpdated += OnResourceSnapshotUpdated;
        resourceIntelligence.AssessmentUpdated += OnResourceAssessmentUpdated;
        resourceMonitoring.Start();
        if (resourceMonitoring.LatestSnapshot is not null)
        {
            ApplyResourceSnapshot(resourceMonitoring.LatestSnapshot);
        }

        if (resourceIntelligence.LatestAssessment is not null)
        {
            ApplyResourceAssessment(resourceIntelligence.LatestAssessment);
        }

        _ = RefreshWorkspacesAsync();
        _ = RefreshLocalPullRequestsAsync();
        _ = RefreshAgentSessionsAsync();
        _ = RefreshServiceInstancesAsync();
        _ = RefreshPortRegistryAsync();
        ReconcileProfilesWithDiscovery();
    }

    partial void OnSelectedDiscoveredServiceChanged(ServiceInfoModel? value)
    {
        SyncSelectedServiceInstances();
    }

    partial void OnSelectedPullRequestChanged(LocalPullRequestItem? value)
    {
        UpdateMergeAvailability();
        ReviewComment = value?.LastReview?.Comment ?? string.Empty;
    }

    partial void OnSelectedProfileChanged(RuntimeProfileItem? value)
    {
        NotifyOverviewSummariesChanged();
        if (value is not null)
        {
            ApplySharedRuntimePlan(sharedRuntimeService.Plan(value));
            RebuildIsolationChoices();
        }
        else
        {
            SyncSharedRuntime();
        }

        SyncWorkspaceRuntimes();
    }

    partial void OnIsProfileBusyChanged(bool value)
    {
        NotifySharedRuntimeCommandsChanged();
    }

    private IEnumerable<ServiceInfoModel> EnumerateDiscoveredServices()
        => Services.ServicesView.SourceCollection?.Cast<ServiceInfoModel>() ?? [];

    private void NotifyOverviewSummariesChanged()
    {
        OnPropertyChanged(nameof(OverviewActiveAgentCount));
        OnPropertyChanged(nameof(OverviewRuntimeProfileDisplay));
        OnPropertyChanged(nameof(OverviewSharedRuntimeStatus));
        OnPropertyChanged(nameof(OverviewSharedRuntimeHealthy));
        OnPropertyChanged(nameof(OverviewSharedRuntimeOwned));
        OnPropertyChanged(nameof(OverviewSharedRuntimePorts));
        OnPropertyChanged(nameof(OverviewCursorRuntimeStatus));
        OnPropertyChanged(nameof(OverviewCursorRuntimeDetail));
        OnPropertyChanged(nameof(OverviewClaudeRuntimeStatus));
        OnPropertyChanged(nameof(OverviewClaudeRuntimeDetail));
        OnPropertyChanged(nameof(OverviewRunningServiceCount));
        OnPropertyChanged(nameof(OverviewStoppedServiceCount));
        OnPropertyChanged(nameof(OverviewRunningNames));
        OnPropertyChanged(nameof(OverviewStoppedNames));
        OnPropertyChanged(nameof(OverviewActiveAgentsDisplay));
        OnPropertyChanged(nameof(OverviewResourcePressure));
    }

    [RelayCommand]
    private void RefreshLocalResources()
    {
        var snapshot = resourceMonitoring.CaptureSnapshot();
        ApplyResourceSnapshot(snapshot);
        ApplyResourceAssessment(resourceIntelligence.AssessFromSnapshot(snapshot, SelectedProfile?.Id));
    }

    [RelayCommand]
    private void RefreshResourceIntelligence()
    {
        var assessment = resourceIntelligence.Assess(SelectedProfile?.Id);
        ApplyResourceAssessment(assessment);
        ResourceIntelligenceStatusMessage = $"Assessed {assessment.Timestamp.ToLocalTime():HH:mm:ss} · {assessment.OverallPressure}";
    }

    [RelayCommand]
    private async Task UseSuggestedProfileAsync()
    {
        var profileId = ResourceIntelligence.SuggestedProfileId;
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return;
        }

        var profile = RuntimeProfiles.FirstOrDefault(item =>
                          string.Equals(item.Id, profileId, StringComparison.OrdinalIgnoreCase))
                      ?? profileOrchestrator.GetProfiles().FirstOrDefault(item =>
                          string.Equals(item.Id, profileId, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
        {
            MessageBox.Show("Suggested profile is not available.", "Resource Intelligence", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var pressure = ResourceIntelligence.OverallPressure;
        if (pressure is ResourcePressureLevel.High or ResourcePressureLevel.Critical)
        {
            var warn = MessageBox.Show(
                $"Memory/resource pressure is currently {pressure}.\n\nStart '{profile.Name}' anyway?\n\nThis explicit confirmation is required — Studio will not auto-switch profiles.",
                "Start Anyway?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (warn != MessageBoxResult.Yes)
            {
                return;
            }
        }
        else
        {
            var confirm = MessageBox.Show(
                $"Start runtime profile '{profile.Name}'?\n\nThis starts Shared Runtime after you confirm. Resource Intelligence itself does not mutate runtime.",
                "Use Suggested Profile",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.OK)
            {
                return;
            }
        }

        SelectedProfile = RuntimeProfiles.FirstOrDefault(item =>
                              string.Equals(item.Id, profile.Id, StringComparison.OrdinalIgnoreCase))
                          ?? profile;
        await StartSelectedProfileAsync();
        ResourceIntelligenceStatusMessage = $"Explicitly started suggested profile '{profile.Name}'.";
    }

    [RelayCommand]
    private void AddSolutionFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select a solution folder containing local development services"
        };

        if (dialog.ShowDialog() == true)
        {
            folderRegistry.Add(dialog.FolderName);
            StatusMessage = $"Added {dialog.FolderName}. Refresh discovery to pick up its services.";
        }
    }

    [RelayCommand]
    private void RemoveSelectedFolder()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolder))
        {
            return;
        }

        folderRegistry.Remove(SelectedFolder);
        StatusMessage = "Folder removed. Refresh discovery to update the service list.";
    }

    [RelayCommand]
    private async Task RefreshDiscoveryAsync()
    {
        StatusMessage = "Rediscovering services…";
        await Services.RefreshServicesCommand.ExecuteAsync(null);
        ReconcileProfilesWithDiscovery();
        await RefreshServiceInstancesAsync();
        StatusMessage = $"{Services.ServiceCount} service(s) discovered across {SolutionFolders.Count} folder(s).";
    }

    [RelayCommand]
    private async Task RefreshWorkspacesAsync()
    {
        if (IsRefreshingWorkspaces)
        {
            return;
        }

        IsRefreshingWorkspaces = true;
        WorkspaceStatusMessage = "Refreshing workspaces…";
        try
        {
            var snapshots = await workspaceService.RefreshAsync();
            void Apply()
            {
                Workspaces.Clear();
                foreach (var workspace in snapshots)
                {
                    Workspaces.Add(workspace);
                }

                var available = snapshots.Count(item => item.Availability != WorkspaceAvailability.Unavailable);
                WorkspaceStatusMessage = $"{available}/{snapshots.Count} workspace(s) accessible.";
                SyncWorkspaceRuntimes();
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is not null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(Apply);
            }
            else
            {
                Apply();
            }
        }
        catch (Exception ex)
        {
            WorkspaceStatusMessage = $"Workspace refresh failed: {ex.Message}";
        }
        finally
        {
            IsRefreshingWorkspaces = false;
        }
    }

    [RelayCommand]
    private void ValidateSelectedProfile()
    {
        if (SelectedProfile is null)
        {
            ProfileStatusMessage = "Select a profile to validate.";
            return;
        }

        var result = profileOrchestrator.Validate(SelectedProfile);
        ApplySharedRuntimePlan(sharedRuntimeService.Plan(SelectedProfile));
        ProfileStatusMessage = result.IsValid
            ? $"{SelectedProfile.Name}: valid ({result.StartupOrder.Count} services in order)."
            : $"{SelectedProfile.Name}: {result.Issues.Count} issue(s).";
        NotifySharedRuntimeCommandsChanged();
    }

    [RelayCommand]
    private void ValidateAllProfiles()
    {
        profileOrchestrator.ValidateAll();
        SyncProfiles();
        if (SelectedProfile is not null)
        {
            ApplySharedRuntimePlan(sharedRuntimeService.Plan(SelectedProfile));
        }

        ProfileStatusMessage = "All profiles revalidated against current discovery.";
        NotifySharedRuntimeCommandsChanged();
    }

    [RelayCommand(CanExecute = nameof(CanStartSharedRuntime))]
    private async Task StartSelectedProfileAsync()
    {
        await StartSharedRuntimeAsync();
    }

    [RelayCommand(CanExecute = nameof(CanStopSharedRuntime))]
    private async Task StopSelectedProfileAsync()
    {
        await StopSharedRuntimeAsync();
    }

    [RelayCommand(CanExecute = nameof(CanRestartSharedRuntime))]
    private async Task RestartSelectedProfileAsync()
    {
        if (SelectedProfile is null || IsProfileBusy || sharedRuntimeService.IsStarting)
        {
            if (sharedRuntimeService.IsStarting)
            {
                MessageBox.Show(
                    "Shared Runtime startup already in progress.",
                    "Shared Runtime",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            return;
        }

        var confirm = MessageBox.Show(
            $"Restart Shared Runtime '{SelectedProfile.Name}'?\n\nStop will affect only owned services, then Plan + Start run again.",
            "Restart Shared Runtime",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.OK)
        {
            return;
        }

        IsProfileBusy = true;
        try
        {
            await sharedRuntimeService.StopAsync(SelectedProfile);
            var plan = sharedRuntimeService.Plan(SelectedProfile);
            ApplySharedRuntimePlan(plan);
            if (!plan.CanStart)
            {
                SharedRuntimeStatusMessage = plan.Summary;
                return;
            }

            var result = await sharedRuntimeService.StartAsync(SelectedProfile);
            ApplySharedRuntimePlan(result.Plan ?? plan);
            SharedRuntimeStatusMessage = result.Error ?? SelectedProfile.ProgressMessage ?? result.Status.ToString();
            ProfileStatusMessage = SharedRuntimeStatusMessage;
            SyncSharedRuntime();
        }
        finally
        {
            IsProfileBusy = false;
        }
    }

    [RelayCommand]
    private void PlanSharedRuntime()
    {
        if (SelectedProfile is null)
        {
            MessageBox.Show("Select a Runtime Profile first.", "Shared Runtime", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var plan = sharedRuntimeService.Plan(SelectedProfile);
        ApplySharedRuntimePlan(plan);
        SharedRuntimeStatusMessage = plan.Summary;
        ProfileStatusMessage = plan.Summary;
    }

    [RelayCommand(CanExecute = nameof(CanStartSharedRuntime))]
    private async Task StartSharedRuntimeAsync()
    {
        if (SelectedProfile is null)
        {
            MessageBox.Show("Select a Runtime Profile first.", "Shared Runtime", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (IsProfileBusy || sharedRuntimeService.IsStarting)
        {
            MessageBox.Show(
                "Shared Runtime startup already in progress.",
                "Shared Runtime",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (Services.ServiceCount == 0)
        {
            await Services.RefreshServicesCommand.ExecuteAsync(null);
        }

        await serviceInstanceService.RefreshAsync();
        var plan = sharedRuntimeService.Plan(SelectedProfile);
        ApplySharedRuntimePlan(plan);
        if (!plan.CanStart)
        {
            MessageBox.Show(
                plan.Summary + Environment.NewLine + string.Join(Environment.NewLine, plan.Issues.Select(i => "• " + i.Message)),
                "Shared Runtime blocked",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            SharedRuntimeStatusMessage = plan.Summary;
            return;
        }

        var pressure = ResourceIntelligence.OverallPressure;
        var resourceNote = pressure is ResourcePressureLevel.High or ResourcePressureLevel.Critical or ResourcePressureLevel.Warning
            ? $"\n\nResource Intelligence (advisory): {OperatorLabels.Pressure(pressure)}. Studio will not switch profiles automatically."
            : "\n\nResource Intelligence (advisory): OK.";

        var confirm = MessageBox.Show(
            $"Start Shared Runtime\n\nProfile: {plan.ProfileName}\nScope: Shared\nServices: {plan.ServiceCount}\nAlready running (reuse): {plan.ReuseCount}\nTo start: {plan.ToStartCount}\nBlocked: {plan.BlockedCount}\n\nCanonical source:\n{plan.CanonicalSource}{resourceNote}\n\nNo processes start until you confirm.",
            "Start Shared Runtime",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.OK)
        {
            SharedRuntimeStatusMessage = "Start cancelled — no mutation occurred.";
            return;
        }

        IsProfileBusy = true;
        try
        {
            var result = await sharedRuntimeService.StartAsync(SelectedProfile);
            ApplySharedRuntimePlan(result.Plan ?? plan);
            SharedRuntimeStatusMessage = result.Error ?? SelectedProfile.ProgressMessage ?? result.Status.ToString();
            ProfileStatusMessage = SharedRuntimeStatusMessage;
            SyncSharedRuntime();
        }
        finally
        {
            IsProfileBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStopSharedRuntime))]
    private async Task StopSharedRuntimeAsync()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        if (IsProfileBusy)
        {
            return;
        }

        var owned = SelectedProfile.OwnedServiceNames.Count;
        var reused = SelectedProfile.Members.Count(m => m.Reused && !m.OwnedByProfile);
        var confirm = MessageBox.Show(
            $"Stop Shared Runtime '{SelectedProfile.Name}'?\n\nOwned services that will stop: {owned}\nReused/external services that will remain: {reused}\n\nUnowned and external processes will not be stopped.",
            "Stop Shared Runtime",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.OK)
        {
            return;
        }

        IsProfileBusy = true;
        try
        {
            var result = await sharedRuntimeService.StopAsync(SelectedProfile);
            SharedRuntimeStatusMessage = result.Error ?? SelectedProfile.ProgressMessage ?? "Stopped owned services.";
            ProfileStatusMessage = SharedRuntimeStatusMessage;
            SyncSharedRuntime();
        }
        finally
        {
            IsProfileBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelSharedRuntimeStartup))]
    private void CancelSharedRuntimeStartup()
    {
        sharedRuntimeService.CancelStartup();
        SharedRuntimeStatusMessage = "Cancel requested — already-started owned services remain running.";
    }

    [RelayCommand]
    private void PlanWorkspaceRuntime(WorkspaceRuntimeItem? item)
    {
        if (item is null || SelectedProfile is null)
        {
            MessageBox.Show("Select a Runtime Profile first.", "Workspace Runtime", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var snapshot = FindWorkspaceSnapshot(item.WorkspaceId);
        var plan = workspaceRuntimeService.Plan(
            SelectedProfile,
            item.WorkspaceId,
            snapshot,
            GetRequestedServices(item),
            SelectedAgentSession?.WorkspaceId == item.WorkspaceId ? SelectedAgentSession.Id : null);
        ApplyWorkspaceRuntimePlan(item, plan);
        WorkspaceRuntimeStatusMessage = $"{item.WorkspaceDisplay}: {plan.Summary}";
    }

    [RelayCommand]
    private async Task StartWorkspaceRuntimeAsync(WorkspaceRuntimeItem? item)
    {
        if (item is null || SelectedProfile is null)
        {
            MessageBox.Show("Select a Runtime Profile first.", "Workspace Runtime", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (workspaceRuntimeService.IsStarting(item.WorkspaceId))
        {
            MessageBox.Show(
                $"{item.WorkspaceDisplay} startup already in progress.",
                "Workspace Runtime",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        await serviceInstanceService.RefreshAsync();
        var snapshot = FindWorkspaceSnapshot(item.WorkspaceId);
        var plan = workspaceRuntimeService.Plan(
            SelectedProfile,
            item.WorkspaceId,
            snapshot,
            GetRequestedServices(item),
            SelectedAgentSession?.WorkspaceId == item.WorkspaceId ? SelectedAgentSession.Id : null);
        ApplyWorkspaceRuntimePlan(item, plan);
        if (!plan.CanStart)
        {
            MessageBox.Show(
                plan.Summary + Environment.NewLine + string.Join(Environment.NewLine, plan.Issues.Select(issue => "• " + issue.Message)),
                $"{item.WorkspaceDisplay} blocked",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            WorkspaceRuntimeStatusMessage = plan.Summary;
            return;
        }

        var pressure = ResourceIntelligence.OverallPressure;
        var resourceWarning = pressure is ResourcePressureLevel.High or ResourcePressureLevel.Critical or ResourcePressureLevel.Warning
                ? $"\n\nResource warning\n{ResourceIntelligence.OverallPressureDisplay}: {ResourceIntelligence.MemoryDisplay}\n{ResourceIntelligence.PrimaryRecommendationTitle}\nThis is advisory. Confirm to proceed. Studio will not switch profiles, reassign ports, or stop Shared Runtime."
            : "\n\nResource Intelligence (advisory): OK.";

        var startLines = plan.Entries
            .Where(entry => entry.Action == WorkspaceRuntimeAction.StartWorkspace)
            .Select(entry => $"• {entry.ServiceName} → START WORKSPACE :{entry.Port?.ToString() ?? "—"}");
        var reuseLines = plan.Entries
            .Where(entry => entry.Action == WorkspaceRuntimeAction.ReuseShared)
            .Select(entry => $"• {entry.ServiceName} → REUSE SHARED :{entry.Port?.ToString() ?? "—"}");

        var confirm = MessageBox.Show(
            $"{item.WorkspaceDisplay} Workspace Runtime\n\nWorkspace: {plan.WorkspaceDisplay}\nBranch: {plan.Branch ?? "—"}\nProfile: {plan.ProfileName}\nSource:\n{plan.SourceRoot}\n\n{string.Join(Environment.NewLine, startLines.Concat(reuseLines))}\n\n{plan.Summary}{resourceWarning}\n\nNo processes start until you confirm. Shared Runtime will not be mutated.",
            $"Start {item.WorkspaceDisplay} Runtime",
            MessageBoxButton.OKCancel,
            pressure is ResourcePressureLevel.High or ResourcePressureLevel.Critical
                ? MessageBoxImage.Warning
                : MessageBoxImage.Question);
        if (confirm != MessageBoxResult.OK)
        {
            WorkspaceRuntimeStatusMessage = "Start cancelled — no mutation occurred.";
            return;
        }

        try
        {
            var result = await workspaceRuntimeService.StartAsync(
                SelectedProfile,
                item.WorkspaceId,
                snapshot,
                GetRequestedServices(item),
                SelectedAgentSession?.WorkspaceId == item.WorkspaceId ? SelectedAgentSession.Id : null);
            ApplyWorkspaceRuntimePlan(item, result.Plan ?? plan);
            WorkspaceRuntimeStatusMessage = result.Error ?? result.Status.ToString();
            await serviceInstanceService.RefreshAsync();
            SyncWorkspaceRuntimes();
        }
        catch (Exception ex)
        {
            WorkspaceRuntimeStatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task StopWorkspaceRuntimeAsync(WorkspaceRuntimeItem? item)
    {
        if (item is null)
        {
            return;
        }

        var owned = workspaceRuntimeService.GetSession(item.WorkspaceId).OwnedServiceNames.ToList();
        var reused = item.PlanRows
            .Where(row => row.ActionDisplay.Contains("REUSE SHARED", StringComparison.OrdinalIgnoreCase))
            .Select(row => row.ServiceName)
            .ToList();
        var confirm = MessageBox.Show(
            $"Stop {item.WorkspaceDisplay} Runtime?\n\nOwned (will stop):\n{(owned.Count == 0 ? "• (none)" : string.Join(Environment.NewLine, owned.Select(name => "• " + name)))}\n\nReused shared (will remain):\n{(reused.Count == 0 ? "• (none)" : string.Join(Environment.NewLine, reused.Select(name => "• " + name)))}\n\nShared Runtime and the other workspace will not be stopped.",
            $"Stop {item.WorkspaceDisplay} Runtime",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.OK)
        {
            return;
        }

        var result = await workspaceRuntimeService.StopAsync(item.WorkspaceId);
        WorkspaceRuntimeStatusMessage = result.Error ?? $"Stopped owned {item.WorkspaceDisplay} services.";
        await serviceInstanceService.RefreshAsync();
        SyncWorkspaceRuntimes();
    }

    [RelayCommand]
    private void CancelWorkspaceRuntimeStartup(WorkspaceRuntimeItem? item)
    {
        if (item is null)
        {
            return;
        }

        workspaceRuntimeService.CancelStartup(item.WorkspaceId);
        WorkspaceRuntimeStatusMessage = $"Cancel requested for {item.WorkspaceDisplay} — already-started owned services remain running.";
    }

    private bool CanStartSharedRuntime()
        => SharedRuntimeCommandGate.CanStart(
            SelectedProfile is not null,
            SelectedProfile?.IsValid == true,
            sharedRuntimeService.LastPlan?.CanStart == true,
            sharedRuntimeService.Session.Status,
            sharedRuntimeService.IsStarting,
            IsProfileBusy);

    private bool CanStopSharedRuntime()
        => SharedRuntimeCommandGate.CanStop(
            SelectedProfile?.OwnedServiceNames.Count ?? 0,
            IsProfileBusy,
            sharedRuntimeService.IsStarting);

    private bool CanCancelSharedRuntimeStartup()
        => SharedRuntimeCommandGate.CanCancel(sharedRuntimeService.IsStarting);

    private bool CanRestartSharedRuntime()
        => SharedRuntimeCommandGate.CanRestart(
            SelectedProfile is not null,
            SelectedProfile?.IsValid == true,
            sharedRuntimeService.LastPlan?.CanStart == true,
            sharedRuntimeService.Session.Status,
            SelectedProfile?.OwnedServiceNames.Count ?? 0,
            sharedRuntimeService.IsStarting,
            IsProfileBusy);

    private void OnDiscoveredServicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(ReconcileProfilesWithDiscovery);
        }
        else
        {
            ReconcileProfilesWithDiscovery();
        }
    }

    /// <summary>
    /// Revalidates profiles against the current discovered collection (no extra discovery pass).
    /// </summary>
    private void ReconcileProfilesWithDiscovery()
    {
        profileOrchestrator.ValidateAll();
        SyncProfiles();
        if (SelectedProfile is not null)
        {
            ApplySharedRuntimePlan(sharedRuntimeService.Plan(SelectedProfile));
        }
        else
        {
            SyncSharedRuntime();
        }

        SyncWorkspaceRuntimes();
    }

    private void NotifySharedRuntimeCommandsChanged()
    {
        StartSharedRuntimeCommand.NotifyCanExecuteChanged();
        StopSharedRuntimeCommand.NotifyCanExecuteChanged();
        CancelSharedRuntimeStartupCommand.NotifyCanExecuteChanged();
        RestartSelectedProfileCommand.NotifyCanExecuteChanged();
        StartSelectedProfileCommand.NotifyCanExecuteChanged();
        StopSelectedProfileCommand.NotifyCanExecuteChanged();
    }

    private void SyncSharedRuntime()
    {
        void Apply()
        {
            var profile = SelectedProfile;
            SharedRuntime.ProfileName = profile?.Name ?? "—";
            SharedRuntime.ProfileId = profile?.Id ?? string.Empty;
            SharedRuntime.IsStarting = sharedRuntimeService.IsStarting;
            SharedRuntime.OwnedCount = profile?.OwnedServiceNames.Count ?? 0;
            SharedRuntime.StartedAtDisplay = sharedRuntimeService.Session.StartedAt?.ToLocalTime().ToString("HH:mm") ?? "—";
            SharedRuntime.ResourceDisplay = ResourceIntelligence.OverallPressureDisplay;
            SharedRuntime.CanStart = sharedRuntimeService.LastPlan?.CanStart ?? false;

            if (profile is not null)
            {
                var members = profile.Members;
                var ready = members.Count(m => m.Status is RuntimeProfileMemberStatus.Ready or RuntimeProfileMemberStatus.Reused);
                SharedRuntime.HealthyDisplay = members.Count == 0 ? "—" : $"{ready} / {members.Count}";
            }
            else
            {
                SharedRuntime.HealthyDisplay = "—";
            }

            var plan = sharedRuntimeService.LastPlan;
            SharedRuntime.IssueCount = plan?.IssueCount ?? 0;
            if (plan is not null)
            {
                SharedRuntime.ServiceCount = plan.ServiceCount;
                SharedRuntime.ToStartCount = plan.ToStartCount;
                SharedRuntime.ReuseCount = plan.ReuseCount;
                SharedRuntime.BlockedCount = plan.BlockedCount;
                var portOk = plan.Entries.Count(e => e.Action != SharedRuntimeAction.Block);
                SharedRuntime.PortsDisplay = plan.ServiceCount == 0
                    ? "—"
                    : $"{portOk}/{plan.ServiceCount} valid";
                SharedRuntimeStatusMessage = plan.CanStart
                    ? plan.Summary
                    : $"BLOCKED · {plan.IssueCount} validation issue(s)";
            }
            else if (profile is not null)
            {
                SharedRuntime.ServiceCount = profile.ServiceCount;
            }

            var mapped = sharedRuntimeService.Session.Status;
            SharedRuntime.ApplyStatus(mapped, sharedRuntimeService.LastPlan?.Summary ?? profile?.ProgressMessage);
            SharedRuntime.CanExecuteStart = CanStartSharedRuntime();
            SharedRuntime.CanExecuteStop = CanStopSharedRuntime();
            SharedRuntime.CanExecuteCancel = CanCancelSharedRuntimeStartup();
            SharedRuntime.CanExecuteRestart = CanRestartSharedRuntime();
            NotifySharedRuntimeCommandsChanged();
            NotifyOverviewSummariesChanged();
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Apply);
        }
        else
        {
            Apply();
        }
    }

    private void ApplySharedRuntimePlan(SharedRuntimePlan plan)
    {
        SharedRuntime.PlanRows.Clear();
        SharedRuntime.ValidationLines.Clear();
        var order = 1;
        foreach (var entry in plan.Entries)
        {
            SharedRuntime.PlanRows.Add(new SharedRuntimePlanRow
            {
                Order = order++,
                ServiceName = entry.ServiceName,
                InstanceDisplay = entry.ScopeDisplay,
                PortDisplay = entry.Port?.ToString() ?? "—",
                ActionDisplay = entry.ActionDisplay,
                OwnerDisplay = entry.OwnerDisplay,
                Reason = entry.Reason ?? string.Empty
            });
        }

        if (plan.CanStart)
        {
            SharedRuntime.ValidationLines.Add("✓ Profile valid");
            SharedRuntime.ValidationLines.Add("✓ Dependencies valid");
            SharedRuntime.ValidationLines.Add("✓ Ports valid");
            SharedRuntime.ValidationLines.Add("✓ Source valid");
            SharedRuntime.ValidationLines.Add("✓ Health configuration valid");
            SharedRuntime.ValidationLines.Add("✓ Shared instances only");
        }
        else
        {
            SharedRuntime.ValidationLines.Add($"BLOCKED · {plan.IssueCount} validation issue(s). See Runtime Profile for details.");
        }

        SyncSharedRuntime();
    }

    private void SyncWorkspaceRuntimes()
    {
        void Apply()
        {
            foreach (var item in WorkspaceRuntimes)
            {
                var snapshot = FindWorkspaceSnapshot(item.WorkspaceId);
                item.Path = snapshot?.Path ?? item.Path;
                item.Branch = snapshot?.Branch;
                item.WorkspaceDisplay = snapshot?.DisplayName ?? item.WorkspaceDisplay;
                item.AgentDisplay = snapshot?.AgentOwnerDisplay ?? item.AgentDisplay;
                item.ProfileName = SelectedProfile?.Name ?? "—";
                item.IsStarting = workspaceRuntimeService.IsStarting(item.WorkspaceId);
                var session = workspaceRuntimeService.GetSession(item.WorkspaceId);
                item.OwnedCount = session.OwnedServiceNames.Count;
                var plan = workspaceRuntimeService.GetLastPlan(item.WorkspaceId);
                item.CanStart = plan?.CanStart ?? false;
                item.IssueCount = plan?.IssueCount ?? 0;
                item.IsolatedCount = plan?.IsolatedCount ?? 0;
                item.ReuseSharedCount = plan?.ReuseSharedCount ?? 0;
                item.RequestedServicesText = string.Join(", ", GetRequestedServices(item));
                item.SourceChangedDisplay =
                    session.Status == SharedRuntimeStatus.Running
                    && !string.IsNullOrWhiteSpace(session.StartedHead)
                    && snapshot is not null
                    && !string.IsNullOrWhiteSpace(snapshot.Head)
                    && !string.Equals(session.StartedHead, snapshot.Head, StringComparison.OrdinalIgnoreCase)
                        ? "Source changed"
                        : null;
                item.RuntimeCaption = session.OwnedServiceNames.Count == 0
                    ? "Stopped"
                    : string.Join(", ", session.OwnedServiceNames.Select(name =>
                    {
                        var row = item.PlanRows.FirstOrDefault(planRow =>
                            string.Equals(planRow.ServiceName, name, StringComparison.OrdinalIgnoreCase));
                        return $"{name} :{row?.PortDisplay ?? "—"}";
                    }));
                item.ApplyStatus(session.Status, plan?.Summary);
                item.CanExecuteStart = WorkspaceRuntimeCommandGate.CanStart(
                    snapshot is not null,
                    item.CanStart,
                    session.Status,
                    item.IsStarting,
                    IsProfileBusy);
                item.CanExecuteStop = WorkspaceRuntimeCommandGate.CanStop(item.OwnedCount, IsProfileBusy, item.IsStarting);
                item.CanExecuteCancel = WorkspaceRuntimeCommandGate.CanCancel(item.IsStarting);
            }

            NotifyOverviewSummariesChanged();
            SyncAgentSessionsRuntimeCaptions();
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Apply);
        }
        else
        {
            Apply();
        }
    }

    private void ApplyWorkspaceRuntimePlan(WorkspaceRuntimeItem item, WorkspaceRuntimePlan plan)
    {
        item.PlanRows.Clear();
        item.ValidationLines.Clear();
        foreach (var entry in plan.Entries)
        {
            item.PlanRows.Add(new WorkspaceRuntimePlanRow
            {
                ServiceName = entry.ServiceName,
                ActionDisplay = entry.ActionDisplay,
                ScopeDisplay = entry.ScopeDisplay,
                PortDisplay = entry.Port?.ToString() ?? "—",
                OwnerDisplay = entry.OwnerDisplay,
                SourcePath = string.IsNullOrWhiteSpace(entry.SourcePath) ? "—" : entry.SourcePath,
                Reason = entry.Reason ?? string.Empty
            });
        }

        if (plan.CanStart)
        {
            item.ValidationLines.Add("✓ Workspace valid");
            item.ValidationLines.Add("✓ Branch valid");
            item.ValidationLines.Add("✓ Dependencies valid");
            item.ValidationLines.Add("✓ Ports valid");
            item.ValidationLines.Add("✓ Source valid");
            item.ValidationLines.Add("✓ Shared Runtime protected");
        }
        else
        {
            foreach (var issue in plan.Issues)
            {
                item.ValidationLines.Add($"BLOCKED · {issue.Message}");
            }
        }

        foreach (var warning in plan.Warnings)
        {
            item.ValidationLines.Add($"WARN · {warning}");
        }

        SyncWorkspaceRuntimes();
    }

    private void RebuildIsolationChoices()
    {
        foreach (var item in WorkspaceRuntimes)
        {
            var previously = item.IsolationChoices
                .Where(choice => choice.IsSelected)
                .Select(choice => choice.ServiceName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var hadPrevious = item.IsolationChoices.Count > 0;
            item.IsolationChoices.Clear();
            if (SelectedProfile is null)
            {
                continue;
            }

            foreach (var entry in SelectedProfile.Services.Where(service => service.Enabled))
            {
                var choice = new WorkspaceIsolationChoice { ServiceName = entry.ServiceName };
                choice.IsSelected = hadPrevious
                    ? previously.Contains(entry.ServiceName)
                    : string.Equals(entry.ServiceName, "OrganizationService", StringComparison.OrdinalIgnoreCase);
                item.IsolationChoices.Add(choice);
            }
        }
    }

    private static IReadOnlyList<string> GetRequestedServices(WorkspaceRuntimeItem item)
        => item.IsolationChoices
            .Where(choice => choice.IsSelected)
            .Select(choice => choice.ServiceName)
            .ToList();

    private DevelopmentWorkspaceItem? FindWorkspaceSnapshot(string workspaceId)
        => Workspaces.FirstOrDefault(item =>
            string.Equals(item.Id, workspaceId, StringComparison.OrdinalIgnoreCase));

    private void SyncAgentSessionsRuntimeCaptions()
    {
        foreach (var session in AgentSessions)
        {
            var runtime = WorkspaceRuntimes.FirstOrDefault(item =>
                string.Equals(item.WorkspaceId, session.WorkspaceId, StringComparison.OrdinalIgnoreCase));
            session.RuntimeDisplay = runtime is null || runtime.OwnedCount == 0
                ? "—"
                : runtime.RuntimeCaption;
        }
    }

    [RelayCommand]
    private void EditSelectedProfile()
    {
        var target = SelectedProfile ?? RuntimeProfiles.FirstOrDefault();
        if (target is null)
        {
            ProfileStatusMessage = "No profile available to edit.";
            return;
        }

        var discoveredNames = serviceManager.Services
            .Select(service => service.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (discoveredNames.Count == 0)
        {
            discoveredNames = target.Services.Select(entry => entry.ServiceName).ToList();
        }

        var editor = new RuntimeProfileEditorWindow(target, discoveredNames)
        {
            Owner = Application.Current?.MainWindow
        };

        if (editor.ShowDialog() == true && editor.Result is not null)
        {
            profileOrchestrator.SaveProfile(editor.Result);
            SelectedProfile = RuntimeProfiles.FirstOrDefault(profile =>
                string.Equals(profile.Id, editor.Result.Id, StringComparison.OrdinalIgnoreCase));
            ProfileStatusMessage = $"Saved profile '{editor.Result.Name}'.";
        }
    }

    private void SyncProfiles()
    {
        void Apply()
        {
            var selectedId = SelectedProfile?.Id;
            RuntimeProfiles.Clear();
            foreach (var profile in profileOrchestrator.GetProfiles())
            {
                RuntimeProfiles.Add(profile);
            }

            SelectedProfile = RuntimeProfiles.FirstOrDefault(profile =>
                                   string.Equals(profile.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                               ?? RuntimeProfiles.FirstOrDefault();
            ProfileStatusMessage = $"{RuntimeProfiles.Count} runtime profile(s) loaded.";
            RebuildIsolationChoices();
            NotifyOverviewSummariesChanged();
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Apply);
        }
        else
        {
            Apply();
        }
    }

    private void SyncFolders()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            SolutionFolders.Clear();
            foreach (var folder in folderRegistry.Folders)
            {
                SolutionFolders.Add(folder);
            }
        });
    }

    [RelayCommand]
    private async Task RefreshLocalPullRequestsAsync()
    {
        if (IsPullRequestBusy)
        {
            return;
        }

        IsPullRequestBusy = true;
        PullRequestStatusMessage = "Refreshing local PRs from Git…";
        try
        {
            await pullRequestService.RefreshAllAsync();
            SyncPullRequests();
            PullRequestStatusMessage = $"{LocalPullRequests.Count} local PR(s).";
        }
        catch (Exception ex)
        {
            PullRequestStatusMessage = $"PR refresh failed: {ex.Message}";
        }
        finally
        {
            IsPullRequestBusy = false;
            UpdateMergeAvailability();
        }
    }

    [RelayCommand]
    private async Task CreateLocalPullRequestAsync()
    {
        var dialog = new CreateLocalPullRequestWindow(pullRequestService)
        {
            Owner = Application.Current?.MainWindow
        };
        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        IsPullRequestBusy = true;
        try
        {
            var created = await pullRequestService.CreateAsync(
                dialog.Result.Title,
                dialog.Result.Description,
                dialog.Result.SourceBranch,
                dialog.Result.TargetBranch,
                dialog.Result.SourceWorktreePath,
                dialog.Result.TargetWorktreePath,
                dialog.Result.AuthorAgent);
            SyncPullRequests();
            SelectedPullRequest = LocalPullRequests.FirstOrDefault(item =>
                string.Equals(item.Id, created.Id, StringComparison.OrdinalIgnoreCase));
            PullRequestStatusMessage = $"Created local PR '{created.Title}' — {created.StatusDisplay}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Create Local PR", MessageBoxButton.OK, MessageBoxImage.Warning);
            PullRequestStatusMessage = ex.Message;
        }
        finally
        {
            IsPullRequestBusy = false;
            UpdateMergeAvailability();
        }
    }

    [RelayCommand]
    private async Task ApproveSelectedPullRequestAsync()
    {
        if (SelectedPullRequest is null)
        {
            return;
        }

        try
        {
            await pullRequestService.ApproveAsync(SelectedPullRequest, "Human", ReviewComment);
            SyncPullRequests();
            PullRequestStatusMessage = $"Approved {SelectedPullRequest.Title} @ {SelectedPullRequest.LastReview?.SourceCommitSha?[..7]}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Approve Local PR", MessageBoxButton.OK, MessageBoxImage.Warning);
            PullRequestStatusMessage = ex.Message;
        }

        UpdateMergeAvailability();
    }

    [RelayCommand]
    private async Task RequestChangesSelectedPullRequestAsync()
    {
        if (SelectedPullRequest is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ReviewComment))
        {
            MessageBox.Show("A comment is required when requesting changes.", "Request Changes", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await pullRequestService.RequestChangesAsync(SelectedPullRequest, "Human", ReviewComment);
            SyncPullRequests();
            PullRequestStatusMessage = "Changes requested.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Request Changes", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        UpdateMergeAvailability();
    }

    [RelayCommand]
    private async Task MergeSelectedPullRequestAsync()
    {
        if (SelectedPullRequest is null || !CanMergeSelectedPullRequest)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"Merge '{SelectedPullRequest.SourceBranch}' into '{SelectedPullRequest.TargetBranch}' with an explicit --no-ff merge commit?\n\nThis will modify the target worktree only.",
            "Merge Local PR",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        IsPullRequestBusy = true;
        try
        {
            var (succeeded, error) = await pullRequestService.MergeAsync(SelectedPullRequest);
            SyncPullRequests();
            PullRequestStatusMessage = succeeded
                ? $"Merged. Commit {SelectedPullRequest.MergeCommitSha}."
                : error ?? "Merge failed.";
            if (!succeeded)
            {
                MessageBox.Show(error ?? "Merge failed.", "Merge Local PR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally
        {
            IsPullRequestBusy = false;
            UpdateMergeAvailability();
        }
    }

    [RelayCommand]
    private async Task LoadSelectedPullRequestDiffAsync()
    {
        if (SelectedPullRequest is null || string.IsNullOrWhiteSpace(SelectedPullRequest.SelectedFilePath))
        {
            return;
        }

        try
        {
            SelectedPullRequest.SelectedDiffText = await pullRequestService.LoadFileDiffAsync(
                SelectedPullRequest,
                SelectedPullRequest.SelectedFilePath);
        }
        catch (Exception ex)
        {
            SelectedPullRequest.SelectedDiffText = ex.Message;
        }
    }

    private void SyncPullRequests()
    {
        void Apply()
        {
            var selectedId = SelectedPullRequest?.Id;
            LocalPullRequests.Clear();
            foreach (var item in pullRequestService.GetPullRequests())
            {
                LocalPullRequests.Add(item);
            }

            SelectedPullRequest = LocalPullRequests.FirstOrDefault(item =>
                                       string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                                   ?? LocalPullRequests.FirstOrDefault();
            UpdateMergeAvailability();
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Apply);
        }
        else
        {
            Apply();
        }
    }

    private void UpdateMergeAvailability()
    {
        CanMergeSelectedPullRequest =
            SelectedPullRequest is { Status: LocalPullRequestStatus.Approved }
            && SelectedPullRequest.LastReview is { IsApproval: true }
            && SelectedPullRequest.Comparison is
            {
                ApprovalIsStale: false,
                HasConflicts: false,
                SourceClean: true,
                TargetClean: true,
                TargetIsOnTargetBranch: true
            };
    }

    [RelayCommand]
    private async Task RefreshAgentSessionsAsync()
    {
        if (IsAgentSessionBusy)
        {
            return;
        }

        IsAgentSessionBusy = true;
        AgentSessionStatusMessage = "Reconciling agent sessions…";
        try
        {
            await agentSessionService.RefreshAllAsync();
            SyncAgentSessions();
            AgentSessionStatusMessage = $"{AgentSessions.Count} agent session(s).";
        }
        catch (Exception ex)
        {
            AgentSessionStatusMessage = $"Session refresh failed: {ex.Message}";
        }
        finally
        {
            IsAgentSessionBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateAgentSessionAsync()
    {
        var dialog = new CreateAgentSessionWindow(agentSessionService)
        {
            Owner = Application.Current?.MainWindow
        };
        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        IsAgentSessionBusy = true;
        try
        {
            var created = await agentSessionService.CreateAsync(
                dialog.Result.AgentType,
                dialog.Result.Title,
                dialog.Result.Description,
                dialog.Result.TaskId,
                dialog.Result.WorkspaceId,
                dialog.Result.ExpectedBranch,
                dialog.Result.RuntimeProfileId,
                dialog.Result.PullRequestId);
            SyncAgentSessions();
            SelectedAgentSession = AgentSessions.FirstOrDefault(item =>
                string.Equals(item.Id, created.Id, StringComparison.OrdinalIgnoreCase));
            AgentSessionStatusMessage = $"Created {created.AgentDisplay} session '{created.Title}'.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "New Agent Session", MessageBoxButton.OK, MessageBoxImage.Warning);
            AgentSessionStatusMessage = ex.Message;
        }
        finally
        {
            IsAgentSessionBusy = false;
        }
    }

    [RelayCommand]
    private async Task StartSelectedAgentSessionAsync()
    {
        if (SelectedAgentSession is null)
        {
            return;
        }

        try
        {
            await agentSessionService.StartAsync(SelectedAgentSession);
            SyncAgentSessions();
            AgentSessionStatusMessage = $"Started session '{SelectedAgentSession.Title}'.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Start Session", MessageBoxButton.OK, MessageBoxImage.Warning);
            SyncAgentSessions();
        }
    }

    [RelayCommand]
    private async Task PauseSelectedAgentSessionAsync()
    {
        if (SelectedAgentSession is null)
        {
            return;
        }

        try
        {
            await agentSessionService.PauseAsync(SelectedAgentSession);
            SyncAgentSessions();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Pause Session", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task ResumeSelectedAgentSessionAsync()
    {
        if (SelectedAgentSession is null)
        {
            return;
        }

        try
        {
            await agentSessionService.ResumeAsync(SelectedAgentSession);
            SyncAgentSessions();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Resume Session", MessageBoxButton.OK, MessageBoxImage.Warning);
            SyncAgentSessions();
        }
    }

    [RelayCommand]
    private async Task CompleteSelectedAgentSessionAsync()
    {
        if (SelectedAgentSession is null)
        {
            return;
        }

        try
        {
            await agentSessionService.CompleteAsync(SelectedAgentSession);
            SyncAgentSessions();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Complete Session", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task CancelSelectedAgentSessionAsync()
    {
        if (SelectedAgentSession is null)
        {
            return;
        }

        try
        {
            await agentSessionService.CancelAsync(SelectedAgentSession);
            SyncAgentSessions();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Cancel Session", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task RefreshServiceInstancesAsync()
    {
        try
        {
            ServiceInstanceStatusMessage = "Reconciling service instances…";
            await serviceInstanceService.RefreshAsync();
            await portRegistryService.RefreshAsync();
            SyncSelectedServiceInstances();
            SyncPortAllocations();
            ServiceInstanceStatusMessage = $"{serviceInstanceService.GetInstances().Count} instance(s) reconciled.";
        }
        catch (Exception ex)
        {
            ServiceInstanceStatusMessage = $"Instance refresh failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshPortRegistryAsync()
    {
        try
        {
            PortRegistryStatusMessage = "Refreshing port registry…";
            await portRegistryService.RefreshAsync();
            SyncPortAllocations();
            SyncSelectedServiceInstances();
            PortRegistryStatusMessage =
                $"{portRegistryService.GetAllocations().Count} allocation(s) · refreshed {DateTime.Now:HH:mm:ss}.";
        }
        catch (Exception ex)
        {
            PortRegistryStatusMessage = $"Port registry refresh failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AssignPort()
    {
        var instance = SelectedServiceInstance;
        if (instance is null || !instance.IsConfigured || string.IsNullOrWhiteSpace(instance.InstanceId))
        {
            MessageBox.Show(
                "Select a configured service instance first.",
                "Assign Port",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new AssignPortWindow(
            instance.ServiceName,
            BuildInstanceLabel(instance),
            instance.Port,
            port => portRegistryService.ValidateAssignment(port, instance.InstanceId));
        var owner = Application.Current?.MainWindow;
        if (owner is not null)
        {
            dialog.Owner = owner;
        }

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var result = portRegistryService.Assign(dialog.SelectedPort, instance.InstanceId);
        if (!result.Succeeded)
        {
            MessageBox.Show(
                result.Error ?? "Port assignment failed.",
                "Assign Port",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            PortRegistryStatusMessage = result.Error ?? "Port assignment failed.";
            return;
        }

        SyncPortAllocations();
        SyncSelectedServiceInstances();
        PortRegistryStatusMessage = $"Assigned port {dialog.SelectedPort} to {BuildInstanceLabel(instance)}.";
    }

    [RelayCommand]
    private void ReleasePort()
    {
        int? port = SelectedPortAllocation?.Port ?? SelectedServiceInstance?.Port;
        if (port is null)
        {
            MessageBox.Show(
                "Select a port allocation or an instance with an assigned port.",
                "Release Port",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Release port {port.Value} from the registry?\n\nThis does not stop any process.",
            "Release Port",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var result = portRegistryService.Release(port.Value);
        if (!result.Succeeded)
        {
            MessageBox.Show(
                result.Error ?? "Release failed.",
                "Release Port",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            PortRegistryStatusMessage = result.Error ?? "Release failed.";
            return;
        }

        SyncPortAllocations();
        SyncSelectedServiceInstances();
        PortRegistryStatusMessage = $"Released port {port.Value} from the registry (no process was stopped).";
    }

    private void SyncSelectedServiceInstances()
    {
        void Apply()
        {
            var selectedId = SelectedServiceInstance?.InstanceId;
            SelectedServiceInstances.Clear();
            if (SelectedDiscoveredService is null || string.IsNullOrWhiteSpace(SelectedDiscoveredService.Name))
            {
                SelectedServiceInstance = null;
                return;
            }

            var serviceId = SelectedDiscoveredService.Name;
            foreach (var instance in serviceInstanceService.GetInstancesForService(serviceId))
            {
                ApplyRegistryPort(instance);
                SelectedServiceInstances.Add(instance);
            }

            foreach (var placeholder in serviceInstanceService.GetNotConfiguredPlaceholders(serviceId, serviceId))
            {
                SelectedServiceInstances.Add(placeholder);
            }

            SelectedServiceInstance = SelectedServiceInstances.FirstOrDefault(item =>
                                           !string.IsNullOrWhiteSpace(selectedId)
                                           && string.Equals(item.InstanceId, selectedId, StringComparison.OrdinalIgnoreCase))
                                       ?? SelectedServiceInstances.FirstOrDefault();
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Apply);
        }
        else
        {
            Apply();
        }
    }

    private void SyncPortAllocations()
    {
        void Apply()
        {
            var selectedPort = SelectedPortAllocation?.Port;
            PortAllocations.Clear();
            foreach (var item in portRegistryService.GetAllocations().OrderBy(a => a.Port))
            {
                PortAllocations.Add(item);
            }

            SelectedPortAllocation = PortAllocations.FirstOrDefault(item => item.Port == selectedPort)
                                     ?? PortAllocations.FirstOrDefault();
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Apply);
        }
        else
        {
            Apply();
        }
    }

    private void ApplyRegistryPort(ServiceInstanceItem instance)
    {
        if (string.IsNullOrWhiteSpace(instance.InstanceId))
        {
            return;
        }

        var allocation = portRegistryService.GetByInstanceId(instance.InstanceId);
        if (allocation is not null)
        {
            instance.ApplyPort(allocation.Port);
        }
    }

    private static string BuildInstanceLabel(ServiceInstanceItem instance)
    {
        if (instance.ExecutionScope == ServiceExecutionScope.Shared)
        {
            return "Shared";
        }

        if (!string.IsNullOrWhiteSpace(instance.WorkspaceId))
        {
            var id = instance.WorkspaceId.Trim();
            return string.Equals(id, "cursor", StringComparison.OrdinalIgnoreCase) ? "Cursor"
                : string.Equals(id, "claude", StringComparison.OrdinalIgnoreCase) ? "Claude"
                : id;
        }

        return instance.WorkspaceDisplay;
    }

    private void SyncAgentSessions()
    {
        void Apply()
        {
            var selectedId = SelectedAgentSession?.Id;
            AgentSessions.Clear();
            foreach (var session in agentSessionService.GetSessions())
            {
                AgentSessions.Add(session);
            }

            SelectedAgentSession = AgentSessions.FirstOrDefault(item =>
                                       string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                                   ?? AgentSessions.FirstOrDefault();
            SyncAgentSessionsRuntimeCaptions();
            NotifyOverviewSummariesChanged();
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Apply);
        }
        else
        {
            Apply();
        }
    }

    private void OnResourceSnapshotUpdated(LocalResourceSnapshot snapshot)
    {
        void Apply() => ApplyResourceSnapshot(snapshot);
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            Apply();
            return;
        }

        if (dispatcher.CheckAccess())
        {
            Apply();
        }
        else
        {
            dispatcher.Invoke(Apply);
        }
    }

    private void ApplyResourceSnapshot(LocalResourceSnapshot snapshot)
    {
        var host = snapshot.Host;
        LocalResources.CapturedAt = snapshot.CapturedAt.ToLocalTime();
        LocalResources.CpuPercent = host.CpuPercent ?? 0;
        LocalResources.CpuDisplay = LocalResourceFormatting.FormatPercent(host.CpuPercent);
        LocalResources.CpuStatus = host.CpuStatus;
        LocalResources.MemoryPercent = host.MemoryPercent ?? 0;
        LocalResources.MemoryDisplay = LocalResourceFormatting.FormatHostMemory(host);
        LocalResources.MemoryStatus = host.MemoryStatus;
        LocalResources.DiskPercent = host.DiskPercent ?? 0;
        LocalResources.DiskDisplay = LocalResourceFormatting.FormatHostDisk(host);
        LocalResources.DiskRootDisplay = string.IsNullOrWhiteSpace(host.DiskRootPath) ? "—" : host.DiskRootPath;
        LocalResources.DiskStatus = host.DiskStatus;
        LocalResources.MeasurementNotes = host.Notes
            ?? "Local host metrics (not Runtime Agent). Observation only — no automatic orchestration.";
        LocalResources.StatusMessage =
            $"Updated {snapshot.CapturedAt.ToLocalTime():HH:mm:ss} · poll {snapshot.SampleInterval.TotalSeconds:0.#}s · {snapshot.Source}";

        ServiceResourceRows.Clear();
        foreach (var service in snapshot.Services)
        {
            var process = service.Process;
            ServiceResourceRows.Add(new LocalServiceResourceRow
            {
                ServiceName = service.ServiceName,
                ProcessIdDisplay = service.ProcessId?.ToString() ?? "—",
                CpuDisplay = LocalResourceFormatting.FormatPercent(process?.CpuPercent),
                MemoryDisplay = process?.MemoryWorkingSetBytes is long bytes
                    ? LocalResourceFormatting.FormatBytes(bytes)
                    : "—",
                Note = service.AssociationNote,
                ScopeDisplay = "Shared",
                WorkspaceDisplay = "Shared",
                AgentDisplay = "Shared",
                PortDisplay = "—"
            });
            var row = ServiceResourceRows[^1];
            var match = workspaceRuntimeTruth.GetAll().FirstOrDefault(snapshot =>
                snapshot.ProcessId is int pid && service.ProcessId == pid);
            if (match.Launch is not null)
            {
                row.ScopeDisplay = ServiceInstanceItem.FormatWorkspaceLabel(match.WorkspaceId);
                row.WorkspaceDisplay = ServiceInstanceItem.FormatWorkspaceLabel(match.WorkspaceId);
                row.AgentDisplay = ServiceInstanceItem.FormatWorkspaceLabel(match.WorkspaceId);
                row.PortDisplay = match.Port?.ToString() ?? "—";
            }
        }
    }

    private void OnResourceAssessmentUpdated(ResourceAssessment assessment)
    {
        void Apply() => ApplyResourceAssessment(assessment);
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            Apply();
            return;
        }

        dispatcher.Invoke(Apply);
    }

    private void ApplyResourceAssessment(ResourceAssessment assessment)
    {
        ResourceIntelligence.OverallPressure = assessment.OverallPressure;
        ResourceIntelligence.OverallPressureDisplay = OperatorLabels.Pressure(assessment.OverallPressure);
        ResourceIntelligence.CpuDisplay = assessment.CpuWindowAveragePercent is double cpuAvg
            ? $"{cpuAvg:0.#}% avg"
            : assessment.CpuPercent is double cpu
                ? $"{cpu:0.#}%"
                : "—";
        ResourceIntelligence.CpuPressureDisplay = OperatorLabels.Pressure(assessment.CpuPressure);
        ResourceIntelligence.MemoryDisplay = assessment.MemoryTotalBytes <= 0
            ? "—"
            : $"{LocalResourceFormatting.FormatBytes(assessment.MemoryUsedBytes)} / {LocalResourceFormatting.FormatBytes(assessment.MemoryTotalBytes)} ({assessment.MemoryPercent:0.#}%)";
        ResourceIntelligence.MemoryPressureDisplay = OperatorLabels.Pressure(assessment.MemoryPressure);
        ResourceIntelligence.DiskDisplay = assessment.DiskPercent is double disk ? $"{disk:0.#}% used" : "—";
        ResourceIntelligence.DiskPressureDisplay = OperatorLabels.Pressure(assessment.DiskPressure);
        ResourceIntelligence.Summary = assessment.Summary;

        ResourceIntelligence.Recommendations.Clear();
        foreach (var recommendation in assessment.Recommendations)
        {
            ResourceIntelligence.Recommendations.Add(recommendation);
        }

        var primary = assessment.Recommendations.FirstOrDefault();
        ResourceIntelligence.PrimaryRecommendationTitle = primary?.Title ?? string.Empty;
        ResourceIntelligence.PrimaryRecommendationExplanation = primary?.Explanation ?? string.Empty;
        ResourceIntelligence.SuggestedProfileId = assessment.Recommendations
            .Select(item => item.RelatedProfileId)
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
        ResourceIntelligence.SuggestedProfileName = assessment.Recommendations
            .Select(item => item.RelatedProfileName)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
        ResourceIntelligence.HasSuggestedProfile = !string.IsNullOrWhiteSpace(ResourceIntelligence.SuggestedProfileId);

        ResourceIntelligence.ActiveSessions.Clear();
        foreach (var session in assessment.ActiveSessions)
        {
            ResourceIntelligence.ActiveSessions.Add(session);
        }

        ResourceIntelligence.ProfileEstimates.Clear();
        foreach (var estimate in assessment.ProfileEstimates)
        {
            ResourceIntelligence.ProfileEstimates.Add(estimate);
        }

        ResourceIntelligenceStatusMessage =
            $"Assessed {assessment.Timestamp.ToLocalTime():HH:mm:ss} · overall {assessment.OverallPressure}";
    }
}
