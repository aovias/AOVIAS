using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.Releases;
using Aovias.Runtime.SDK;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;
using ComplianceServiceManager.Contracts.Release;
using ComplianceServiceManager.Contracts.Runtime;
using ComplianceServiceManager.Navigation;
using ComplianceServiceManager.Services.Deployment;
using ComplianceServiceManager.ViewModels;
using ComplianceServiceManager.ViewModels.Deployment;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace ComplianceServiceManager.ViewModels.ReleaseCenter;

/// <summary>
/// Enterprise Release Center workspace — library, wizard, live console, and history.
/// </summary>
public sealed partial class ReleaseCenterViewModel : ObservableObject, IAsyncDisposable
{
    private static readonly string[] WizardStepNames =
    [
        "Select Runtime",
        "Select Release",
        "Analyze",
        "Review",
        "Approve",
        "Deploy",
        "Verify"
    ];

    private static readonly string[] VisualPlanStepNames =
    [
        "Validate Package",
        "Verify Runtime",
        "Verify Dependencies",
        "Backup Current Release",
        "Docker Compose Stop",
        "Release Extraction",
        "Stage Release",
        "Load Docker Image",
        "Ensure Runtime Folders",
        "Docker Compose Up",
        "Configure Nginx",
        "Health Verification",
        "Deployment Acceptance",
        "Activate Release",
        "Register Deployment",
        "Register Runtime",
        "Audit"
    ];

    private static readonly string[] RuntimeTargets =
    [
        "Local",
        "Demo",
        "UAT",
        "Production",
        "Hospital"
    ];

    private readonly IDeploymentService deploymentService;
    private readonly IReleaseCatalogService releaseCatalogService;
    private readonly IRuntimeService runtimeService;
    private readonly IRuntimeHubClient runtimeHubClient;
    private readonly IWorkspaceNavigator workspaceNavigator;
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;

    private IReadOnlyList<ReleaseLibraryItem> allLibraryItems = [];

    [ObservableProperty]
    private string statusMessage = "Discovering releases…";

    [ObservableProperty]
    private int currentWizardStep;

    [ObservableProperty]
    private string selectedRuntimeTarget = "Demo";

    [ObservableProperty]
    private ReleaseLibraryItem? selectedLibraryItem;

    [ObservableProperty]
    private ReleaseHistoryEntry? selectedHistoryEntry;

    [ObservableProperty]
    private string selectedPackagePath = string.Empty;

    [ObservableProperty]
    private string selectedReleaseId = string.Empty;

    [ObservableProperty]
    private PackageContents? selectedPackage;

    [ObservableProperty]
    private PackageValidationResult? localValidation;

    [ObservableProperty]
    private DeploymentPlan? currentPlan;

    [ObservableProperty]
    private ReleaseRecord? selectedRelease;

    [ObservableProperty]
    private string signalRStatus = "Disconnected";

    [ObservableProperty]
    private string currentWorkflowId = string.Empty;

    [ObservableProperty]
    private string currentPipelineStep = "Waiting for deployment…";

    [ObservableProperty]
    private string deploymentProgressSummary = string.Empty;

    [ObservableProperty]
    private string rollbackStatus = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isRollbackMode;

    [ObservableProperty]
    private bool isReleaseDetailsOpen;

    [ObservableProperty]
    private int selectedBottomTabIndex;

    [ObservableProperty]
    private string librarySearchText = string.Empty;

    [ObservableProperty]
    private string filterService = string.Empty;

    [ObservableProperty]
    private string filterEnvironment = string.Empty;

    [ObservableProperty]
    private string filterPackageType = string.Empty;

    [ObservableProperty]
    private string librarySortMode = "Latest";

    [ObservableProperty]
    private string historySearchText = string.Empty;

    [ObservableProperty]
    private string operatorNotes = string.Empty;

    [ObservableProperty]
    private bool operatorApproved;

    [ObservableProperty]
    private int selectedDetailsTabIndex;

    public bool HasRollbackStatus => !string.IsNullOrWhiteSpace(RollbackStatus);

    public IReadOnlyList<string> WizardSteps => WizardStepNames;

    public IReadOnlyList<string> RuntimeTargetsList => RuntimeTargets;

    public IReadOnlyList<string> LibrarySortOptions { get; } = ["Latest", "Service", "Version"];

