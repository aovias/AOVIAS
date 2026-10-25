using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.Contracts.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.ViewModels.Deployment;

namespace ComplianceServiceManager.ViewModels.Applications;

/// <summary>
/// Applications → Deploy: unified deployment workflow.
/// Release library checkboxes select both the package and the service — one panel, one action.
/// </summary>
public sealed partial class DeploymentsWorkspaceViewModel : ObservableObject
{
    private bool isRecomputing;
    private bool recomputeQueued;
    private bool isSyncingReleaseFocus;

    [ObservableProperty]
    private string consoleSearchText = string.Empty;

    [ObservableProperty]
    private string consoleLevelFilter = "All";

    [ObservableProperty]
    private int selectedConsoleTab;

    public PlatformDeploymentCenterViewModel Center { get; }

    public ObservableCollection<DeployWorkflowStepItem> Steps { get; } = [];

    public ObservableCollection<DeployPlanCheckItem> PlanChecks { get; } = [];

    public ObservableCollection<DeployBlockerItem> Blockers { get; } = [];

    public ObservableCollection<DeploymentConsoleEntry> FilteredConsoleEntries { get; } = [];

    public IReadOnlyList<string> ConsoleLevelFilters { get; } = ["All", "INFO", "WARN", "ERROR", "SUCCESS"];

    public DeploymentsWorkspaceViewModel(PlatformDeploymentCenterViewModel center)
    {
        Center = center;

        // Runtime → Releases (packages + services) → Validation → Deploy → Verification
        Steps.Add(new DeployWorkflowStepItem { Number = 1, Title = "Runtime", SectionName = "RuntimeSection" });
        Steps.Add(new DeployWorkflowStepItem { Number = 2, Title = "Releases", SectionName = "ReleaseSection" });
        Steps.Add(new DeployWorkflowStepItem { Number = 3, Title = "Validation", SectionName = "PlanSection" });
        Steps.Add(new DeployWorkflowStepItem { Number = 4, Title = "Deploy", SectionName = "ProgressSection" });
        Steps.Add(new DeployWorkflowStepItem { Number = 5, Title = "Verification", SectionName = "ProgressSection" });

        center.PropertyChanged += OnCenterPropertyChanged;
        center.CatalogCardsChanged += OnCatalogCardsChanged;
        center.ConsoleEntries.CollectionChanged += (_, _) => ApplyConsoleFilter();
        HookCatalogSelectionHandlers();
        RecomputeWorkflowState();
        ApplyConsoleFilter();
    }

    public event Action<string>? SectionRequested;

    public IEnumerable<DeploymentCatalogCardItem> SelectedReleases =>
        Center.CatalogCards.Where(card => card.IsSelected);

    public int SelectedServiceCount => SelectedReleases.Count();

    public bool HasServiceSelection => SelectedServiceCount > 0;

    public string SelectionSummary => SelectedServiceCount switch
    {
        0 => "0 selected",
        1 => "1 selected",
        _ => string.Create(CultureInfo.InvariantCulture, $"{SelectedServiceCount} selected")
    };

    public string DeploymentModeLabel
    {
        get
        {
            var mode = PlatformDeploymentScope.InferMode(
                SelectedServiceCount == 0 ? null : SelectedReleases.Select(card => card.ServiceId).ToList());
            return SelectedServiceCount == 0
                ? "Select release packages to deploy"
                : PlatformDeploymentScope.FormatModeLabel(mode);
        }
    }

    public string RequiredDependenciesDisplay
    {
        get
        {
            // Shown after Validate / when manifest is available — empty until then.
            return string.IsNullOrWhiteSpace(requiredDependenciesSummary) ? "—" : requiredDependenciesSummary;
        }
    }

    private string requiredDependenciesSummary = string.Empty;

    public string DeployButtonLabel => SelectedServiceCount switch
    {
        0 => "Deploy Selected Services",
        1 => "Deploy Selected Service",
        _ => string.Create(CultureInfo.InvariantCulture, $"Deploy {SelectedServiceCount} Selected Services")
    };

    public bool CanDeploy => Center.SelectedEnvironment is not null && HasServiceSelection && !Center.IsBusy && !HasBlockers;

    public bool HasBlockers => Blockers.Count > 0;

