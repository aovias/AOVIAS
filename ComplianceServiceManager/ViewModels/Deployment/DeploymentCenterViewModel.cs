using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.Releases;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Contracts.Deployment;
using Aovias.Runtime.SDK;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;
using ComplianceServiceManager.Contracts.Release;
using ComplianceServiceManager.Contracts.Runtime;
using ComplianceServiceManager.Navigation;
using ComplianceServiceManager.Services.Deployment;
using ComplianceServiceManager.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Guided single-service deployment workspace (Sprint 12.1).
/// </summary>
public sealed partial class DeploymentCenterViewModel : ObservableObject, IAsyncDisposable
{
    private static readonly IReadOnlyList<string> LiveStageNames =
    [
        "Validate Package",
        "Upload",
        "Provision",
        "Docker",
        "Health",
        "Certification"
    ];

    private static readonly IReadOnlyList<string> AnalyzeCheckNames =
    [
        "Compatibility",
        "Infrastructure",
        "Secrets",
        "Certificates",
        "Database",
        "Dependencies"
    ];

    private readonly IDeploymentService deploymentService;
    private readonly IReleaseCatalogService releaseCatalogService;
    private readonly IEnvironmentManager environmentManager;
    private readonly IRuntimeService runtimeService;
    private readonly IRuntimeAgentClient runtimeAgentClient;
    private readonly IRuntimeHubClient runtimeHubClient;
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;
    private readonly IWorkspaceNavigator workspaceNavigator;
    private readonly IDeploymentRequestTraceService deploymentRequestTraceService;
    private readonly Dispatcher dispatcher;

    private IReadOnlyList<DeploymentCatalogCardItem> allCatalogCards = [];
    private DateTimeOffset? deploymentStartedUtc;

    [ObservableProperty]
    private DeploymentSessionState sessionState = DeploymentSessionState.NotStarted;

    [ObservableProperty]
    private DeploymentWizardStep wizardStep = DeploymentWizardStep.Environment;

    [ObservableProperty]
    private string statusMessage = "Select an environment and package to begin.";

    [ObservableProperty]
    private RemoteEnvironment? selectedEnvironment;

    [ObservableProperty]
    private DeploymentCatalogCardItem? selectedCatalogCard;

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
    private string analyzeRiskLevel = "Not assessed";

    [ObservableProperty]
    private string analyzeEstimatedDuration = "-";

    [ObservableProperty]
    private bool analyzePassed;

    [ObservableProperty]
    private string currentServiceName = "-";

    [ObservableProperty]
    private string currentServiceStatus = "Ready";

    [ObservableProperty]
    private string currentServiceHealth = "Unknown";

    [ObservableProperty]
    private string currentServiceCertification = "Pending";

    [ObservableProperty]
    private int overallProgressPercent;

    [ObservableProperty]
    private string elapsedTimeDisplay = "00:00";

    [ObservableProperty]
    private string remainingTimeDisplay = "--:--";

    [ObservableProperty]
    private string currentProgressLabel = "Waiting to deploy";

    [ObservableProperty]
    private string deploymentDurationDisplay = "-";

    [ObservableProperty]
    private string runtimeConnectionStatus = "Unknown";

    [ObservableProperty]
    private string dockerStatus = "Unknown";

    [ObservableProperty]
    private string signalRStatus = "Disconnected";

    [ObservableProperty]
    private string deploymentApiStatus = "Unknown";

    [ObservableProperty]
    private string configurationStatus = "Unknown";

    [ObservableProperty]
    private string verificationOutcome = "Pending";

    [ObservableProperty]
    private DeploymentOperatorErrorPresentation? lastError;

    [ObservableProperty]
    private bool isConsoleExpanded;

    [ObservableProperty]
    private bool isAdvancedExpanded;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string catalogSearchText = string.Empty;

    [ObservableProperty]
    private string catalogFilter = "All";

    public bool IsHomeVisible =>
        SessionState is DeploymentSessionState.NotStarted or DeploymentSessionState.ReadyToDeploy
        && WizardStep is DeploymentWizardStep.Environment or DeploymentWizardStep.Package;

