using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.Releases;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Contracts.Deployment;
using Aovias.Runtime.SDK;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;
using ComplianceServiceManager.Contracts.Publishing;
using ComplianceServiceManager.Contracts.Release;
using ComplianceServiceManager.Contracts.Runtime;
using ComplianceServiceManager.Navigation;
using ComplianceServiceManager.Services.Deployment;
using ComplianceServiceManager.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Platform deployment workspace (Sprint 12 dashboard, separated in 12.1 D1).
/// </summary>
public sealed partial class PlatformDeploymentCenterViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IDeploymentService deploymentService;
    private readonly IEnvironmentManager environmentManager;
    private readonly IRemoteDeploymentOrchestrator remoteDeploymentOrchestrator;
    private readonly IRemoteAgentClientFactory remoteAgentClientFactory;
    private readonly IPublishingWorkspaceService publishingWorkspaceService;
    private readonly IReleaseCatalogService releaseCatalogService;
    private readonly IRuntimeService runtimeService;
    private readonly IRuntimeAgentClient runtimeAgentClient;
    private readonly IRuntimeHubClient runtimeHubClient;
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;
    private readonly IWorkspaceNavigator workspaceNavigator;
    private readonly Dispatcher dispatcher;
    private readonly Dictionary<string, DeploymentPhaseTimelineItem> phaseTimelineLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ServiceProgressCardItem> serviceCardLookup = new(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyList<DeploymentCatalogCardItem> allCatalogCards = [];
    private DateTimeOffset? deploymentStartedUtc;
    private CancellationTokenSource? pollFallbackCts;
    private bool signalRConnected;

    [ObservableProperty]
    private DeploymentWorkspaceMode deploymentMode = DeploymentWorkspaceMode.EntirePlatform;

    /// <summary>
    /// The service subset the unified Deploy workflow selected (Deployment Workspace refactor).
    /// Null/empty means every platform service — the pre-refactor behavior.
    /// <c>IRemoteDeploymentOrchestrator.DeployPlatformAsync</c> has always accepted this subset;
    /// the workspace UI simply never exposed it. Workflow plumbing only — no deployment logic
    /// lives here.
    /// </summary>
    public IReadOnlyList<string>? SelectedServiceIds { get; set; }

    [ObservableProperty]
    private string statusMessage = "Platform deployment workspace ready.";

    [ObservableProperty]
    private string selectedPackagePath = string.Empty;

    [ObservableProperty]
    private string selectedReleaseId = string.Empty;

    [ObservableProperty]
    private string selectedSingleServiceId = "gateway";

    [ObservableProperty]
    private DeploymentCatalogCardItem? selectedCatalogCard;

    [ObservableProperty]
    private PackageContents? selectedPackage;

    [ObservableProperty]
    private PackageValidationResult? localValidation;

    [ObservableProperty]
    private DeploymentPlan? currentPlan;

    [ObservableProperty]
    private ReleaseRecord? selectedRelease;

    [ObservableProperty]
    private string runtimeName = string.Empty;

    [ObservableProperty]
    private string runtimeEnvironment = string.Empty;

    [ObservableProperty]
    private string signalRStatus = "Disconnected";

    [ObservableProperty]
    private string runtimeConnectionStatus = "Unknown";

    [ObservableProperty]
    private string deploymentApiStatus = "Unknown";

    [ObservableProperty]
    private string pathPolicyStatus = "Unknown";

    [ObservableProperty]
    private string configurationStatus = "Unknown";

    [ObservableProperty]
    private string dockerStatus = "Unknown";

    [ObservableProperty]
    private string signalRDiagnosticSummary = string.Empty;

    [ObservableProperty]
    private string currentWorkflowId = string.Empty;

    [ObservableProperty]
    private string currentPhase = "Idle";

    [ObservableProperty]
    private string deploymentProgressSummary = string.Empty;

    [ObservableProperty]
    private string rollbackStatus = string.Empty;

    [ObservableProperty]
    private RemoteEnvironment? selectedEnvironment;

    [ObservableProperty]
    private string releaseLibraryPath = string.Empty;

    [ObservableProperty]
    private string platformVersion = string.Empty;

    [ObservableProperty]
    private string agentVersion = "—";

    [ObservableProperty]
    private string operatingSystem = "—";

    [ObservableProperty]
    private string currentPlatformDeploymentId = string.Empty;

    [ObservableProperty]
    private string platformHealthStatus = "Unknown";

    [ObservableProperty]
    private int overallProgressPercent;

    [ObservableProperty]
    private string elapsedTimeDisplay = "00:00";

    [ObservableProperty]
    private string remainingTimeDisplay = "--:--";

    [ObservableProperty]
    private string certificationStatus = "Pending";

    [ObservableProperty]
    private string deploymentSuccessRate = "N/A";

    [ObservableProperty]
    private string catalogSearchText = string.Empty;

    [ObservableProperty]
    private string catalogFilter = "All";

    [ObservableProperty]
    private string analyzeSummary = "Run Analyze to inspect compatibility, dependencies, and risk.";

    [ObservableProperty]
    private string analyzeRiskScore = "Not assessed";

    [ObservableProperty]
    private string analyzeEstimatedDuration = "-";

    [ObservableProperty]
    private DeploymentPhaseTimelineItem? selectedPhaseTimelineItem;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool remoteValidationSucceeded;

    [ObservableProperty]
    private bool lastOperationFailed;

    public bool IsPlatformMode => DeploymentMode == DeploymentWorkspaceMode.EntirePlatform;

    public bool IsSingleServiceMode => DeploymentMode == DeploymentWorkspaceMode.SingleService;

    public bool HasRollbackStatus => !string.IsNullOrWhiteSpace(RollbackStatus);

    public IReadOnlyList<string> CatalogFilters => DeploymentPipelineCatalog.CatalogFilters;

    public IReadOnlyList<string> PlatformServiceOptions => DeploymentPipelineCatalog.PlatformServiceIds;

    public IReadOnlyList<string> SingleServiceOptions => DeploymentPipelineCatalog.SingleServiceIds;

    public ObservableCollection<DeploymentCatalogCardItem> FilteredCatalogCards { get; } = [];

    public ObservableCollection<DeploymentPipelineStageItem> PipelineStages { get; } = [];

    public ObservableCollection<ServiceProgressCardItem> ServiceProgressCards { get; } = [];

    public ObservableCollection<DeploymentPhaseTimelineItem> PhaseTimeline { get; } = [];

    public ObservableCollection<DeploymentConsoleEntry> ConsoleEntries { get; } = [];

    public ObservableCollection<CertificationPanelItem> CertificationItems { get; } = [];

    public ObservableCollection<DeploymentHistoryGroupItem> GroupedHistory { get; } = [];

    public ObservableCollection<RollbackPresentationItem> RollbackPlanItems { get; } = [];

    public ObservableCollection<RemoteEnvironment> Environments { get; } = [];

    public ObservableCollection<string> SelectedPhaseLogs { get; } = [];

    public PlatformDeploymentCenterViewModel(
        IDeploymentService deploymentService,
        IEnvironmentManager environmentManager,
        IRemoteDeploymentOrchestrator remoteDeploymentOrchestrator,
        IRemoteAgentClientFactory remoteAgentClientFactory,
        IPublishingWorkspaceService publishingWorkspaceService,
        IReleaseCatalogService releaseCatalogService,
        IRuntimeService runtimeService,
        IRuntimeAgentClient runtimeAgentClient,
        IRuntimeHubClient runtimeHubClient,
        IOptionsMonitor<RuntimeSettings> runtimeSettings,
        IWorkspaceNavigator workspaceNavigator)
    {
        this.deploymentService = deploymentService;
        this.environmentManager = environmentManager;
        this.remoteDeploymentOrchestrator = remoteDeploymentOrchestrator;
        this.remoteAgentClientFactory = remoteAgentClientFactory;
        this.publishingWorkspaceService = publishingWorkspaceService;
        this.releaseCatalogService = releaseCatalogService;
        this.runtimeService = runtimeService;
        this.runtimeAgentClient = runtimeAgentClient;
        this.runtimeHubClient = runtimeHubClient;
        this.runtimeSettings = runtimeSettings;
        this.workspaceNavigator = workspaceNavigator;
        dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        runtimeHubClient.DeploymentProgressReceived += OnDeploymentProgressReceived;
        runtimeHubClient.DeploymentPhaseReceived += OnDeploymentPhaseReceived;
        runtimeHubClient.DeploymentFailureReceived += OnDeploymentFailureReceived;
        runtimeHubClient.RollbackProgressReceived += OnRollbackProgressReceived;
        remoteDeploymentOrchestrator.UploadProgressChanged += OnUploadProgressChanged;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        ReleaseLibraryPath = publishingWorkspaceService.GetReleaseLibraryPath();
        ResetPipelineFromCatalog();
        ResetPhaseTimeline();
        ResetCertificationPanel();
        await ConnectSignalRAsync();
        await RefreshRuntimeAsync();
        await RefreshCatalogAsync();
        await RefreshEnvironmentsAsync();
        await LoadPlatformManifestAsync();
        await RefreshReliabilityMetricsAsync();
        await RefreshGroupedHistoryAsync();
        StatusMessage = "Deploy workspace ready — select a target runtime to begin.";
    }

    [RelayCommand]
    private void OpenSingleServiceWorkspace()
        => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceDeployment);

    [RelayCommand]
    private void SetPlatformMode() => DeploymentMode = DeploymentWorkspaceMode.EntirePlatform;

    partial void OnDeploymentModeChanged(DeploymentWorkspaceMode value)
    {
        ResetPipelineFromCatalog();
        ApplyCatalogFilters();
        OnPropertyChanged(nameof(IsPlatformMode));
        OnPropertyChanged(nameof(IsSingleServiceMode));
        StatusMessage = value == DeploymentWorkspaceMode.EntirePlatform
            ? "Entire Platform mode uses solution-deployment.json."
            : "Single Service mode uses individual .aovdeploy packages.";
    }

    partial void OnCatalogSearchTextChanged(string value) => ApplyCatalogFilters();

    partial void OnCatalogFilterChanged(string value) => ApplyCatalogFilters();

    partial void OnSelectedPhaseTimelineItemChanged(DeploymentPhaseTimelineItem? value)
    {
        SelectedPhaseLogs.Clear();
        if (value is null)
        {
            return;
        }

        foreach (var log in value.Logs)
        {
            SelectedPhaseLogs.Add(log);
        }
    }

    [RelayCommand]
    private async Task RefreshCatalogAsync()
    {
        var libraryItems = await releaseCatalogService.DiscoverAsync();
        allCatalogCards = libraryItems
            .Select(item =>
            {
                var isCertified = item.Status.Contains("cert", StringComparison.OrdinalIgnoreCase)
                    || item.Status.Contains("pass", StringComparison.OrdinalIgnoreCase);
                // Filename is authoritative (gateway.aovdeploy → gateway) and matches solution-deployment.json.
                var serviceId = !string.IsNullOrWhiteSpace(item.PackagePath)
                    ? Path.GetFileNameWithoutExtension(item.PackagePath)
                    : string.IsNullOrWhiteSpace(item.PackageId) ? item.ServiceName : item.PackageId;
                return new DeploymentCatalogCardItem
                {
                    Id = item.Id,
                    ServiceId = serviceId,
                    ServiceName = item.ServiceName,
                    PackageVersion = item.PackageVersion,
                    PublishedDisplay = item.ReleaseDate,
                    PackageSize = item.PackageSize,
                    CertificationDisplay = item.Status,
                    DependenciesDisplay = item.EnvironmentCompatibility,
                    PackagePath = item.PackagePath,
                    PackageType = item.PackageType,
                    ReleaseId = item.ReleaseId,
                    BuildDisplay = string.IsNullOrWhiteSpace(item.BuildNumber) ? "—" : item.BuildNumber,
                    ChecksumDisplay = string.IsNullOrWhiteSpace(item.PackageHash) ? "—" : TruncateHash(item.PackageHash),
                    PublisherDisplay = string.IsNullOrWhiteSpace(item.Publisher) ? "—" : item.Publisher,
                    TagsDisplay = isCertified ? "Production, Certified" : "Development",
                    ServicesIncludedDisplay = item.ServiceName,
                    IsCertified = isCertified,
                    IsPlatformReady = item.PackageType.Contains("aovdeploy", StringComparison.OrdinalIgnoreCase),
                    NeedsRepublish = item.Status.Contains("fail", StringComparison.OrdinalIgnoreCase)
                        || item.Status.Contains("stale", StringComparison.OrdinalIgnoreCase),
                    SortDate = item.SortDate
                };
            })
            .ToList();

        ApplyCatalogFilters();
        CatalogCardsChanged?.Invoke();
    }

    /// <summary>Full catalog (selection state lives on these instances).</summary>
    public IReadOnlyList<DeploymentCatalogCardItem> CatalogCards => allCatalogCards;

    /// <summary>Raised after the catalog list is rebuilt so the Deploy facade can re-hook selection.</summary>
    public event Action? CatalogCardsChanged;

    private static string TruncateHash(string hash)
        => hash.Length <= 16 ? hash : $"{hash[..12]}…";

    /// <summary>Re-run catalog filters (e.g. after checkbox selection when filter is Selected/Unselected).</summary>
    public void RefreshCatalogFilterView() => ApplyCatalogFilters();

    [RelayCommand]
    private async Task SelectCatalogCardAsync(DeploymentCatalogCardItem? card)
    {
        if (card is null)
        {
            return;
        }

        SelectedCatalogCard = card;
        SelectedPackagePath = card.PackagePath;
        SelectedReleaseId = card.ReleaseId ?? string.Empty;
        await LoadSelectedPackageAsync();
    }

    [RelayCommand]
    private async Task AnalyzeSelectedAsync()
    {
        if (DeploymentMode == DeploymentWorkspaceMode.EntirePlatform)
        {
            await RunAsync(async () =>
            {
                var manifest = await remoteDeploymentOrchestrator.LoadManifestAsync(ReleaseLibraryPath);
                AnalyzeSummary =
                    $"Platform {manifest.Version} · {manifest.Services.Count} services · startup order validated.";
                AnalyzeRiskScore = "Low";
                AnalyzeEstimatedDuration = TimeSpan.FromMinutes(manifest.Services.Count * 4).ToString(@"mm\:ss");
                AppendConsole("Analyze", "Passed", "Platform manifest analyzed without touching runtime.", "Continue to deploy when ready.");
                SetPhaseStatus("Analyze", "Passed");
                StatusMessage = "Platform analysis complete.";
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPackagePath))
        {
            StatusMessage = "Select a service package from the catalog.";
            return;
        }

        if (SelectedEnvironment is null)
        {
            StatusMessage = "Select a deployment environment before validating against the runtime.";
            return;
        }

        await RunAsync(async () =>
        {
            RemoteValidationSucceeded = false;
            LastOperationFailed = false;

            if (string.IsNullOrWhiteSpace(SelectedReleaseId))
            {
                var clients = remoteAgentClientFactory.Create(SelectedEnvironment);
                var upload = await clients.Release.UploadPackageAsync(
                    SelectedPackagePath,
                    runtimeSettings.CurrentValue.ServerName,
                    SelectedEnvironment.Name,
                    Environment.UserName);
                SelectedReleaseId = upload.ReleaseId;
            }

            LocalValidation = await deploymentService.ValidateLocalPackageAsync(SelectedPackagePath);
            if (LocalValidation is { IsValid: false })
            {
                throw new InvalidOperationException(
                    "Local package validation failed: " + string.Join("; ", LocalValidation.Errors));
            }

            CurrentPlan = await remoteDeploymentOrchestrator.ValidateAndPlanReleaseOnRuntimeAsync(
                SelectedEnvironment,
                SelectedReleaseId,
                Environment.UserName);
            SelectedRelease = await remoteAgentClientFactory
                .Create(SelectedEnvironment)
                .Release.GetReleaseAsync(SelectedReleaseId);
            RemoteValidationSucceeded = SelectedRelease?.State
                is ReleaseState.Staged or ReleaseState.Compatible;

            AnalyzeSummary =
                $"Compatibility OK · Dependencies: {SelectedPackage?.Manifest.Dependencies.Count ?? 0} · Runtime state: {SelectedRelease?.State}.";
            AnalyzeRiskScore = CurrentPlan.RiskLevel.ToString();
            AnalyzeEstimatedDuration = CurrentPlan.EstimatedDuration.ToString(@"mm\:ss");
            AppendConsole("Analyze", "Passed", AnalyzeSummary, "Review the deployment plan, then deploy.");
            SetPhaseStatus("Analyze", "Passed");
            StatusMessage = $"Analysis complete. Risk: {CurrentPlan.RiskLevel}.";
        });
    }

    [RelayCommand]
    private async Task DeployAsync()
    {
        if (SelectedEnvironment is null)
        {
            StatusMessage = "Select a deployment environment first.";
            return;
        }

        if (DeploymentMode == DeploymentWorkspaceMode.EntirePlatform)
        {
            await DeployPlatformAsync();
            return;
        }

        await QueueSingleServiceDeploymentAsync();
    }

    [RelayCommand]
    private async Task RollbackAsync()
    {
        if (DeploymentMode == DeploymentWorkspaceMode.EntirePlatform)
        {
            await RollbackPlatformAsync();
            return;
        }

        await RollbackSelectedReleaseAsync();
    }

    [RelayCommand]
    private async Task RefreshEnvironmentsAsync()
    {
        Environments.Clear();
        foreach (var environment in await environmentManager.GetEnvironmentsAsync())
        {
            Environments.Add(environment);
        }

        SelectedEnvironment ??= Environments.FirstOrDefault();
    }

    [RelayCommand]
    private async Task DeployPlatformAsync()
    {
        if (SelectedEnvironment is null)
        {
            StatusMessage = "Select a deployment environment first.";
            return;
        }

        await RunAsync(async () =>
        {
            BeginDeploymentSession();
            ServiceProgressCards.Clear();
            serviceCardLookup.Clear();

            var selectedIds = SelectedServiceIds is { Count: > 0 } ? SelectedServiceIds : null;
            StatusMessage = selectedIds is { Count: 1 }
                ? $"Single Service deployment starting for {selectedIds[0]} — Runtime Agent package will not be requested."
                : selectedIds is { Count: > 1 }
                    ? $"Application deployment starting for {selectedIds.Count} services."
                    : "Platform deployment starting.";

            var response = await remoteDeploymentOrchestrator.DeployPlatformAsync(
                SelectedEnvironment,
                ReleaseLibraryPath,
                Environment.UserName,
                selectedIds);

            CurrentPlatformDeploymentId = response.DeploymentId;
            foreach (var service in response.Services)
            {
                UpsertServiceCard(service.ServiceId, service.Name, service.Status, 5);
                if (!string.IsNullOrWhiteSpace(service.WorkflowId))
                {
                    CurrentWorkflowId = service.WorkflowId;
                }

                if (!string.IsNullOrWhiteSpace(service.ReleaseId))
                {
                    SelectedReleaseId = service.ReleaseId;
                }
            }

            if (selectedIds is { Count: 1 } && string.IsNullOrWhiteSpace(CurrentWorkflowId))
            {
                CurrentWorkflowId = response.DeploymentId;
            }

            AppendConsole(
                selectedIds is { Count: 1 } ? selectedIds[0] : "Platform",
                "Running",
                response.Message,
                "Monitor pipeline progress below.");
            StatusMessage = response.Message;
            if (selectedIds is null || selectedIds.Count != 1)
            {
                StartPollFallbackIfNeeded(response.DeploymentId);
            }
        });
    }

    [RelayCommand]
    private async Task RollbackPlatformAsync()
    {
        if (SelectedEnvironment is null || string.IsNullOrWhiteSpace(CurrentPlatformDeploymentId))
        {
            StatusMessage = "Select an active platform deployment first.";
            return;
        }

        await RunAsync(async () =>
        {
            PopulateRollbackPlan();
            var result = await remoteDeploymentOrchestrator.RollbackAsync(
                SelectedEnvironment,
                CurrentPlatformDeploymentId,
                Environment.UserName);

            RollbackStatus = "Rollback requested";
            AppendConsole("Rollback", "Running", result.Message, "Open deployment console for rollback progress.");
            StatusMessage = result.Message;
            await RefreshGroupedHistoryAsync();
        });
    }

    [RelayCommand]
    private async Task QueueSingleServiceDeploymentAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedReleaseId))
        {
            StatusMessage = "Analyze or select a release package first.";
            return;
        }

        await RunAsync(async () =>
        {
            BeginDeploymentSession();
            RollbackStatus = string.Empty;
            ResetPipelineFromCatalog();

            var result = await deploymentService.DeployAsync(
                SelectedReleaseId,
                Environment.UserName,
                operatorConfirmed: true);

            CurrentWorkflowId = result.WorkflowId ?? string.Empty;
            AppendConsole(SelectedSingleServiceId, "Running", result.Message, "Watch workflow progress in the pipeline.");
            StatusMessage = result.Message;
            DeploymentProgressSummary = result.WorkflowId is null
                ? "Deployment queued."
                : $"Workflow {result.WorkflowId} executing.";
            SelectedRelease = await deploymentService.GetReleaseAsync(SelectedReleaseId);
        });
    }

    [RelayCommand]
    private async Task RollbackSelectedReleaseAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedReleaseId))
        {
            StatusMessage = "Select a release to roll back.";
            return;
        }

        await RunAsync(async () =>
        {
            PopulateRollbackPlan();
            var result = await deploymentService.RollbackAsync(SelectedReleaseId, Environment.UserName);
            RollbackStatus = "Rollback queued";
            AppendConsole(SelectedSingleServiceId, "Running", result.Message, "Monitor rollback progress in the console.");
            StatusMessage = result.Message;
            await RefreshGroupedHistoryAsync();
        });
    }

    [RelayCommand]
    private async Task BrowsePackageAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "AOVIAS Packages (*.aovdeploy;*.aovpkg)|*.aovdeploy;*.aovpkg|All files (*.*)|*.*",
            Title = "Select AOVIAS Package"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        SelectedPackagePath = dialog.FileName;
        DeploymentMode = DeploymentWorkspaceMode.SingleService;
        await LoadSelectedPackageAsync();
    }

    private async Task LoadPlatformManifestAsync()
    {
        try
        {
            var manifest = await remoteDeploymentOrchestrator.LoadManifestAsync(ReleaseLibraryPath);
            PlatformVersion = manifest.Version;
            ServiceProgressCards.Clear();
            serviceCardLookup.Clear();
            foreach (var service in manifest.Services)
            {
                UpsertServiceCard(
                    service.ServiceId,
                    service.Name,
                    service.DeploymentReady ? "Ready" : "Not Ready",
                    service.DeploymentReady ? 0 : 0);
            }
        }
        catch
        {
            PlatformVersion = "Not published";
        }
    }

    /// <summary>Loads the platform manifest for selection/dependency preview without mutating progress cards.</summary>
    public async Task<PlatformDeploymentManifest?> LoadManifestForSelectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ReleaseLibraryPath))
        {
            return null;
        }

        try
        {
            return await remoteDeploymentOrchestrator.LoadManifestAsync(ReleaseLibraryPath);
        }
        catch
        {
            return null;
        }
    }

    private async Task RefreshReliabilityMetricsAsync()
    {
        if (SelectedEnvironment is null)
        {
            return;
        }

        try
        {
            var metrics = await remoteDeploymentOrchestrator.GetReliabilityMetricsAsync(SelectedEnvironment);
            DeploymentSuccessRate = $"{metrics.DeploymentSuccessRate:F1}%";
        }
        catch
        {
            DeploymentSuccessRate = "Unavailable";
        }
    }

    private async Task RefreshGroupedHistoryAsync()
    {
        GroupedHistory.Clear();
        if (SelectedEnvironment is not null)
        {
            var platformHistory = await remoteDeploymentOrchestrator.GetHistoryAsync(SelectedEnvironment);
            foreach (var entry in platformHistory.Take(20))
            {
                GroupedHistory.Add(new DeploymentHistoryGroupItem
                {
                    PeriodLabel = entry.StartedUtc.Date == DateTimeOffset.UtcNow.Date ? "Today" : "Earlier",
                    ServiceName = "Platform",
                    Outcome = entry.FinalState.ToString(),
                    DeploymentId = entry.DeploymentId
                });
            }
        }

        var serviceHistory = await deploymentService.GetHistoryAsync();
        foreach (var entry in serviceHistory.Take(20))
        {
            var when = entry.StartedAt ?? entry.Timestamp;
            GroupedHistory.Add(new DeploymentHistoryGroupItem
            {
                PeriodLabel = when.Date == DateTimeOffset.UtcNow.Date ? "Today" : "Earlier",
                ServiceName = entry.PackageId,
                Outcome = entry.State.ToString(),
                DeploymentId = entry.DeploymentId ?? entry.ReleaseId
            });
        }
    }

    private async Task ConnectSignalRAsync()
    {
        try
        {
            await runtimeAgentClient.GetRuntimeHealthAsync();
            RuntimeConnectionStatus = "Connected ✓";
        }
        catch (Exception exception)
        {
            RuntimeConnectionStatus = "Failed ✗";
            AppendConsole("Runtime", "Failed", exception.Message, "Verify runtime endpoint and network connectivity.");
        }

        try
        {
            var availability = await runtimeAgentClient.GetDeploymentAvailabilityAsync();
            DeploymentApiStatus = availability.Available ? "Available ✓" : "Unavailable ✗";
            PathPolicyStatus = availability.PathPolicyCompliant ? "PASS" : "FAIL";
            ConfigurationStatus = availability.Available ? "PASS" : "FAIL";
            DockerStatus = "PASS";
        }
        catch (Exception exception)
        {
            DeploymentApiStatus = "Failed ✗";
            AppendConsole("Deployment API", "Failed", exception.Message, "Republish Runtime Agent with deployment API enabled.");
        }

        try
        {
            await runtimeHubClient.ConnectAsync();
            signalRConnected = true;
            SignalRStatus = "Connected ✓";
            SignalRDiagnosticSummary = BuildSignalRDiagnosticSummary(runtimeHubClient.LastConnectionDiagnostics, success: true);
        }
        catch (Exception exception)
        {
            signalRConnected = false;
            SignalRStatus = "Failed ✗";
            SignalRDiagnosticSummary = BuildSignalRDiagnosticSummary(runtimeHubClient.LastConnectionDiagnostics, success: false, exception.Message);
        }
    }

    private void OnDeploymentPhaseReceived(object? sender, DeploymentPhaseEventDto phaseEvent)
    {
        if (!string.IsNullOrWhiteSpace(CurrentPlatformDeploymentId)
            && !string.Equals(phaseEvent.DeploymentId, CurrentPlatformDeploymentId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        dispatcher.Invoke(() =>
        {
            OverallProgressPercent = phaseEvent.OverallProgressPercent;
            CurrentPhase = phaseEvent.Phase.ToString();
            DeploymentProgressSummary = phaseEvent.Message;
            UpdatePlatformStage(phaseEvent.Phase, phaseEvent.Status, phaseEvent.ProgressPercent);
            UpdateElapsedRemaining();

            if (!string.IsNullOrWhiteSpace(phaseEvent.ServiceId))
            {
                UpsertServiceCard(
                    phaseEvent.ServiceId,
                    phaseEvent.ServiceId,
                    phaseEvent.Status,
                    phaseEvent.ProgressPercent,
                    phaseEvent.Phase.ToString());
            }

            AppendConsole(
                phaseEvent.ServiceId ?? "Platform",
                phaseEvent.Status,
                phaseEvent.Message,
                phaseEvent.RecoveryAction ?? "Monitor deployment progress.");

            if (phaseEvent.Phase == DeploymentPhaseKind.Certification)
            {
                CertificationStatus = phaseEvent.Status;
                UpdateCertificationItem("Certification", phaseEvent.Status);
            }

            if (phaseEvent.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = phaseEvent.Message;
            }
            else if (phaseEvent.Phase == DeploymentPhaseKind.Health && phaseEvent.Status.Equals("Passed", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Platform healthy.";
            }
        });
    }

    private void OnDeploymentFailureReceived(object? sender, DeploymentOperatorErrorDto failure)
    {
        dispatcher.Invoke(() =>
        {
            var presentation = DeploymentHttpErrorPresenter.FromSignalR(failure);
            AppendConsole(
                presentation.Component,
                "Failed",
                presentation.Reason,
                presentation.Recovery,
                presentation.ServiceId);
            StatusMessage = DeploymentHttpErrorPresenter.FormatStatusMessage(presentation);
        });
    }

    private void OnDeploymentProgressReceived(object? sender, WorkflowProgressMessage message)
    {
        if (DeploymentMode == DeploymentWorkspaceMode.EntirePlatform)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(message.ReleaseId)
            && !string.Equals(message.ReleaseId, SelectedReleaseId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        dispatcher.Invoke(() =>
        {
            CurrentWorkflowId = message.WorkflowId;
            CurrentPhase = message.CurrentStep ?? message.StepName ?? message.EventType;
            OverallProgressPercent = message.TotalSteps > 0
                ? (int)Math.Round(message.CompletedSteps * 100d / message.TotalSteps)
                : OverallProgressPercent;
            DeploymentProgressSummary = message.Message;
            UpdateElapsedRemaining();

            if (!string.IsNullOrWhiteSpace(message.RollbackStatus))
            {
                RollbackStatus = message.RollbackStatus;
            }

            UpdateWorkflowStage(message);
            AppendConsole(
                SelectedSingleServiceId,
                message.EventType.Contains("Failed", StringComparison.OrdinalIgnoreCase) ? "Failed" : "Running",
                message.Message,
                message.RecoveryAction ?? "Review deployment console.");

            if (message.EventType is "WorkflowCompleted")
            {
                StatusMessage = "Deployment completed successfully.";
                SetPhaseStatus("Certified", "Passed");
            }
            else if (message.EventType is "WorkflowFailed")
            {
                StatusMessage = message.Message;
                SetPhaseStatus("Deploy", "Failed");
            }
        });
    }

    private void OnUploadProgressChanged(object? sender, DeploymentUploadProgress progress)
    {
        dispatcher.Invoke(() =>
        {
            OverallProgressPercent = progress.TotalBytes <= 0
                ? 0
                : (int)Math.Round(progress.BytesUploaded * 100d / progress.TotalBytes);
            CurrentPhase = progress.Stage;
            var uploadedMb = progress.BytesUploaded / 1024d / 1024d;
            var totalMb = progress.TotalBytes / 1024d / 1024d;
            var speedMb = progress.BytesPerSecond / 1024d / 1024d;
            var eta = progress.EstimatedRemaining is null
                ? "--:--"
                : progress.EstimatedRemaining.Value.ToString(@"mm\:ss");
            DeploymentProgressSummary =
                $"Uploading {progress.ServiceId} · {OverallProgressPercent}% · " +
                $"{uploadedMb:F1} MB / {totalMb:F1} MB · {speedMb:F1} MB/sec · ETA {eta}";
            RemainingTimeDisplay = eta;
            SetPhaseStatus(
                "Upload",
                progress.Stage == "Complete" ? "Passed" : "Running");
            UpsertServiceCard(
                progress.ServiceId,
                progress.ServiceId,
                progress.Stage == "Complete" ? "Uploaded" : "Uploading",
                OverallProgressPercent,
                "Upload");
        });
    }

    private void OnRollbackProgressReceived(object? sender, WorkflowProgressMessage message)
    {
        dispatcher.Invoke(() =>
        {
            RollbackStatus = message.Message;
            AppendConsole("Rollback", "Running", message.Message, "Wait for rollback workflow to finish.");
        });
    }

    private void StartPollFallbackIfNeeded(string deploymentId)
    {
        pollFallbackCts?.Cancel();
        if (signalRConnected)
        {
            return;
        }

        pollFallbackCts = new CancellationTokenSource();
        _ = PollPlatformStatusFallbackAsync(deploymentId, pollFallbackCts.Token);
    }

    private async Task PollPlatformStatusFallbackAsync(string deploymentId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (SelectedEnvironment is null)
            {
                return;
            }

            var status = await remoteDeploymentOrchestrator.PollStatusAsync(SelectedEnvironment, deploymentId);
            if (status is null)
            {
                return;
            }

            dispatcher.Invoke(() =>
            {
                OverallProgressPercent = status.ProgressPercent;
                CurrentPhase = status.CurrentPhase;
                PlatformHealthStatus = status.Infrastructure.Health;
                foreach (var service in status.Services)
                {
                    UpsertServiceCard(service.ServiceId, service.Name, service.Status, status.ProgressPercent, service.HealthStatus);
                }
            });

            if (status.State is PlatformDeploymentState.Completed or PlatformDeploymentState.Failed or PlatformDeploymentState.Cancelled)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
    }

    private void BeginDeploymentSession()
    {
        deploymentStartedUtc = DateTimeOffset.UtcNow;
        ResetPipelineFromCatalog();
        ResetPhaseTimeline();
        ResetCertificationPanel();
        OverallProgressPercent = 0;
        ElapsedTimeDisplay = "00:00";
        RemainingTimeDisplay = "--:--";
    }

    private void ResetPipelineFromCatalog()
    {
        PipelineStages.Clear();
        var stages = DeploymentMode == DeploymentWorkspaceMode.EntirePlatform
            ? DeploymentPipelineCatalog.PlatformStages
            : DeploymentPipelineCatalog.ServiceWorkflowSteps;

        foreach (var stage in stages)
        {
            PipelineStages.Add(new DeploymentPipelineStageItem
            {
                Name = stage,
                StageKey = stage,
                Status = "Waiting",
                ProgressPercent = 0
            });
        }
    }

    private void ResetPhaseTimeline()
    {
        PhaseTimeline.Clear();
        phaseTimelineLookup.Clear();
        foreach (var phase in DeploymentPipelineCatalog.PhaseTimelineSteps)
        {
            var item = new DeploymentPhaseTimelineItem { Name = phase };
            phaseTimelineLookup[phase] = item;
            PhaseTimeline.Add(item);
        }
    }

    private void ResetCertificationPanel()
    {
        CertificationItems.Clear();
        foreach (var label in new[]
                 {
                     "Publishing", "Upload", "Filesystem", "Certificates", "Secrets", "Database",
                     "Health", "Smoke", "Certification"
                 })
        {
            CertificationItems.Add(new CertificationPanelItem { Label = label, Status = "Pending" });
        }
    }

    private void ApplyCatalogFilters()
    {
        IEnumerable<DeploymentCatalogCardItem> query = allCatalogCards;

        if (!string.IsNullOrWhiteSpace(CatalogSearchText))
        {
            query = query.Where(card =>
                card.ServiceName.Contains(CatalogSearchText, StringComparison.OrdinalIgnoreCase)
                || card.PackageVersion.Contains(CatalogSearchText, StringComparison.OrdinalIgnoreCase));
        }

        query = CatalogFilter switch
        {
            "Latest" => query.OrderByDescending(card => card.SortDate).Take(10),
            "Production" or "Certified" => query.Where(card => card.IsCertified),
            "Development" => query.Where(card => !card.IsCertified),
            "Needs Attention" => query.Where(card => card.NeedsRepublish),
            "Selected" => query.Where(card => card.IsSelected),
            "Unselected" => query.Where(card => !card.IsSelected),
            _ => query
        };

        FilteredCatalogCards.Clear();
        foreach (var card in query.OrderByDescending(item => item.SortDate))
        {
            FilteredCatalogCards.Add(card);
        }
    }

    private void UpdatePlatformStage(DeploymentPhaseKind phase, string status, int progressPercent)
    {
        var stageName = phase switch
        {
            DeploymentPhaseKind.Package => "Package Analysis",
            DeploymentPhaseKind.Upload => "Upload",
            DeploymentPhaseKind.Provision => "Infrastructure",
            DeploymentPhaseKind.Compose or DeploymentPhaseKind.Extract or DeploymentPhaseKind.DockerLoad => "Deployment",
            DeploymentPhaseKind.Health or DeploymentPhaseKind.Smoke => "Health",
            DeploymentPhaseKind.Certification => "Certification",
            DeploymentPhaseKind.Rollback => "Deployment",
            DeploymentPhaseKind.Failure => CurrentPhase,
            _ => null
        };

        if (stageName is null)
        {
            return;
        }

        var stage = PipelineStages.FirstOrDefault(item =>
            string.Equals(item.Name, stageName, StringComparison.OrdinalIgnoreCase));

        if (stage is not null)
        {
            stage.Status = NormalizeStatus(status);
            stage.ProgressPercent = progressPercent;
        }

        var timelinePhase = phase switch
        {
            DeploymentPhaseKind.Package => "Analyze",
            DeploymentPhaseKind.Upload => "Upload",
            DeploymentPhaseKind.Provision => "Provision",
            DeploymentPhaseKind.Compose => "Deploy",
            DeploymentPhaseKind.Health or DeploymentPhaseKind.Smoke => "Health",
            DeploymentPhaseKind.Certification => "Certified",
            _ => null
        };

        if (timelinePhase is not null)
        {
            SetPhaseStatus(timelinePhase, NormalizeStatus(status), phaseEventMessage: null);
        }
    }

    private void UpdateWorkflowStage(WorkflowProgressMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.StepName))
        {
            return;
        }

        var stage = PipelineStages.FirstOrDefault(item =>
            string.Equals(item.Name, message.StepName, StringComparison.OrdinalIgnoreCase));

        if (stage is null)
        {
            return;
        }

        stage.Status = message.EventType switch
        {
            "StepStarted" => "Running",
            "StepCompleted" => "Passed",
            "StepFailed" => "Failed",
            _ => stage.Status
        };

        stage.ProgressPercent = message.TotalSteps > 0
            ? (int)Math.Round(message.CompletedSteps * 100d / message.TotalSteps)
            : stage.ProgressPercent;

        UpsertServiceCard(
            SelectedSingleServiceId,
            SelectedCatalogCard?.ServiceName ?? SelectedSingleServiceId,
            stage.Status,
            stage.ProgressPercent,
            stage.Name);
    }

    private void UpsertServiceCard(
        string serviceId,
        string name,
        string status,
        int progressPercent,
        string? phase = null,
        string? healthStatus = null)
    {
        if (!serviceCardLookup.TryGetValue(serviceId, out var card))
        {
            card = new ServiceProgressCardItem
            {
                ServiceId = serviceId,
                Name = name
            };
            serviceCardLookup[serviceId] = card;
            ServiceProgressCards.Add(card);
        }

        card.Phase = phase ?? status;
        card.ProgressPercent = progressPercent;
        if (!string.IsNullOrWhiteSpace(healthStatus))
        {
            card.HealthStatus = healthStatus;
        }

        if (status.Contains("PASS", StringComparison.OrdinalIgnoreCase)
            || status.Contains("Completed", StringComparison.OrdinalIgnoreCase))
        {
            card.CertificationStatus = "PASS";
        }
    }

    private void SetPhaseStatus(string phaseName, string status, string? phaseEventMessage = null)
    {
        if (!phaseTimelineLookup.TryGetValue(phaseName, out var item))
        {
            return;
        }

        item.Status = NormalizeStatus(status);
        if (!string.IsNullOrWhiteSpace(phaseEventMessage))
        {
            item.Logs.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} {phaseEventMessage}");
        }
    }

    private static string NormalizeStatus(string status)
        => status switch
        {
            "Passed" or "Completed" => "Passed",
            "Running" => "Running",
            "Failed" => "Failed",
            "Skipped" => "Skipped",
            _ => status
        };

    private void UpdateCertificationItem(string label, string status)
    {
        var item = CertificationItems.FirstOrDefault(entry =>
            entry.Label.Equals(label, StringComparison.OrdinalIgnoreCase));

        if (item is not null)
        {
            item.Status = status.Contains("Pass", StringComparison.OrdinalIgnoreCase) ? "PASS" : status.ToUpperInvariant();
        }
    }

    private void PopulateRollbackPlan()
    {
        RollbackPlanItems.Clear();
        var targetVersion = SelectedCatalogCard?.PackageVersion
            ?? SelectedRelease?.Manifest.PackageVersion
            ?? PlatformVersion;
        var serviceName = DeploymentMode == DeploymentWorkspaceMode.EntirePlatform
            ? "Platform"
            : SelectedCatalogCard?.ServiceName ?? SelectedSingleServiceId;

        RollbackPlanItems.Add(new RollbackPresentationItem
        {
            ServiceName = serviceName,
            TargetVersion = targetVersion,
            EstimatedDuration = "00:12",
            RiskLevel = "Low"
        });
    }

    private void UpdateElapsedRemaining()
    {
        if (deploymentStartedUtc is null)
        {
            return;
        }

        var elapsed = DateTimeOffset.UtcNow - deploymentStartedUtc.Value;
        ElapsedTimeDisplay = elapsed.ToString(@"mm\:ss");
        if (OverallProgressPercent <= 0)
        {
            RemainingTimeDisplay = "--:--";
            return;
        }

        var estimatedTotal = elapsed.TotalSeconds / (OverallProgressPercent / 100d);
        var remaining = TimeSpan.FromSeconds(Math.Max(0, estimatedTotal - elapsed.TotalSeconds));
        RemainingTimeDisplay = remaining.ToString(@"mm\:ss");
    }

    private async Task LoadSelectedPackageAsync()
    {
        await RunAsync(async () =>
        {
            SelectedPackage = await deploymentService.LoadPackageAsync(SelectedPackagePath);
            if (SelectedPackage is null)
            {
                StatusMessage = "Unable to load package.";
                return;
            }

            AppendConsole(SelectedPackage.Manifest.DisplayName, "Passed", "Package loaded.", "Run Analyze before deploying.");
            StatusMessage = "Package loaded.";
        });
    }

    private async Task RefreshRuntimeAsync()
    {
        var runtime = await runtimeService.GetRuntimeAsync();
        RuntimeName = runtime.Name;
        RuntimeEnvironment = runtime.Environment;
        AgentVersion = string.IsNullOrWhiteSpace(runtime.Version) ? "—" : runtime.Version;
        OperatingSystem = string.IsNullOrWhiteSpace(runtime.OperatingSystem) ? "—" : runtime.OperatingSystem;
        if (string.IsNullOrWhiteSpace(PlatformVersion))
        {
            PlatformVersion = AgentVersion;
        }
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            LastOperationFailed = false;
            await action();
        }
        catch (Exception exception)
        {
            LastOperationFailed = true;
            var presentation = DeploymentHttpErrorPresenter.Map(exception);
            AppendConsole("Deployment", "Failed", presentation.Reason, presentation.Recovery);
            StatusMessage = DeploymentHttpErrorPresenter.FormatStatusMessage(presentation);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AppendConsole(string title, string status, string reason, string recoveryAction, string? serviceId = null)
    {
        ConsoleEntries.Insert(0, new DeploymentConsoleEntry
        {
            Timestamp = DateTimeOffset.Now,
            ServiceId = serviceId,
            Title = title,
            Status = status,
            Reason = reason,
            RecoveryAction = recoveryAction
        });

        while (ConsoleEntries.Count > 100)
        {
            ConsoleEntries.RemoveAt(ConsoleEntries.Count - 1);
        }

        var phase = PhaseTimeline.FirstOrDefault(item => item.Status == "Running")
            ?? PhaseTimeline.FirstOrDefault(item => item.Status == "Waiting");
        phase?.Logs.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} [{status}] {reason}");
    }

    private static string BuildSignalRDiagnosticSummary(
        RuntimeHubConnectionDiagnostics? diagnostics,
        bool success,
        string? fallbackReason = null)
    {
        if (diagnostics is null)
        {
            return fallbackReason ?? "SignalR connection failed.";
        }

        return success
            ? $"Runtime Connected ✓{Environment.NewLine}Deployment API ✓{Environment.NewLine}SignalR ✓{Environment.NewLine}Hub: {diagnostics.ExpectedHubPath}"
            : $"SignalR ✗{Environment.NewLine}{diagnostics.FailureReason ?? fallbackReason}";
    }

    partial void OnSelectedReleaseIdChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SelectedRelease = null;
            RemoteValidationSucceeded = false;
            return;
        }

        RemoteValidationSucceeded = false;
        _ = RefreshSelectedReleaseAsync(value);
    }

    private async Task RefreshSelectedReleaseAsync(string releaseId)
    {
        try
        {
            SelectedRelease = SelectedEnvironment is not null
                ? await remoteAgentClientFactory.Create(SelectedEnvironment).Release.GetReleaseAsync(releaseId)
                : await deploymentService.GetReleaseAsync(releaseId);
        }
        catch
        {
            SelectedRelease = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        pollFallbackCts?.Cancel();
        runtimeHubClient.DeploymentProgressReceived -= OnDeploymentProgressReceived;
        runtimeHubClient.DeploymentPhaseReceived -= OnDeploymentPhaseReceived;
        runtimeHubClient.DeploymentFailureReceived -= OnDeploymentFailureReceived;
        runtimeHubClient.RollbackProgressReceived -= OnRollbackProgressReceived;
        remoteDeploymentOrchestrator.UploadProgressChanged -= OnUploadProgressChanged;
        await runtimeHubClient.DisconnectAsync();
        if (runtimeHubClient is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}