    public string BlockerSummary => Blockers.Count switch
    {
        0 => string.Empty,
        1 => "Deployment blocked — 1 blocking issue found",
        _ => string.Create(CultureInfo.InvariantCulture, $"Deployment blocked — {Blockers.Count} blocking issues found")
    };

    public string RollbackAvailability => string.IsNullOrWhiteSpace(Center.CurrentPlatformDeploymentId) ? "Not yet" : "Available";

    public string ApprovalStatus => Center.CurrentPlan?.OperatorConfirmationRequired == true
        ? "Operator confirmation required"
        : Center.CurrentPlan is not null
            ? "Ready"
            : "Pending validation";

    public string HeaderRuntimeDisplay => Center.SelectedEnvironment?.Name ?? "Not selected";

    public string HeaderReleaseDisplay
    {
        get
        {
            var selected = SelectedReleases.ToList();
            return selected.Count switch
            {
                0 => "Not selected",
                1 => $"{selected[0].ServiceName} {selected[0].PackageVersion}",
                _ => string.Create(CultureInfo.InvariantCulture, $"{selected.Count} packages selected")
            };
        }
    }

    public string EstimatedTimeDisplay => string.IsNullOrWhiteSpace(Center.AnalyzeEstimatedDuration)
        || Center.AnalyzeEstimatedDuration == "-"
        ? EstimateFromSelection()
        : Center.AnalyzeEstimatedDuration;

    public string EstimatedDowntimeDisplay
    {
        get
        {
            var downtime = Center.SelectedPackage?.Deployment.EstimatedDowntime;
            if (downtime is { TotalSeconds: > 0 })
            {
                return downtime.Value.ToString(@"mm\:ss");
            }

            return SelectedServiceCount == 0 ? "—" : EstimateFromSelection();
        }
    }

    public string RestartRequiredDisplay => SelectedServiceCount == 0
        ? "—"
        : SelectedServiceCount == 1
            ? "Service restart"
            : "Rolling service restarts";

    public string ConnectedDisplay =>
        Center.RuntimeConnectionStatus.Contains("Connected", StringComparison.OrdinalIgnoreCase)
            ? "Connected"
            : Center.SelectedEnvironment is null
                ? "Not connected"
                : Center.RuntimeConnectionStatus;

    [RelayCommand]
    private void SelectAllReleases()
    {
        foreach (var card in Center.FilteredCatalogCards)
        {
            card.IsSelected = true;
        }

        Center.RefreshCatalogFilterView();
    }

    [RelayCommand]
    private void ClearReleaseSelection()
    {
        foreach (var card in Center.CatalogCards)
        {
            card.IsSelected = false;
        }

        Center.SelectedCatalogCard = null;
        Center.SelectedPackagePath = string.Empty;
        Center.SelectedReleaseId = string.Empty;
        Center.RefreshCatalogFilterView();
        OnReleaseSelectionChanged();
    }

    [RelayCommand]
    private void GoToStep(DeployWorkflowStepItem? step)
    {
        if (step is not null)
        {
            SectionRequested?.Invoke(step.SectionName);
        }
    }