    public bool IsAnalyzeVisible =>
        WizardStep == DeploymentWizardStep.Analyze || SessionState == DeploymentSessionState.Analyzing;

    public bool IsReviewVisible => WizardStep == DeploymentWizardStep.Review;

    public bool IsMonitorVisible =>
        SessionState is DeploymentSessionState.Uploading
            or DeploymentSessionState.Provisioning
            or DeploymentSessionState.Deploying
            or DeploymentSessionState.HealthChecking
            or DeploymentSessionState.Certifying
        || WizardStep == DeploymentWizardStep.Monitor;

    public bool IsVerifyVisible =>
        SessionState == DeploymentSessionState.Completed || WizardStep == DeploymentWizardStep.Verify;

    public bool IsFailureVisible => SessionState == DeploymentSessionState.Failed;

    public bool IsRuntimeStripVisible => true;

    public bool IsLiveStagesVisible => IsMonitorVisible;

    public bool IsServicePanelVisible =>
        SelectedCatalogCard is not null && (IsMonitorVisible || IsVerifyVisible || IsFailureVisible);

    public bool IsConsoleVisible =>
        IsConsoleExpanded || SessionState is DeploymentSessionState.Completed or DeploymentSessionState.Failed;

    public bool IsAdvancedVisible => IsAdvancedExpanded;

    public bool CanContinue =>
        WizardStep switch
        {
            DeploymentWizardStep.Environment => SelectedEnvironment is not null,
            DeploymentWizardStep.Package => SelectedCatalogCard is not null,
            DeploymentWizardStep.Analyze => AnalyzePassed,
            DeploymentWizardStep.Review => AnalyzePassed,
            _ => false
        };

    public bool CanAnalyze =>
        SelectedEnvironment is not null
        && SelectedCatalogCard is not null
        && !string.IsNullOrWhiteSpace(SelectedPackagePath)
        && SessionState is not DeploymentSessionState.Analyzing;

    public bool CanDeploy => AnalyzePassed && SessionState == DeploymentSessionState.ReadyToDeploy && !IsBusy;

    public bool CanRetry => SessionState == DeploymentSessionState.Failed;

    public string ConsoleToggleLabel => IsConsoleExpanded ? "Hide Details ▲" : "Show Details ▼";

    public IReadOnlyList<string> CatalogFilters => DeploymentPipelineCatalog.CatalogFilters;

    public ObservableCollection<RemoteEnvironment> Environments { get; } = [];

    public ObservableCollection<DeploymentCatalogCardItem> FilteredCatalogCards { get; } = [];

    public ObservableCollection<DeploymentAnalyzeCheckItem> AnalyzeChecks { get; } = [];

    public ObservableCollection<DeploymentReviewRowItem> ReviewRows { get; } = [];

    public ObservableCollection<DeploymentLiveStageItem> LiveStages { get; } = [];

    public ObservableCollection<DeploymentProgressSubStepItem> ProgressSubSteps { get; } = [];

    public ObservableCollection<DeploymentVerificationCheckItem> VerificationChecks { get; } = [];

    public ObservableCollection<DeploymentConsoleEntry> ConsoleEntries { get; } = [];

    public DeploymentCenterViewModel(
        IDeploymentService deploymentService,
        IReleaseCatalogService releaseCatalogService,
        IEnvironmentManager environmentManager,
        IRuntimeService runtimeService,
        IRuntimeAgentClient runtimeAgentClient,
        IRuntimeHubClient runtimeHubClient,
        IOptionsMonitor<RuntimeSettings> runtimeSettings,
        IWorkspaceNavigator workspaceNavigator,
        IDeploymentRequestTraceService deploymentRequestTraceService)
    {
        this.deploymentService = deploymentService;
        this.releaseCatalogService = releaseCatalogService;
        this.environmentManager = environmentManager;
        this.runtimeService = runtimeService;
        this.runtimeAgentClient = runtimeAgentClient;
        this.runtimeHubClient = runtimeHubClient;
        this.runtimeSettings = runtimeSettings;
        this.workspaceNavigator = workspaceNavigator;
        this.deploymentRequestTraceService = deploymentRequestTraceService;
        dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        ResetLiveStages();
        ResetProgressSubSteps();
        runtimeHubClient.DeploymentProgressReceived += OnDeploymentProgressReceived;
        runtimeHubClient.DeploymentFailureReceived += OnDeploymentFailureReceived;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await ConnectRuntimeStripAsync();
        await RefreshEnvironmentsAsync();
        await RefreshCatalogAsync();
        ResetAnalyzeChecks();
        StatusMessage = "Select an environment and package to begin.";
    }