    public ObservableCollection<ReleaseLibraryItem> FilteredLibraryItems { get; } = [];

    public ObservableCollection<ReleaseHistoryEntry> FilteredHistory { get; } = [];

    public ObservableCollection<ReleaseHistoryEntry> DeploymentHistory { get; } = [];

    public ObservableCollection<DeploymentTimelineItem> LiveTimeline { get; } = [];

    public ObservableCollection<DeploymentPlanStepItem> DeploymentPlanSteps { get; } = [];

    public ObservableCollection<DeploymentPipelineStepItem> PipelineSteps { get; } = [];

    public ObservableCollection<string> ChecksumLines { get; } = [];

    public ObservableCollection<string> DependencyLines { get; } = [];

    public ObservableCollection<string> ConfigurationLines { get; } = [];

    public ObservableCollection<string> InfrastructureLines { get; } = [];

    public ObservableCollection<string> BundlePackageLines { get; } = [];

    public bool IsWizardStepRuntime => CurrentWizardStep == 0;
    public bool IsWizardStepRelease => CurrentWizardStep == 1;
    public bool IsWizardStepAnalyze => CurrentWizardStep == 2;
    public bool IsWizardStepReview => CurrentWizardStep == 3;
    public bool IsWizardStepApprove => CurrentWizardStep == 4;
    public bool IsWizardStepDeploy => CurrentWizardStep == 5;
    public bool IsWizardStepVerify => CurrentWizardStep == 6;

    public string ApplicationDisplay => SelectedPackage?.Manifest.ApplicationName ?? SelectedLibraryItem?.ApplicationName ?? "-";
    public string ServiceDisplay => SelectedPackage?.Manifest.DisplayName ?? SelectedLibraryItem?.ServiceName ?? "-";
    public string VersionDisplay => SelectedPackage?.Manifest.PackageVersion ?? SelectedLibraryItem?.PackageVersion ?? "-";
    public string FrameworkDisplay => SelectedPackage?.Manifest.Framework ?? "-";
    public string BuildDisplay => SelectedPackage?.Manifest.BuildNumber ?? SelectedLibraryItem?.BuildNumber ?? "-";
    public string PackageSizeDisplay => SelectedLibraryItem?.PackageSize ?? "-";
    public string RollbackSupportDisplay => SelectedPackage?.Deployment.RollbackSupported == true ? "Supported" : "Not available";
    public string EstimatedDowntimeDisplay => SelectedPackage?.Deployment.EstimatedDowntime.ToString() ?? CurrentPlan?.EstimatedDuration.ToString() ?? "-";
    public string EstimatedDeploymentTimeDisplay => SelectedPackage?.Deployment.EstimatedDeploymentTime.ToString() ?? "-";
    public string RiskLevelDisplay => CurrentPlan?.RiskLevel.ToString() ?? "Not assessed";
    public string HealthEndpointDisplay => SelectedPackage?.Health.HealthEndpoint ?? "-";
    public string HealthTimeoutDisplay => SelectedPackage?.Health.Timeout.ToString() ?? "-";
    public string HealthRetriesDisplay => SelectedPackage?.Health.RetryCount.ToString(CultureInfo.InvariantCulture) ?? "-";
    public string DatabaseCurrentVersion => "Runtime";
    public string DatabaseTargetVersion => SelectedLibraryItem?.DatabaseVersion ?? "See package";
    public string MigrationCountDisplay => SelectedPackage?.ArtifactPaths.Count(path => path.Contains("migration", StringComparison.OrdinalIgnoreCase)).ToString(CultureInfo.InvariantCulture) ?? "0";
    public string ReleaseNotesDisplay => SelectedPackage?.ReleaseNotes ?? "No release notes in package.";
    public string ManifestSummary => SelectedPackage is null ? "Select a release to inspect the manifest." : $"{SelectedPackage.Manifest.PackageId} · {SelectedPackage.Manifest.PackageVersion} · {SelectedPackage.Manifest.CreatedAt:u}";
    public string OperatorNotesDisplay => string.IsNullOrWhiteSpace(OperatorNotes) ? "No operator notes." : OperatorNotes;