    [RelayCommand]
    private async Task ReviewAsync()
    {
        await RefreshRequiredDependenciesAsync();
        SectionRequested?.Invoke("PlanSection");
        if (Center.AnalyzeSelectedCommand.CanExecute(null))
        {
            await Center.AnalyzeSelectedCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    private void CancelWorkflow()
    {
        if (Center.IsBusy)
        {
            Center.StatusMessage = "Deployment in progress — wait for the current session to finish, or use Rollback if it fails.";
            SectionRequested?.Invoke("ProgressSection");
            return;
        }

        ClearReleaseSelection();
        requiredDependenciesSummary = string.Empty;
        OnPropertyChanged(nameof(RequiredDependenciesDisplay));
        Center.StatusMessage = "Selection cleared. Choose a runtime and release packages to continue.";
        SectionRequested?.Invoke("RuntimeSection");
    }

    [RelayCommand]
    private async Task DeploySelectedAsync()
    {
        var selected = SelectedReleases.ToList();
        // Prefer package filename (gateway.aovdeploy → gateway) so ids match solution-deployment.json.
        var selectedIds = selected
            .Select(ResolveCatalogServiceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        await RefreshRequiredDependenciesAsync();

        // Operator selection only — Runtime Agent is bootstrap infrastructure, never a deploy package.
        Center.SelectedServiceIds = selectedIds;
        Center.DeploymentMode = selectedIds.Count <= 1
            ? DeploymentWorkspaceMode.SingleService
            : DeploymentWorkspaceMode.EntirePlatform;

        var mode = PlatformDeploymentScope.InferMode(selectedIds);
        Center.StatusMessage =
            $"Deployment Mode: {PlatformDeploymentScope.FormatModeLabel(mode)}; " +
            $"Services: {string.Join(", ", selectedIds)}; Packages: {selectedIds.Count}.";

        var focus = Center.SelectedCatalogCard is { IsSelected: true } card
            ? card
            : selected.FirstOrDefault();
        if (focus is not null && !ReferenceEquals(Center.SelectedCatalogCard, focus))
        {
            await Center.SelectCatalogCardCommand.ExecuteAsync(focus);
        }

        // Always use the scoped platform orchestrator (single-service → package upload API).
        // Do not call DeployCommand — that routes SingleService through the local release workflow.
        await Center.DeployPlatformCommand.ExecuteAsync(null);
        if (!Center.LastOperationFailed)
        {
            SectionRequested?.Invoke("ProgressSection");
        }
    }

    private static string ResolveCatalogServiceId(DeploymentCatalogCardItem card)
    {
        if (!string.IsNullOrWhiteSpace(card.PackagePath))
        {
            var fromFile = Path.GetFileNameWithoutExtension(card.PackagePath);
            if (!string.IsNullOrWhiteSpace(fromFile))
            {
                return fromFile;
            }
        }

        return string.IsNullOrWhiteSpace(card.ServiceId) ? card.ServiceName : card.ServiceId;
    }

    private async Task RefreshRequiredDependenciesAsync()
    {
        try
        {
            if (SelectedServiceCount == 0 || string.IsNullOrWhiteSpace(Center.ReleaseLibraryPath))
            {
                requiredDependenciesSummary = string.Empty;
                OnPropertyChanged(nameof(RequiredDependenciesDisplay));
                return;
            }

            var manifest = await Center.LoadManifestForSelectionAsync();
            if (manifest is null)
            {
                requiredDependenciesSummary = string.Empty;
                OnPropertyChanged(nameof(RequiredDependenciesDisplay));
                return;
            }

            var selectedIds = SelectedReleases.Select(card => card.ServiceId).ToList();
            var required = PlatformDeploymentScope.GetRequiredApplicationDependencies(manifest, selectedIds);
            requiredDependenciesSummary = required.Count == 0
                ? "None"
                : string.Join(", ", required);
            OnPropertyChanged(nameof(RequiredDependenciesDisplay));
        }
        catch
        {
            requiredDependenciesSummary = string.Empty;
            OnPropertyChanged(nameof(RequiredDependenciesDisplay));
        }
    }

    /// <summary>Checkbox or card click — toggle selection and focus package details.</summary>
    [RelayCommand]
    private async Task ToggleReleaseSelectionAsync(DeploymentCatalogCardItem? card)
    {
        if (card is null)
        {
            return;
        }

        card.IsSelected = !card.IsSelected;
        if (card.IsSelected)
        {
            await Center.SelectCatalogCardCommand.ExecuteAsync(card);
        }
        else if (ReferenceEquals(Center.SelectedCatalogCard, card))
        {
            var next = SelectedReleases.FirstOrDefault();
            if (next is not null)
            {
                await Center.SelectCatalogCardCommand.ExecuteAsync(next);
            }
            else
            {
                Center.SelectedCatalogCard = null;
                Center.SelectedPackagePath = string.Empty;
                Center.SelectedReleaseId = string.Empty;
            }
        }

        Center.RefreshCatalogFilterView();
    }

    partial void OnConsoleSearchTextChanged(string value) => ApplyConsoleFilter();

    partial void OnConsoleLevelFilterChanged(string value) => ApplyConsoleFilter();

    private void OnCatalogCardsChanged()
    {
        HookCatalogSelectionHandlers();
        OnReleaseSelectionChanged();
    }

    private void HookCatalogSelectionHandlers()
    {
        foreach (var card in Center.CatalogCards)
        {
            card.SelectionChanged -= OnReleaseSelectionChanged;
            card.SelectionChanged += OnReleaseSelectionChanged;
        }
    }

    private void OnReleaseSelectionChanged()
    {
        if (isSyncingReleaseFocus)
        {
            return;
        }

        OnPropertyChanged(nameof(SelectedServiceCount));
        OnPropertyChanged(nameof(HasServiceSelection));
        OnPropertyChanged(nameof(SelectionSummary));
        OnPropertyChanged(nameof(DeploymentModeLabel));
        OnPropertyChanged(nameof(DeployButtonLabel));
        OnPropertyChanged(nameof(EstimatedTimeDisplay));
        OnPropertyChanged(nameof(EstimatedDowntimeDisplay));
        OnPropertyChanged(nameof(RestartRequiredDisplay));
        OnPropertyChanged(nameof(HeaderReleaseDisplay));
        QueueRecomputeWorkflowState();

        // Keep package details focused on a selected card without fighting the checkbox binding.
        if (Center.SelectedCatalogCard is { IsSelected: false } or null)
        {
            var next = SelectedReleases.FirstOrDefault();
            if (next is not null && !isSyncingReleaseFocus)
            {
                isSyncingReleaseFocus = true;
                _ = SyncFocusAsync(next);
            }
        }
    }

    private async Task SyncFocusAsync(DeploymentCatalogCardItem card)
    {
        try
        {
            await Center.SelectCatalogCardCommand.ExecuteAsync(card);
        }
        finally
        {
            isSyncingReleaseFocus = false;
        }
    }

    private void OnCenterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PlatformDeploymentCenterViewModel.SelectedEnvironment)
            or nameof(PlatformDeploymentCenterViewModel.SelectedCatalogCard)
            or nameof(PlatformDeploymentCenterViewModel.SelectedReleaseId)
            or nameof(PlatformDeploymentCenterViewModel.SelectedPackage)
            or nameof(PlatformDeploymentCenterViewModel.LocalValidation)
            or nameof(PlatformDeploymentCenterViewModel.CurrentPlan)
            or nameof(PlatformDeploymentCenterViewModel.SelectedRelease)
            or nameof(PlatformDeploymentCenterViewModel.RemoteValidationSucceeded)
            or nameof(PlatformDeploymentCenterViewModel.LastOperationFailed)
            or nameof(PlatformDeploymentCenterViewModel.AnalyzeRiskScore)
            or nameof(PlatformDeploymentCenterViewModel.AnalyzeEstimatedDuration)
            or nameof(PlatformDeploymentCenterViewModel.RuntimeConnectionStatus)
            or nameof(PlatformDeploymentCenterViewModel.DeploymentApiStatus)
            or nameof(PlatformDeploymentCenterViewModel.DockerStatus)
            or nameof(PlatformDeploymentCenterViewModel.ConfigurationStatus)
            or nameof(PlatformDeploymentCenterViewModel.PathPolicyStatus)
            or nameof(PlatformDeploymentCenterViewModel.CurrentPlatformDeploymentId)
            or nameof(PlatformDeploymentCenterViewModel.OverallProgressPercent)
            or nameof(PlatformDeploymentCenterViewModel.CertificationStatus)
            or nameof(PlatformDeploymentCenterViewModel.CurrentPhase)
            or nameof(PlatformDeploymentCenterViewModel.IsBusy)
            or nameof(PlatformDeploymentCenterViewModel.AgentVersion)
            or nameof(PlatformDeploymentCenterViewModel.OperatingSystem)
            or nameof(PlatformDeploymentCenterViewModel.SignalRStatus)
            or nameof(PlatformDeploymentCenterViewModel.PlatformHealthStatus))
        {
            QueueRecomputeWorkflowState();
        }
    }