    [RelayCommand]
    private void OpenPlatformWorkspace()
        => workspaceNavigator.NavigateTo(ShellViewModel.WorkspacePlatformDeployment);

    [RelayCommand]
    private void ContinueWizard()
    {
        if (!CanContinue)
        {
            return;
        }

        WizardStep = WizardStep switch
        {
            DeploymentWizardStep.Environment => DeploymentWizardStep.Package,
            DeploymentWizardStep.Package => DeploymentWizardStep.Analyze,
            DeploymentWizardStep.Analyze => DeploymentWizardStep.Review,
            _ => WizardStep
        };

        NotifyStepVisibilityChanged();
        StatusMessage = WizardStep switch
        {
            DeploymentWizardStep.Package => "Choose a package to deploy.",
            DeploymentWizardStep.Analyze => "Run Analyze to validate the package.",
            DeploymentWizardStep.Review => "Review deployment details and confirm.",
            _ => StatusMessage
        };
    }

    [RelayCommand]
    private void BackWizard()
    {
        if (WizardStep <= DeploymentWizardStep.Environment || IsMonitorVisible)
        {
            return;
        }

        WizardStep--;
        NotifyStepVisibilityChanged();
    }

    [RelayCommand]
    private async Task RefreshCatalogAsync()
    {
        var libraryItems = await releaseCatalogService.DiscoverAsync();
        allCatalogCards = libraryItems
            .Where(item => !string.IsNullOrWhiteSpace(item.PackagePath))
            .Select(item => new DeploymentCatalogCardItem
            {
                Id = item.Id,
                ServiceId = string.IsNullOrWhiteSpace(item.PackageId) ? item.ServiceName : item.PackageId,
                ServiceName = item.ServiceName,
                PackageVersion = item.PackageVersion,
                PublishedDisplay = item.ReleaseDate,
                PackageSize = item.PackageSize,
                CertificationDisplay = NormalizeCertification(item.Status),
                DependenciesDisplay = item.EnvironmentCompatibility,
                PackagePath = item.PackagePath,
                PackageType = item.PackageType,
                ReleaseId = item.ReleaseId,
                FrameworkDisplay = InferFramework(item.PackageType),
                IsCertified = item.Status.Contains("cert", StringComparison.OrdinalIgnoreCase)
                    || item.Status.Contains("pass", StringComparison.OrdinalIgnoreCase),
                IsPlatformReady = item.PackageType.Contains("aovdeploy", StringComparison.OrdinalIgnoreCase),
                NeedsRepublish = item.Status.Contains("fail", StringComparison.OrdinalIgnoreCase)
                    || item.Status.Contains("stale", StringComparison.OrdinalIgnoreCase),
                SortDate = item.SortDate
            })
            .ToList();

        ApplyCatalogFilters();
    }

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
        CurrentServiceName = card.ServiceName;
        AnalyzePassed = false;
        SessionState = DeploymentSessionState.NotStarted;

        await LoadSelectedPackageAsync();
        if (WizardStep < DeploymentWizardStep.Package)
        {
            WizardStep = DeploymentWizardStep.Package;
        }