    public ReleaseCenterViewModel(
        IDeploymentService deploymentService,
        IReleaseCatalogService releaseCatalogService,
        IRuntimeService runtimeService,
        IRuntimeHubClient runtimeHubClient,
        IWorkspaceNavigator workspaceNavigator,
        IOptionsMonitor<RuntimeSettings> runtimeSettings)
    {
        this.deploymentService = deploymentService;
        this.releaseCatalogService = releaseCatalogService;
        this.runtimeService = runtimeService;
        this.runtimeHubClient = runtimeHubClient;
        this.workspaceNavigator = workspaceNavigator;
        this.runtimeSettings = runtimeSettings;
        SelectedRuntimeTarget = MapProfileToRuntimeTarget(runtimeSettings.CurrentValue.Environment);
        runtimeHubClient.DeploymentProgressReceived += OnDeploymentProgressReceived;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            await ConnectSignalRAsync();
            await RefreshLibraryAsync();
            await RefreshHistoryAsync();
            ResetVisualPlanSteps();
            StatusMessage = $"{FilteredLibraryItems.Count} releases available. Select a runtime to begin.";
        }
        catch (Exception exception)
        {
            StatusMessage = DeploymentHttpErrorPresenter.FormatStatusMessage(
                DeploymentHttpErrorPresenter.Map(exception));
            SignalRStatus = "Offline";
        }
    }

    [RelayCommand]
    private void OpenDeploymentCommandCenter()
        => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceDeployment);

    [RelayCommand]
    private async Task RefreshLibraryAsync()
    {
        allLibraryItems = await releaseCatalogService.DiscoverAsync();
        ApplyLibraryFilters();
        StatusMessage = $"{FilteredLibraryItems.Count} releases in library.";
    }

    [RelayCommand]
    private async Task SelectLibraryItemAsync(ReleaseLibraryItem? item)
    {
        if (item is null)
        {
            return;
        }

        SelectedLibraryItem = item;
        SelectedPackagePath = item.PackagePath;
        SelectedReleaseId = item.ReleaseId ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(item.PackagePath))
        {
            await LoadSelectedPackageAsync();
        }

        if (CurrentWizardStep < 1)
        {
            CurrentWizardStep = 1;
        }

        NotifyReleaseDetailsChanged();
    }

    [RelayCommand]
    private void NextWizardStep()
    {
        if (CurrentWizardStep >= WizardStepNames.Length - 1)
        {
            return;
        }

        CurrentWizardStep++;
        NotifyWizardStepChanged();
    }

    [RelayCommand]
    private void PreviousWizardStep()
    {
        if (CurrentWizardStep <= 0)
        {
            return;
        }

        CurrentWizardStep--;
        NotifyWizardStepChanged();
    }

    [RelayCommand]
    private async Task AnalyzeReleaseAsync()
    {
        if (SelectedLibraryItem is null)
        {
            StatusMessage = "Select a release from the library.";
            return;
        }

        await RunAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(SelectedPackagePath))
            {
                LocalValidation = await deploymentService.ValidateLocalPackageAsync(SelectedPackagePath);
                if (!LocalValidation.IsValid)
                {
                    StatusMessage = $"Analysis failed: {string.Join("; ", LocalValidation.Errors)}";
                    return;
                }

                var runtime = runtimeSettings.CurrentValue.ServerName;
                var environment = runtimeSettings.CurrentValue.Environment;
                var upload = await deploymentService.UploadPackageAsync(
                    SelectedPackagePath,
                    runtime,
                    environment,
                    Environment.UserName);

                SelectedReleaseId = upload.ReleaseId;
            }
            else if (string.IsNullOrWhiteSpace(SelectedReleaseId))
            {
                StatusMessage = "Unable to analyze — select a release package or a runtime-registered release.";
                return;
            }

            var releaseValidation = await deploymentService.ValidateReleaseAsync(SelectedReleaseId);
            if (!releaseValidation.IsValid)
            {
                StatusMessage = $"Release validation failed: {string.Join("; ", releaseValidation.Errors)}";
                return;
            }

            CurrentPlan = await deploymentService.GenerateDeploymentPlanAsync(SelectedReleaseId, Environment.UserName);
            SelectedRelease = await deploymentService.GetReleaseAsync(SelectedReleaseId);
            PopulateVisualPlanFromCurrentPlan();
            StatusMessage = $"Analysis complete. Risk: {CurrentPlan.RiskLevel}.";
            CurrentWizardStep = 3;
            NotifyWizardStepChanged();
            await RefreshLibraryAsync();
        });
    }

    [RelayCommand]
    private void ApproveDeployment()
    {
        OperatorApproved = true;
        StatusMessage = "Deployment approved by operator.";
        CurrentWizardStep = 5;
        NotifyWizardStepChanged();
    }

    [RelayCommand]
    private async Task ExecuteDeployAsync()
    {
        if (!OperatorApproved)
        {
            StatusMessage = "Approve the deployment before executing.";
            CurrentWizardStep = 4;
            NotifyWizardStepChanged();
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedReleaseId))
        {
            StatusMessage = "No release selected for deployment.";
            return;
        }

        await RunAsync(async () =>
        {
            ResetPipelineSteps();
            LiveTimeline.Clear();
            RollbackStatus = string.Empty;
            SelectedBottomTabIndex = 0;

            var result = await deploymentService.DeployAsync(
                SelectedReleaseId,
                Environment.UserName,
                operatorConfirmed: true);

            CurrentWorkflowId = result.WorkflowId ?? string.Empty;
            StatusMessage = result.Message;
            DeploymentProgressSummary = result.WorkflowId is null
                ? "Deployment queued."
                : $"Workflow {result.WorkflowId} executing.";

            CurrentWizardStep = 6;
            NotifyWizardStepChanged();
            await RefreshLibraryAsync();
            await RefreshHistoryAsync();
            SelectedRelease = await deploymentService.GetReleaseAsync(SelectedReleaseId);
        });
    }

    [RelayCommand]
    private async Task ExecuteRollbackAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedReleaseId))
        {
            StatusMessage = "Select a release to roll back.";
            return;
        }

        await RunAsync(async () =>
        {
            var result = await deploymentService.RollbackAsync(SelectedReleaseId, Environment.UserName);
            StatusMessage = result.Message;
            RollbackStatus = "Rollback queued";
            IsRollbackMode = false;
            await RefreshLibraryAsync();
            await RefreshHistoryAsync();
        });
    }

    [RelayCommand]
    private void StartRollbackWizard()
    {
        IsRollbackMode = true;
        OperatorApproved = false;
        CurrentWizardStep = 3;
        StatusMessage = "Rollback mode — review plan and approve.";
        NotifyWizardStepChanged();
    }

    [RelayCommand]
    private async Task DeployLatestAsync()
    {
        var latest = FilteredLibraryItems.FirstOrDefault();
        if (latest is null)
        {
            StatusMessage = "No releases available.";
            return;
        }

        await SelectLibraryItemAsync(latest);
        CurrentWizardStep = 2;
        NotifyWizardStepChanged();
        await AnalyzeReleaseAsync();
    }

    [RelayCommand]
    private async Task RollbackLastReleaseAsync()
    {
        var last = DeploymentHistory.FirstOrDefault();
        if (last is null)
        {
            StatusMessage = "No deployment history.";
            return;
        }

        SelectedHistoryEntry = last;
        SelectedReleaseId = last.ReleaseId;
        StartRollbackWizard();
        await LoadSelectedReleaseAsync(last.ReleaseId);
    }

    [RelayCommand]
    private void ViewRuntime() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceRuntime);

    [RelayCommand]
    private void ViewTimeline() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceObservability);

    [RelayCommand]
    private void OpenLogs() => SelectedBottomTabIndex = 0;

    [RelayCommand]
    private void OpenReleaseDetails()
    {
        IsReleaseDetailsOpen = true;
        SelectedDetailsTabIndex = 0;
    }

    [RelayCommand]
    private void CloseReleaseDetails() => IsReleaseDetailsOpen = false;

    [RelayCommand]
    private async Task RedeployFromHistoryAsync(ReleaseHistoryEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        SelectedReleaseId = entry.ReleaseId;
        SelectedHistoryEntry = entry;
        await LoadSelectedReleaseAsync(entry.ReleaseId);
        CurrentWizardStep = 4;
        NotifyWizardStepChanged();
        StatusMessage = $"Re-deploy {entry.PackageId} v{entry.PackageVersion} to {entry.Environment}.";
    }

    [RelayCommand]
    private void ExportHistory()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"deployment-history-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Release,Version,Runtime,Environment,Timestamp,Status,Summary");
        foreach (var entry in FilteredHistory)
        {
            builder.AppendLine(string.Join(',',
                Csv(entry.ReleaseId),
                Csv(entry.PackageVersion),
                Csv(entry.Runtime),
                Csv(entry.Environment),
                Csv(entry.Timestamp.ToString("u")),
                Csv(entry.State.ToString()),
                Csv(entry.Summary ?? string.Empty)));
        }

        File.WriteAllText(dialog.FileName, builder.ToString());
        StatusMessage = $"Exported {FilteredHistory.Count} history entries.";
    }

    [RelayCommand]
    private async Task RefreshHistoryAsync()
    {
        try
        {
            DeploymentHistory.Clear();
            var history = await deploymentService.GetHistoryAsync();
            foreach (var entry in history.OrderByDescending(item => item.Timestamp))
            {
                DeploymentHistory.Add(entry);
            }

            ApplyHistoryFilters();
        }
        catch (Exception exception)
        {
            StatusMessage = DeploymentHttpErrorPresenter.FormatStatusMessage(
                DeploymentHttpErrorPresenter.Map(exception));
        }
    }

    partial void OnLibrarySearchTextChanged(string value) => ApplyLibraryFilters();
    partial void OnFilterServiceChanged(string value) => ApplyLibraryFilters();
    partial void OnFilterEnvironmentChanged(string value) => ApplyLibraryFilters();
    partial void OnFilterPackageTypeChanged(string value) => ApplyLibraryFilters();
    partial void OnLibrarySortModeChanged(string value) => ApplyLibraryFilters();
    partial void OnHistorySearchTextChanged(string value) => ApplyHistoryFilters();

    partial void OnSelectedReleaseIdChanged(string value) => _ = LoadSelectedReleaseAsync(value);

    partial void OnRollbackStatusChanged(string value) => OnPropertyChanged(nameof(HasRollbackStatus));

    partial void OnSelectedRuntimeTargetChanged(string value)
    {
        if (CurrentWizardStep == 0)
        {
            StatusMessage = $"Runtime target: {value}. Select a release next.";
        }
    }

    private void ApplyLibraryFilters()
    {
        IEnumerable<ReleaseLibraryItem> query = allLibraryItems;

        if (!string.IsNullOrWhiteSpace(LibrarySearchText))
        {
            query = query.Where(item =>
                item.ServiceName.Contains(LibrarySearchText, StringComparison.OrdinalIgnoreCase) ||
                item.PackageId.Contains(LibrarySearchText, StringComparison.OrdinalIgnoreCase) ||
                item.PackageVersion.Contains(LibrarySearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(FilterService))
        {
            query = query.Where(item => item.ServiceName.Contains(FilterService, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(FilterEnvironment))
        {
            query = query.Where(item => item.EnvironmentCompatibility.Contains(FilterEnvironment, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(FilterPackageType))
        {
            query = query.Where(item => item.PackageType.Contains(FilterPackageType, StringComparison.OrdinalIgnoreCase));
        }

        query = LibrarySortMode switch
        {
            "Service" => query.OrderBy(item => item.ServiceName),
            "Version" => query.OrderByDescending(item => item.PackageVersion),
            _ => query.OrderByDescending(item => item.SortDate)
        };

        FilteredLibraryItems.Clear();
        foreach (var item in query)
        {
            FilteredLibraryItems.Add(item);
        }
    }

    private void ApplyHistoryFilters()
    {
        IEnumerable<ReleaseHistoryEntry> query = DeploymentHistory;
        if (!string.IsNullOrWhiteSpace(HistorySearchText))
        {
            query = query.Where(entry =>
                entry.ReleaseId.Contains(HistorySearchText, StringComparison.OrdinalIgnoreCase) ||
                entry.PackageId.Contains(HistorySearchText, StringComparison.OrdinalIgnoreCase) ||
                entry.Runtime.Contains(HistorySearchText, StringComparison.OrdinalIgnoreCase));
        }

        FilteredHistory.Clear();
        foreach (var entry in query)
        {
            FilteredHistory.Add(entry);
        }
    }

    private async Task ConnectSignalRAsync()
    {
        try
        {
            await runtimeHubClient.ConnectAsync();
            SignalRStatus = "Connected ✓";
            AddTimelineEntry("Connected", "Live deployment console ready.");
        }
        catch (Exception exception)
        {
            SignalRStatus = "Failed ✗";
            var diagnostics = runtimeHubClient.LastConnectionDiagnostics;
            if (diagnostics is not null)
            {
                AddTimelineEntry(
                    "SignalR",
                    $"Reason: {diagnostics.FailureReason ?? exception.Message}\n" +
                    $"Expected: {diagnostics.ExpectedHubPath}\n" +
                    $"Configured: {diagnostics.ConfiguredHubPath}\n" +
                    $"Attempted: {diagnostics.AttemptedHubUrl}");
            }
            else
            {
                AddTimelineEntry("SignalR", exception.Message);
            }
        }
    }

    private void OnDeploymentProgressReceived(object? sender, WorkflowProgressMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.ReleaseId) &&
            !string.Equals(message.ReleaseId, SelectedReleaseId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentWorkflowId = message.WorkflowId;
            CurrentPipelineStep = message.CurrentStep ?? message.StepName ?? message.EventType;
            DeploymentProgressSummary = message.TotalSteps > 0
                ? $"{message.CompletedSteps}/{message.TotalSteps} steps completed"
                : message.Message;

            if (!string.IsNullOrWhiteSpace(message.RollbackStatus))
            {
                RollbackStatus = message.RollbackStatus;
            }

            AddTimelineEntry(message.EventType, message.Message, highlightCurrent: message.EventType is "StepStarted");
            UpdatePipelineFromEvent(message);
            UpdateVisualPlanFromEvent(message);

            if (message.EventType is "WorkflowCompleted")
            {
                StatusMessage = "Deployment completed successfully.";
                _ = RefreshLibraryAsync();
                _ = RefreshHistoryAsync();
            }
            else if (message.EventType is "WorkflowFailed")
            {
                StatusMessage = message.Message;
                _ = RefreshLibraryAsync();
                _ = RefreshHistoryAsync();
            }
        });
    }

    private void ResetPipelineSteps()
    {
        PipelineSteps.Clear();
        foreach (var stepName in VisualPlanStepNames)
        {
            PipelineSteps.Add(new DeploymentPipelineStepItem { Name = stepName, Status = "Pending" });
        }
    }

    private void ResetVisualPlanSteps()
    {
        DeploymentPlanSteps.Clear();
        foreach (var stepName in VisualPlanStepNames)
        {
            DeploymentPlanSteps.Add(new DeploymentPlanStepItem
            {
                Name = stepName,
                Status = "Pending",
                EstimatedDuration = "~30s"
            });
        }
    }

    private void PopulateVisualPlanFromCurrentPlan()
    {
        ResetVisualPlanSteps();
        if (CurrentPlan is null)
        {
            return;
        }

        var perStep = CurrentPlan.EstimatedDuration.TotalSeconds / Math.Max(DeploymentPlanSteps.Count, 1);
        foreach (var step in DeploymentPlanSteps)
        {
            step.EstimatedDuration = TimeSpan.FromSeconds(perStep).ToString();
            step.Status = "Pending";
        }

        foreach (var action in CurrentPlan.RequiredActions.OrderBy(item => item.Order))
        {
            var step = DeploymentPlanSteps.FirstOrDefault(item => string.Equals(item.Name, action.Name, StringComparison.OrdinalIgnoreCase));
            if (step is not null)
            {
                step.Status = "Pending";
                step.EstimatedDuration = TimeSpan.FromSeconds(perStep).ToString();
            }
        }

        if (CurrentPlan.InspectionDetails is not null)
        {
            PopulateInspectionDetails(CurrentPlan.InspectionDetails);
        }
    }

    private void PopulateInspectionDetails(DeploymentInspectionDetails details)
    {
        InfrastructureLines.Clear();
        InfrastructureLines.Add($"Service: {details.ServiceId}");
        InfrastructureLines.Add($"Version: {details.PackageVersion}");
        InfrastructureLines.Add($"Package size: {details.PackageSizeBytes:N0} bytes");
        InfrastructureLines.Add($"Docker image: {details.DockerImageName}:{details.DockerImageTag}");
        InfrastructureLines.Add($"Health endpoint: {details.HealthEndpoint}");
        InfrastructureLines.Add($"Estimated duration: {details.EstimatedDuration}");
        InfrastructureLines.Add($"Risk level: {details.RiskLevel}");
        InfrastructureLines.Add($"Rollback available: {details.RollbackAvailable}");

        ConfigurationLines.Clear();
        foreach (var key in details.ConfigTemplateKeys)
        {
            ConfigurationLines.Add($"Config key: {key} (value redacted)");
        }

        DependencyLines.Clear();
        foreach (var dependency in details.Dependencies)
        {
            DependencyLines.Add(dependency);
        }
    }

    private void UpdatePipelineFromEvent(WorkflowProgressMessage message)
    {
        if (PipelineSteps.Count == 0)
        {
            ResetPipelineSteps();
        }

        if (message.EventType is "StepStarted" && !string.IsNullOrWhiteSpace(message.StepName))
        {
            SetPipelineStepStatus(message.StepName, "Running");
        }
        else if (message.EventType is "StepCompleted" && !string.IsNullOrWhiteSpace(message.StepName))
        {
            SetPipelineStepStatus(message.StepName, "Completed");
        }
        else if (message.EventType is "StepFailed" && !string.IsNullOrWhiteSpace(message.StepName))
        {
            SetPipelineStepStatus(message.StepName, "Failed");
        }
    }

    private void UpdateVisualPlanFromEvent(WorkflowProgressMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.StepName))
        {
            return;
        }

        var status = message.EventType switch
        {
            "StepStarted" => "Running",
            "StepCompleted" => "Success",
            "StepFailed" => "Failed",
            _ => null
        };

        if (status is null)
        {
            return;
        }

        var step = DeploymentPlanSteps.FirstOrDefault(item =>
            item.Name.Contains(message.StepName!, StringComparison.OrdinalIgnoreCase) ||
            message.StepName!.Contains(item.Name, StringComparison.OrdinalIgnoreCase));

        if (step is not null)
        {
            step.Status = status;
        }
    }

    private void SetPipelineStepStatus(string stepName, string status)
    {
        var step = PipelineSteps.FirstOrDefault(item =>
            string.Equals(item.Name, stepName, StringComparison.OrdinalIgnoreCase));

        if (step is not null)
        {
            var index = PipelineSteps.IndexOf(step);
            PipelineSteps[index] = new DeploymentPipelineStepItem { Name = step.Name, Status = status };
        }
    }

    private async Task LoadSelectedPackageAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedPackagePath))
        {
            return;
        }

        await RunAsync(async () =>
        {
            SelectedPackage = await deploymentService.LoadPackageAsync(SelectedPackagePath);
            if (SelectedPackage is null)
            {
                StatusMessage = "Unable to load package.";
                return;
            }

            ChecksumLines.Clear();
            foreach (var checksum in SelectedPackage.Checksums)
            {
                ChecksumLines.Add($"{checksum.Key}  {checksum.Value}");
            }

            DependencyLines.Clear();
            foreach (var dependency in SelectedPackage.Manifest.Dependencies)
            {
                DependencyLines.Add($"{dependency.DisplayName} ({dependency.PackageId} {dependency.PackageVersion})");
            }

            ConfigurationLines.Clear();
            ConfigurationLines.Add($"Strategy: {SelectedPackage.Deployment.DeploymentStrategy}");
            ConfigurationLines.Add($"Activation: {SelectedPackage.Deployment.ActivationMode}");
            ConfigurationLines.Add($"Restart required: {SelectedPackage.Deployment.RequiresRestart}");

            InfrastructureLines.Clear();
            InfrastructureLines.Add("Docker Compose: Included in package");
            InfrastructureLines.Add($"Health endpoint: {SelectedPackage.Health.HealthEndpoint}");
            InfrastructureLines.Add($"Expected status: {SelectedPackage.Health.ExpectedStatusCode}");

            BundlePackageLines.Clear();
            if (SelectedLibraryItem?.PackageType == ".aovbundle")
            {
                BundlePackageLines.Add("Gateway · Identity · Compliance · Organization");
                BundlePackageLines.Add("Audit · KnowledgeBase · Angular · Configuration · Database");
            }

            StatusMessage = $"Loaded {SelectedPackage.Manifest.PackageId} v{SelectedPackage.Manifest.PackageVersion}.";
            NotifyReleaseDetailsChanged();
        });
    }

    private async Task LoadSelectedReleaseAsync(string releaseId)
    {
        if (string.IsNullOrWhiteSpace(releaseId))
        {
            SelectedRelease = null;
            return;
        }

        try
        {
            SelectedRelease = await deploymentService.GetReleaseAsync(releaseId);
            CurrentPlan = SelectedRelease?.LastPlan ?? CurrentPlan;
            NotifyReleaseDetailsChanged();
        }
        catch (Exception exception)
        {
            var presentation = DeploymentHttpErrorPresenter.Map(exception, SelectedLibraryItem?.ServiceName);
            StatusMessage = DeploymentHttpErrorPresenter.FormatStatusMessage(presentation);
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
            await action();
        }
        catch (Exception exception)
        {
            var presentation = DeploymentHttpErrorPresenter.Map(exception, SelectedLibraryItem?.ServiceName);
            StatusMessage = DeploymentHttpErrorPresenter.FormatStatusMessage(presentation);
            AddTimelineEntry(presentation.Component, DeploymentHttpErrorPresenter.FormatTimelineMessage(presentation));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AddTimelineEntry(string title, string message, bool highlightCurrent = false)
    {
        foreach (var item in LiveTimeline)
        {
            if (item.IsCurrent)
            {
                LiveTimeline[LiveTimeline.IndexOf(item)] = new DeploymentTimelineItem
                {
                    Timestamp = item.Timestamp,
                    Title = item.Title,
                    Message = item.Message,
                    IsCurrent = false
                };
            }
        }

        LiveTimeline.Insert(0, new DeploymentTimelineItem
        {
            Timestamp = DateTime.Now.ToString("HH:mm"),
            Title = title,
            Message = message,
            IsCurrent = highlightCurrent
        });

        while (LiveTimeline.Count > 100)
        {
            LiveTimeline.RemoveAt(LiveTimeline.Count - 1);
        }
    }

    private void NotifyWizardStepChanged()
    {
        OnPropertyChanged(nameof(IsWizardStepRuntime));
        OnPropertyChanged(nameof(IsWizardStepRelease));
        OnPropertyChanged(nameof(IsWizardStepAnalyze));
        OnPropertyChanged(nameof(IsWizardStepReview));
        OnPropertyChanged(nameof(IsWizardStepApprove));
        OnPropertyChanged(nameof(IsWizardStepDeploy));
        OnPropertyChanged(nameof(IsWizardStepVerify));
    }

    private void NotifyReleaseDetailsChanged()
    {
        OnPropertyChanged(nameof(ApplicationDisplay));
        OnPropertyChanged(nameof(ServiceDisplay));
        OnPropertyChanged(nameof(VersionDisplay));
        OnPropertyChanged(nameof(FrameworkDisplay));
        OnPropertyChanged(nameof(BuildDisplay));
        OnPropertyChanged(nameof(PackageSizeDisplay));
        OnPropertyChanged(nameof(RollbackSupportDisplay));
        OnPropertyChanged(nameof(EstimatedDowntimeDisplay));
        OnPropertyChanged(nameof(EstimatedDeploymentTimeDisplay));
        OnPropertyChanged(nameof(RiskLevelDisplay));
        OnPropertyChanged(nameof(HealthEndpointDisplay));
        OnPropertyChanged(nameof(HealthTimeoutDisplay));
        OnPropertyChanged(nameof(HealthRetriesDisplay));
        OnPropertyChanged(nameof(DatabaseTargetVersion));
        OnPropertyChanged(nameof(MigrationCountDisplay));
        OnPropertyChanged(nameof(ReleaseNotesDisplay));
        OnPropertyChanged(nameof(ManifestSummary));
    }

    private static (string Server, string Environment) MapRuntime(string target) => target switch
    {
        "UAT" => ("uat", "UAT"),
        "Production" => ("production", "Production"),
        "Local" => ("local", "Local"),
        "Hospital" => ("hospital", "Hospital"),
        _ => ("demo", "Demo")
    };

    private static string MapProfileToRuntimeTarget(string environment) => environment switch
    {
        "UAT" => "UAT",
        "Production" => "Production",
        "Local" => "Local",
        "Hospital" => "Hospital",
        _ => "Demo"
    };

    private static string Csv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        runtimeHubClient.DeploymentProgressReceived -= OnDeploymentProgressReceived;

        try
        {
            await runtimeHubClient.DisconnectAsync();
        }
        catch (ObjectDisposedException)
        {
            // Hub already torn down during application shutdown.
        }
    }
}