    private void QueueRecomputeWorkflowState()
    {
        if (recomputeQueued)
        {
            return;
        }

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            RecomputeWorkflowState();
            return;
        }

        recomputeQueued = true;
        _ = dispatcher.BeginInvoke(static (DeploymentsWorkspaceViewModel vm) =>
        {
            vm.recomputeQueued = false;
            vm.RecomputeWorkflowState();
        }, System.Windows.Threading.DispatcherPriority.Background, this);
    }

    private void RecomputeWorkflowState()
    {
        if (isRecomputing)
        {
            recomputeQueued = true;
            return;
        }

        isRecomputing = true;
        try
        {
            var runtimeSelected = Center.SelectedEnvironment is not null;
            var packagesSelected = HasServiceSelection;
            var validated = Center.RemoteValidationSucceeded
                && Center.SelectedRelease?.State is ReleaseState.Staged or ReleaseState.Compatible;
            var deployStarted = !string.IsNullOrWhiteSpace(Center.CurrentPlatformDeploymentId);
            var deployComplete = deployStarted && Center.OverallProgressPercent >= 100;
            var verified = string.Equals(Center.CertificationStatus, "Certified", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Center.CertificationStatus, "PASS", StringComparison.OrdinalIgnoreCase);

            RecomputePlanChecks(runtimeSelected, packagesSelected);
            RecomputeBlockers();

            Steps[0].IsComplete = runtimeSelected;
            Steps[1].IsComplete = packagesSelected;
            Steps[2].IsComplete = validated && !HasBlockers;
            Steps[3].IsComplete = deployComplete;
            Steps[4].IsComplete = verified;

            var activeIndex = !runtimeSelected ? 0
                : !packagesSelected ? 1
                : !validated || HasBlockers ? 2
                : !deployComplete ? 3
                : 4;
            for (var i = 0; i < Steps.Count; i++)
            {
                Steps[i].IsActive = i == activeIndex;
            }

            OnPropertyChanged(nameof(HasBlockers));
            OnPropertyChanged(nameof(BlockerSummary));
            OnPropertyChanged(nameof(CanDeploy));
            OnPropertyChanged(nameof(RollbackAvailability));
            OnPropertyChanged(nameof(ApprovalStatus));
            OnPropertyChanged(nameof(HeaderRuntimeDisplay));
            OnPropertyChanged(nameof(HeaderReleaseDisplay));
            OnPropertyChanged(nameof(EstimatedTimeDisplay));
            OnPropertyChanged(nameof(EstimatedDowntimeDisplay));
            OnPropertyChanged(nameof(ConnectedDisplay));
            OnPropertyChanged(nameof(DeployButtonLabel));
            OnPropertyChanged(nameof(SelectionSummary));
            OnPropertyChanged(nameof(DeploymentModeLabel));
            OnPropertyChanged(nameof(HasServiceSelection));
            OnPropertyChanged(nameof(SelectedServiceCount));
            OnPropertyChanged(nameof(RestartRequiredDisplay));
        }
        finally
        {
            isRecomputing = false;
            if (recomputeQueued)
            {
                recomputeQueued = false;
                QueueRecomputeWorkflowState();
            }
        }
    }

    private void RecomputePlanChecks(bool runtimeSelected, bool packagesSelected)
    {
        PlanChecks.Clear();

        var runtimeHealthy = runtimeSelected
            && Center.RuntimeConnectionStatus.Contains("Connected", StringComparison.OrdinalIgnoreCase);
        PlanChecks.Add(Check(
            "Runtime Healthy",
            runtimeHealthy ? "Passed" : runtimeSelected ? "Failed" : "Pending",
            runtimeSelected ? $"{Center.SelectedEnvironment?.Name} — {Center.RuntimeConnectionStatus}" : "Choose a target runtime above.",
            "Deployment cannot reach the host.",
            "Select a reachable runtime and refresh connection status."));

        var releaseValid = packagesSelected
            && Center.RemoteValidationSucceeded
            && Center.SelectedRelease?.State is ReleaseState.Staged or ReleaseState.Compatible
            && (Center.LocalValidation is null || Center.LocalValidation.IsValid)
            && (Center.CurrentPlan is null || Center.CurrentPlan.CompatibilityPassed);
        PlanChecks.Add(Check(
            "Release Valid",
            !packagesSelected ? "Pending" : releaseValid ? "Passed" : "Failed",
            !packagesSelected
                ? "Check one or more release packages above."
                : HeaderReleaseDisplay,
            "An invalid release will fail on the runtime.",
            "Re-publish the package, refresh the library, and select the corrected release."));

        var depsOk = Center.CurrentPlan?.DependenciesValidated
            ?? (Center.SelectedCatalogCard is not null && !string.IsNullOrWhiteSpace(Center.SelectedCatalogCard.DependenciesDisplay));
        PlanChecks.Add(Check(
            "Dependencies",
            !packagesSelected ? "Pending" : depsOk ? "Passed" : "Failed",
            Center.CurrentPlan is not null
                ? string.Join("; ", Center.CurrentPlan.DependencyMessages.Take(3))
                : Center.SelectedCatalogCard?.DependenciesDisplay ?? "Run Validate to inspect dependencies.",
            "Missing dependencies block startup order.",
            "Publish required dependency packages first."));

        var dockerOk = Center.DockerStatus.Contains("PASS", StringComparison.OrdinalIgnoreCase)
            || Center.DockerStatus.Contains("Available", StringComparison.OrdinalIgnoreCase);
        PlanChecks.Add(Check(
            "Docker Images",
            string.Equals(Center.DockerStatus, "Unknown", StringComparison.OrdinalIgnoreCase) ? "Pending"
                : dockerOk ? "Passed" : "Failed",
            $"Docker daemon: {Center.DockerStatus}",
            "Containers cannot be created without Docker.",
            "Start the Docker daemon on the target host."));

        var configOk = Center.ConfigurationStatus.Contains("PASS", StringComparison.OrdinalIgnoreCase);
        PlanChecks.Add(Check(
            "Configuration",
            string.Equals(Center.ConfigurationStatus, "Unknown", StringComparison.OrdinalIgnoreCase) ? "Pending"
                : configOk ? "Passed" : "Failed",
            Center.ConfigurationStatus,
            "Misconfiguration prevents service startup.",
            "Verify runtime configuration and path policy."));

        var pathOk = Center.PathPolicyStatus.Contains("PASS", StringComparison.OrdinalIgnoreCase);
        PlanChecks.Add(Check(
            "Disk",
            string.Equals(Center.PathPolicyStatus, "Unknown", StringComparison.OrdinalIgnoreCase) ? "Pending"
                : pathOk ? "Passed" : "Failed",
            $"Path policy: {Center.PathPolicyStatus}",
            "Insufficient or non-compliant disk paths block extraction.",
            "Free disk space and align runtime path policy."));

        PlanChecks.Add(Check(
            "Ports",
            !packagesSelected ? "Pending" : "Passed",
            packagesSelected ? $"{SelectionSummary} — port bindings validated at deploy time." : "Select release packages to include port checks.",
            "Port conflicts stop container publish.",
            "Stop conflicting listeners or change service ports."));

        PlanChecks.Add(Check(
            "Volumes",
            !packagesSelected ? "Pending" : "Passed",
            packagesSelected ? "Volume mounts follow the selected package contract." : "Select a release to inspect volume requirements.",
            "Missing volumes lose persistent data mounts.",
            "Ensure required directories exist on the runtime host."));

        var networkOk = Center.SignalRStatus.Contains("Connected", StringComparison.OrdinalIgnoreCase)
            || Center.DeploymentApiStatus.Contains("Available", StringComparison.OrdinalIgnoreCase);
        PlanChecks.Add(Check(
            "Network",
            !runtimeSelected ? "Pending" : networkOk ? "Passed" : "Warning",
            $"API {Center.DeploymentApiStatus} · Live channel {Center.SignalRStatus}",
            "Network issues interrupt live progress and health probes.",
            "Verify firewall rules and the runtime hub endpoint."));

        var healthPending = string.Equals(Center.PlatformHealthStatus, "Unknown", StringComparison.OrdinalIgnoreCase);
        PlanChecks.Add(Check(
            "Health Checks",
            healthPending && !packagesSelected ? "Pending"
                : Center.PlatformHealthStatus.Contains("Fail", StringComparison.OrdinalIgnoreCase) ? "Failed"
                : packagesSelected ? "Passed" : "Pending",
            $"Platform health: {Center.PlatformHealthStatus}",
            "Failed health gates roll the deployment back.",
            "Inspect service health endpoints after deploy."));
    }

    private static DeployPlanCheckItem Check(string label, string status, string detail, string impact, string recovery)
        => new()
        {
            Label = label,
            Status = status,
            Detail = detail,
            Impact = impact,
            Recovery = recovery
        };

    private void RecomputeBlockers()
    {
        Blockers.Clear();

        if (string.Equals(Center.DockerStatus, "Unavailable", StringComparison.OrdinalIgnoreCase)
            || Center.DockerStatus.Contains("Fail", StringComparison.OrdinalIgnoreCase)
            || Center.DockerStatus.Contains("✗", StringComparison.OrdinalIgnoreCase))
        {
            Blockers.Add(new DeployBlockerItem
            {
                Category = "Runtime validation",
                Problem = "Docker daemon unavailable",
                Reason = "The target runtime's Docker daemon did not answer, so no container can be created or replaced.",
                Recovery = "Start the Docker daemon on the target host, then Validate again.",
                FixCommand = "sudo systemctl start docker",
                TechnicalDetails = $"Docker status: {Center.DockerStatus}"
            });
        }

        if (Center.SelectedEnvironment is not null
            && (Center.RuntimeConnectionStatus.Contains("Unavailable", StringComparison.OrdinalIgnoreCase)
                || Center.RuntimeConnectionStatus.Contains("Disconnected", StringComparison.OrdinalIgnoreCase)
                || Center.RuntimeConnectionStatus.Contains("Failed", StringComparison.OrdinalIgnoreCase)
                || Center.RuntimeConnectionStatus.Contains("✗", StringComparison.OrdinalIgnoreCase)))
        {
            Blockers.Add(new DeployBlockerItem
            {
                Category = "Runtime validation",
                Problem = "Runtime unreachable",
                Reason = "The selected runtime's API did not respond — nothing can be deployed to a runtime that cannot be reached.",
                Recovery = "Verify the runtime is running and reachable, select it again, then Validate.",
                TechnicalDetails = $"Deployment API: {Center.DeploymentApiStatus}; SignalR: {Center.SignalRStatus}"
            });
        }

        if (Center.LocalValidation is { IsValid: false } validation)
        {
            Blockers.Add(new DeployBlockerItem
            {
                Category = "Release validation",
                Problem = "Release validation failed",
                Reason = "The selected package did not pass local validation and would fail on the runtime.",
                Recovery = "Re-publish the affected package, refresh the library, and select the corrected release.",
                TechnicalDetails = string.Join(Environment.NewLine, validation.Errors)
            });
        }

        if (Center.CurrentPlan is { CompatibilityPassed: false } plan)
        {
            Blockers.Add(new DeployBlockerItem
            {
                Category = "Release validation",
                Problem = "Release compatibility failed",
                Reason = "The selected release is not compatible with the target runtime profile.",
                Recovery = "Choose a compatible release or update the runtime capability profile.",
                TechnicalDetails = string.Join(Environment.NewLine, plan.CompatibilityMessages)
            });
        }

        if (Center.CurrentPlan is { DependenciesValidated: false } dependencyPlan)
        {
            var dependencyDetail = string.Join("; ", dependencyPlan.DependencyMessages.DefaultIfEmpty("One or more package dependencies are missing."));
            Blockers.Add(new DeployBlockerItem
            {
                Category = "Release validation",
                Problem = "Missing release dependency",
                Reason = dependencyDetail,
                Recovery = "Publish the missing dependency first, then Validate again.",
                TechnicalDetails = string.Join(Environment.NewLine, dependencyPlan.DependencyMessages)
            });
        }
    }

    private void ApplyConsoleFilter()
    {
        FilteredConsoleEntries.Clear();
        foreach (var entry in Center.ConsoleEntries)
        {
            if (!MatchesConsoleLevel(entry))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(ConsoleSearchText)
                && !entry.Summary.Contains(ConsoleSearchText, StringComparison.OrdinalIgnoreCase)
                && !entry.Reason.Contains(ConsoleSearchText, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            FilteredConsoleEntries.Add(entry);
        }
    }

    private bool MatchesConsoleLevel(DeploymentConsoleEntry entry) => ConsoleLevelFilter switch
    {
        "ERROR" => entry.Status.Contains("Fail", StringComparison.OrdinalIgnoreCase) || entry.Status.Contains("Error", StringComparison.OrdinalIgnoreCase),
        "WARN" => entry.Status.Contains("Warn", StringComparison.OrdinalIgnoreCase),
        "SUCCESS" => entry.Status.Contains("Success", StringComparison.OrdinalIgnoreCase) || entry.Status.Contains("Completed", StringComparison.OrdinalIgnoreCase) || entry.Status.Contains("Certified", StringComparison.OrdinalIgnoreCase),
        "INFO" => entry.Status.Contains("Running", StringComparison.OrdinalIgnoreCase) || entry.Status.Contains("Queued", StringComparison.OrdinalIgnoreCase),
        _ => true
    };

    private string EstimateFromSelection()
    {
        if (SelectedServiceCount == 0)
        {
            return "—";
        }

        return TimeSpan.FromMinutes(Math.Max(2, SelectedServiceCount * 3)).ToString(@"mm\:ss");
    }
}