        NotifyStepVisibilityChanged();
        StatusMessage = $"{card.ServiceName} selected. Run Analyze when ready.";
    }

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        if (!CanAnalyze)
        {
            StatusMessage = "Select an environment and package before analyzing.";
            return;
        }

        await RunAsync(async () =>
        {
            SessionState = DeploymentSessionState.Analyzing;
            WizardStep = DeploymentWizardStep.Analyze;
            LastError = null;
            NotifyStepVisibilityChanged();
            StatusMessage = "Analyzing package…";

            LocalValidation = await deploymentService.ValidateLocalPackageAsync(SelectedPackagePath);
            if (!LocalValidation.IsValid)
            {
                AnalyzePassed = false;
                SessionState = DeploymentSessionState.Failed;
                PopulateAnalyzeChecksFromValidation(false);
                StatusMessage = "Analysis failed. Resolve validation issues and retry.";
                return;
            }

            var upload = await deploymentService.UploadPackageAsync(
                SelectedPackagePath,
                runtimeSettings.CurrentValue.ServerName,
                runtimeSettings.CurrentValue.Environment,
                Environment.UserName);
            SelectedReleaseId = upload.ReleaseId;

            var validation = await deploymentService.ValidateReleaseAsync(SelectedReleaseId);
            if (!validation.IsValid)
            {
                AnalyzePassed = false;
                SessionState = DeploymentSessionState.Failed;
                PopulateAnalyzeChecksFromValidation(false, validation.Errors);
                StatusMessage = "Runtime validation failed.";
                return;
            }

            CurrentPlan = await deploymentService.GenerateDeploymentPlanAsync(SelectedReleaseId, Environment.UserName);
            SelectedRelease = await deploymentService.GetReleaseAsync(SelectedReleaseId);
            AnalyzeRiskLevel = CurrentPlan.RiskLevel.ToString();
            AnalyzeEstimatedDuration = CurrentPlan.EstimatedDuration.ToString(@"mm\:ss");
            AnalyzePassed = true;
            SessionState = DeploymentSessionState.ReadyToDeploy;
            PopulateAnalyzeChecksFromPlan();
            PopulateReviewRows();
            AppendConsole("Analyze", "Passed", "Package analyzed successfully.", "Continue to review and deploy.");
            StatusMessage = $"Analysis complete. Risk: {AnalyzeRiskLevel}.";
        });
    }

    [RelayCommand]
    private async Task DeployConfirmedAsync()
    {
        if (!CanDeploy)
        {
            StatusMessage = "Complete analysis before deploying.";
            return;
        }

        deploymentRequestTraceService.TraceDeployButtonClicked(
            SelectedPackagePath,
            SelectedReleaseId,
            CurrentServiceName);

        await RunAsync(async () =>
        {
            SessionState = DeploymentSessionState.Uploading;
            WizardStep = DeploymentWizardStep.Monitor;
            deploymentStartedUtc = DateTimeOffset.UtcNow;
            LastError = null;
            ResetLiveStages();
            ResetProgressSubSteps();
            NotifyStepVisibilityChanged();
            CurrentServiceStatus = "Deploying";
            CurrentProgressLabel = "Starting deployment";
            StatusMessage = "Deploying package…";

            var result = await deploymentService.DeployAsync(
                SelectedReleaseId,
                Environment.UserName,
                operatorConfirmed: true);

            AppendConsole(CurrentServiceName, "Running", result.Message, "Monitor live progress below.");
            DeploymentDurationDisplay = "-";
        });
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        LastError = null;
        SessionState = DeploymentSessionState.NotStarted;
        WizardStep = DeploymentWizardStep.Analyze;
        AnalyzePassed = false;
        NotifyStepVisibilityChanged();
        await AnalyzeAsync();
    }

    [RelayCommand]
    private void FinishDeployment()
    {
        SessionState = DeploymentSessionState.NotStarted;
        WizardStep = DeploymentWizardStep.Environment;
        SelectedCatalogCard = null;
        SelectedPackagePath = string.Empty;
        SelectedReleaseId = string.Empty;
        AnalyzePassed = false;
        OverallProgressPercent = 0;
        NotifyStepVisibilityChanged();
        StatusMessage = "Deployment finished. Select another package to deploy.";
    }

    [RelayCommand]
    private void ToggleConsole() => IsConsoleExpanded = !IsConsoleExpanded;

    [RelayCommand]
    private void ToggleAdvanced() => IsAdvancedExpanded = !IsAdvancedExpanded;

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
        SelectedCatalogCard = new DeploymentCatalogCardItem
        {
            Id = dialog.FileName,
            ServiceId = Path.GetFileNameWithoutExtension(dialog.FileName),
            ServiceName = Path.GetFileNameWithoutExtension(dialog.FileName),
            PackageVersion = "-",
            PublishedDisplay = File.GetLastWriteTime(dialog.FileName).ToString("yyyy-MM-dd HH:mm"),
            PackageSize = FormatSize(new FileInfo(dialog.FileName).Length),
            CertificationDisplay = "Unknown",
            DependenciesDisplay = "-",
            PackagePath = dialog.FileName,
            PackageType = Path.GetExtension(dialog.FileName),
            FrameworkDisplay = "AspNetCore"
        };

        CurrentServiceName = SelectedCatalogCard.ServiceName;
        await LoadSelectedPackageAsync();
        WizardStep = DeploymentWizardStep.Package;
        NotifyStepVisibilityChanged();
    }

    partial void OnCatalogSearchTextChanged(string value) => ApplyCatalogFilters();

    partial void OnCatalogFilterChanged(string value) => ApplyCatalogFilters();

    partial void OnSessionStateChanged(DeploymentSessionState value) => NotifyStepVisibilityChanged();

    partial void OnWizardStepChanged(DeploymentWizardStep value) => NotifyStepVisibilityChanged();

    partial void OnIsConsoleExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsConsoleVisible));
        OnPropertyChanged(nameof(ConsoleToggleLabel));
    }

    partial void OnIsAdvancedExpandedChanged(bool value) => OnPropertyChanged(nameof(IsAdvancedVisible));

    private void OnDeploymentProgressReceived(object? sender, WorkflowProgressMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.ReleaseId)
            && !string.Equals(message.ReleaseId, SelectedReleaseId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        dispatcher.Invoke(() =>
        {
            SessionState = DeploymentSessionStateMapper.FromWorkflowEvent(message.EventType, message.StepName);
            OverallProgressPercent = message.TotalSteps > 0
                ? (int)Math.Round(message.CompletedSteps * 100d / message.TotalSteps)
                : OverallProgressPercent;
            CurrentProgressLabel = message.StepName ?? message.EventType;
            UpdateElapsedRemaining();
            UpdateLiveStage(message);
            UpdateProgressSubSteps(message);
            CurrentServiceStatus = SessionState switch
            {
                DeploymentSessionState.Completed => "Deployed",
                DeploymentSessionState.Failed => "Failed",
                _ => "Deploying"
            };

            if (message.EventType is "WorkflowCompleted")
            {
                WizardStep = DeploymentWizardStep.Verify;
                VerificationOutcome = "SUCCESS";
                PopulateVerificationChecks(true);
                DeploymentDurationDisplay = deploymentStartedUtc is null
                    ? "-"
                    : (DateTimeOffset.UtcNow - deploymentStartedUtc.Value).ToString(@"mm\:ss");
                CurrentServiceHealth = "Healthy";
                CurrentServiceCertification = "PASS";
                AppendConsole(CurrentServiceName, "Passed", "Deployment completed successfully.", "Review verification and finish.");
                StatusMessage = "Deployment successful.";
            }
            else if (message.EventType is "WorkflowFailed")
            {
                SessionState = DeploymentSessionState.Failed;
                VerificationOutcome = "FAILED";
                AppendConsole(CurrentServiceName, "Failed", message.Message, message.RecoveryAction ?? "Review error details and retry.");
                StatusMessage = message.Message;
            }
            else
            {
                AppendConsole(CurrentServiceName, "Running", message.Message, message.RecoveryAction ?? "Monitor deployment progress.");
            }

            NotifyStepVisibilityChanged();
        });
    }

    private void OnDeploymentFailureReceived(object? sender, DeploymentOperatorErrorDto failure)
    {
        dispatcher.Invoke(() =>
        {
            SessionState = DeploymentSessionState.Failed;
            LastError = DeploymentOperatorErrorPresentation.FromSignalR(failure);
            StatusMessage = $"{LastError.Phase} · {LastError.Component}: {LastError.Reason}";
            AppendConsole(LastError.Component, "Failed", LastError.Reason, LastError.Recovery);
            NotifyStepVisibilityChanged();
        });
    }

    private async Task ConnectRuntimeStripAsync()
    {
        try
        {
            await runtimeAgentClient.GetRuntimeHealthAsync();
            RuntimeConnectionStatus = "Connected";
        }
        catch
        {
            RuntimeConnectionStatus = "Offline";
        }

        try
        {
            var availability = await runtimeAgentClient.GetDeploymentAvailabilityAsync();
            DeploymentApiStatus = availability.Available ? "Available" : "Unavailable";
            ConfigurationStatus = availability.PathPolicyCompliant ? "PASS" : "FAIL";
            DockerStatus = "Healthy";
        }
        catch
        {
            DeploymentApiStatus = "Unavailable";
            ConfigurationStatus = "FAIL";
            DockerStatus = "Unknown";
        }

        try
        {
            await runtimeHubClient.ConnectAsync();
            SignalRStatus = "Connected";
        }
        catch
        {
            SignalRStatus = "Offline";
        }
    }

    private async Task RefreshEnvironmentsAsync()
    {
        Environments.Clear();
        foreach (var environment in await environmentManager.GetEnvironmentsAsync())
        {
            Environments.Add(environment);
        }

        SelectedEnvironment ??= Environments.FirstOrDefault();
    }

    private async Task LoadSelectedPackageAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedPackagePath))
        {
            return;
        }

        SelectedPackage = await deploymentService.LoadPackageAsync(SelectedPackagePath);
        if (SelectedPackage is not null && SelectedCatalogCard is not null)
        {
            CurrentServiceName = SelectedPackage.Manifest.DisplayName;
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
            "Latest" => query.OrderByDescending(card => card.SortDate).Take(12),
            "Certified" => query.Where(card => card.IsCertified),
            "Needs Attention" => query.Where(card => card.NeedsRepublish),
            "Failed Publish" => query.Where(card => card.NeedsRepublish),
            _ => query
        };

        FilteredCatalogCards.Clear();
        foreach (var card in query.OrderByDescending(item => item.SortDate))
        {
            FilteredCatalogCards.Add(card);
        }
    }

    private void ResetAnalyzeChecks()
    {
        AnalyzeChecks.Clear();
        foreach (var name in AnalyzeCheckNames)
        {
            AnalyzeChecks.Add(new DeploymentAnalyzeCheckItem { Name = name, Status = "Pending" });
        }
    }

    private void PopulateAnalyzeChecksFromValidation(bool passed, IReadOnlyList<string>? errors = null)
    {
        ResetAnalyzeChecks();
        var status = passed ? "PASS" : "FAIL";
        foreach (var item in AnalyzeChecks)
        {
            item.Status = status;
        }

        if (!passed && errors is { Count: > 0 })
        {
            LastError = new DeploymentOperatorErrorPresentation
            {
                Headline = "Deployment Failed",
                Category = "PackageValidation",
                Phase = "Analyze",
                Component = "ReleaseClient",
                Reason = string.Join("; ", errors),
                Details = string.Join(Environment.NewLine, errors),
                Recovery = "Fix package issues in Publishing, then retry Analyze."
            };
        }
    }

    private void PopulateAnalyzeChecksFromPlan()
    {
        ResetAnalyzeChecks();
        foreach (var item in AnalyzeChecks)
        {
            item.Status = "PASS";
        }

        AnalyzeChecks.Add(new DeploymentAnalyzeCheckItem
        {
            Name = "Risk",
            Status = AnalyzeRiskLevel.ToUpperInvariant()
        });
        AnalyzeChecks.Add(new DeploymentAnalyzeCheckItem
        {
            Name = "Estimated Deployment",
            Status = AnalyzeEstimatedDuration
        });
    }

    private void PopulateReviewRows()
    {
        ReviewRows.Clear();
        ReviewRows.Add(new DeploymentReviewRowItem { Label = "Runtime", Value = SelectedEnvironment?.Name ?? "-" });
        ReviewRows.Add(new DeploymentReviewRowItem { Label = "Package", Value = CurrentServiceName });
        ReviewRows.Add(new DeploymentReviewRowItem { Label = "Version", Value = SelectedCatalogCard?.PackageVersion ?? "-" });
        ReviewRows.Add(new DeploymentReviewRowItem
        {
            Label = "Dependencies",
            Value = SelectedPackage?.Manifest.Dependencies.Count > 0
                ? string.Join(", ", SelectedPackage.Manifest.Dependencies.Select(item => item.DisplayName))
                : "None"
        });
        ReviewRows.Add(new DeploymentReviewRowItem
        {
            Label = "Database",
            Value = SelectedCatalogCard?.DependenciesDisplay ?? "See package"
        });
        ReviewRows.Add(new DeploymentReviewRowItem { Label = "Estimated Time", Value = AnalyzeEstimatedDuration });
    }

    private void PopulateVerificationChecks(bool passed)
    {
        VerificationChecks.Clear();
        var status = passed ? "PASS" : "FAIL";
        VerificationChecks.Add(new DeploymentVerificationCheckItem { Name = "Deployment", Status = passed ? "SUCCESS" : "FAILED" });
        VerificationChecks.Add(new DeploymentVerificationCheckItem { Name = CurrentServiceName, Status = passed ? "Healthy" : "Unhealthy" });
        VerificationChecks.Add(new DeploymentVerificationCheckItem { Name = "API", Status = status });
        VerificationChecks.Add(new DeploymentVerificationCheckItem { Name = "Health", Status = status });
        VerificationChecks.Add(new DeploymentVerificationCheckItem { Name = "Smoke Tests", Status = status });
        VerificationChecks.Add(new DeploymentVerificationCheckItem { Name = "Certification", Status = status });
    }

    private void ResetLiveStages()
    {
        LiveStages.Clear();
        foreach (var stage in LiveStageNames)
        {
            LiveStages.Add(new DeploymentLiveStageItem
            {
                Name = stage,
                Status = "Waiting",
                ProgressPercent = 0
            });
        }
    }

    private void ResetProgressSubSteps()
    {
        ProgressSubSteps.Clear();
        foreach (var label in new[] { "Uploading Package", "Extracting", "Loading Image", "Starting Container" })
        {
            ProgressSubSteps.Add(new DeploymentProgressSubStepItem
            {
                Label = label,
                Status = "Waiting",
                ProgressPercent = 0
            });
        }
    }

    private void UpdateLiveStage(WorkflowProgressMessage message)
    {
        var stageName = message.StepName switch
        {
            null => null,
            var step when step.Contains("Validate", StringComparison.OrdinalIgnoreCase) => "Validate Package",
            var step when step.Contains("Upload", StringComparison.OrdinalIgnoreCase)
                || step.Contains("Extraction", StringComparison.OrdinalIgnoreCase) => "Upload",
            var step when step.Contains("Stage", StringComparison.OrdinalIgnoreCase)
                || step.Contains("Provision", StringComparison.OrdinalIgnoreCase) => "Provision",
            var step when step.Contains("Docker", StringComparison.OrdinalIgnoreCase)
                || step.Contains("Compose", StringComparison.OrdinalIgnoreCase)
                || step.Contains("Image", StringComparison.OrdinalIgnoreCase) => "Docker",
            var step when step.Contains("Health", StringComparison.OrdinalIgnoreCase)
                || step.Contains("Smoke", StringComparison.OrdinalIgnoreCase) => "Health",
            var step when step.Contains("Acceptance", StringComparison.OrdinalIgnoreCase)
                || step.Contains("Audit", StringComparison.OrdinalIgnoreCase) => "Certification",
            _ => null
        };

        if (stageName is null)
        {
            return;
        }

        var stage = LiveStages.FirstOrDefault(item =>
            string.Equals(item.Name, stageName, StringComparison.OrdinalIgnoreCase));
        if (stage is null)
        {
            return;
        }

        stage.Status = message.EventType switch
        {
            "StepCompleted" => "PASS",
            "StepFailed" => "FAILED",
            "StepStarted" => "RUNNING",
            _ => stage.Status
        };
        stage.ProgressPercent = OverallProgressPercent;
    }

    private void UpdateProgressSubSteps(WorkflowProgressMessage message)
    {
        var label = message.StepName switch
        {
            null => null,
            var step when step.Contains("Upload", StringComparison.OrdinalIgnoreCase) => "Uploading Package",
            var step when step.Contains("Extraction", StringComparison.OrdinalIgnoreCase) => "Extracting",
            var step when step.Contains("Image", StringComparison.OrdinalIgnoreCase) => "Loading Image",
            var step when step.Contains("Compose", StringComparison.OrdinalIgnoreCase)
                || step.Contains("Docker", StringComparison.OrdinalIgnoreCase) => "Starting Container",
            _ => null
        };

        if (label is null)
        {
            return;
        }

        var subStep = ProgressSubSteps.FirstOrDefault(item =>
            string.Equals(item.Label, label, StringComparison.OrdinalIgnoreCase));
        if (subStep is null)
        {
            return;
        }

        subStep.Status = message.EventType switch
        {
            "StepCompleted" => "PASS",
            "StepFailed" => "FAILED",
            "StepStarted" => "RUNNING",
            _ => subStep.Status
        };
        subStep.ProgressPercent = OverallProgressPercent;
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
            SessionState = DeploymentSessionState.Failed;
            LastError = DeploymentOperatorErrorPresentation.FromHttp(exception, CurrentServiceName);
            StatusMessage = $"{LastError.Phase} · {LastError.Component}: {LastError.Reason}";
            AppendConsole(LastError.Component, "Failed", LastError.Reason, LastError.Recovery);
            NotifyStepVisibilityChanged();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AppendConsole(string title, string status, string reason, string recoveryAction)
    {
        ConsoleEntries.Insert(0, new DeploymentConsoleEntry
        {
            Timestamp = DateTimeOffset.Now,
            Title = title,
            Status = status,
            Reason = reason,
            RecoveryAction = recoveryAction
        });

        while (ConsoleEntries.Count > 50)
        {
            ConsoleEntries.RemoveAt(ConsoleEntries.Count - 1);
        }
    }

    private void NotifyStepVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsHomeVisible));
        OnPropertyChanged(nameof(IsAnalyzeVisible));
        OnPropertyChanged(nameof(IsReviewVisible));
        OnPropertyChanged(nameof(IsMonitorVisible));
        OnPropertyChanged(nameof(IsVerifyVisible));
        OnPropertyChanged(nameof(IsFailureVisible));
        OnPropertyChanged(nameof(IsLiveStagesVisible));
        OnPropertyChanged(nameof(IsServicePanelVisible));
        OnPropertyChanged(nameof(IsConsoleVisible));
        OnPropertyChanged(nameof(CanContinue));
        OnPropertyChanged(nameof(CanAnalyze));
        OnPropertyChanged(nameof(CanDeploy));
        OnPropertyChanged(nameof(CanRetry));
    }

    private static string NormalizeCertification(string status)
        => status.Contains("pass", StringComparison.OrdinalIgnoreCase)
            || status.Contains("cert", StringComparison.OrdinalIgnoreCase)
            ? "PASS"
            : status;

    private static string InferFramework(string packageType)
        => packageType.Contains("deploy", StringComparison.OrdinalIgnoreCase) ? "WorkerService" : "AspNetCore";

    private static string FormatSize(long bytes)
        => bytes >= 1_048_576 ? $"{bytes / 1_048_576.0:F1} MB" : $"{bytes / 1024.0:F1} KB";

    public async ValueTask DisposeAsync()
    {
        runtimeHubClient.DeploymentProgressReceived -= OnDeploymentProgressReceived;
        runtimeHubClient.DeploymentFailureReceived -= OnDeploymentFailureReceived;
        await runtimeHubClient.DisconnectAsync();
        if (runtimeHubClient is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}
