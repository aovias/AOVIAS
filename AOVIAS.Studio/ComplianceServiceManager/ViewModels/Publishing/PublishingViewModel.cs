using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using Aovias.Publishing.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Publishing;
using ComplianceServiceManager.Navigation;
using ComplianceServiceManager.Services.Presentation;
using Microsoft.Win32;

namespace ComplianceServiceManager.ViewModels.Publishing;

/// <summary>
/// Publishing workspace view model for the AOVIAS Publishing Engine operator flow.
/// </summary>
public sealed partial class PublishingViewModel : ObservableObject
{
    private static readonly string[] WizardStepNames =
    [
        "Select Project",
        "Project Analysis",
        "Publishing Profile",
        "Publish",
        "Publishing Readiness",
        "Release Library"
    ];

    private readonly IPublishingWorkspaceService publishingWorkspaceService;
    private readonly IWorkspaceNavigator workspaceNavigator;
    private CancellationTokenSource? publishCancellation;

    [ObservableProperty]
    private string statusMessage = "Select a project to begin publishing.";

    [ObservableProperty]
    private int currentWizardStep;

    [ObservableProperty]
    private PublishingProjectOption? selectedProject;

    [ObservableProperty]
    private string manualProjectPath = string.Empty;

    [ObservableProperty]
    private ProjectDiscoveryResult? discoveryResult;

    [ObservableProperty]
    private string serviceId = string.Empty;

    [ObservableProperty]
    private string version = "1.0.0";

    [ObservableProperty]
    private string runtimeTarget = string.Empty;

    [ObservableProperty]
    private string outputDirectory = string.Empty;

    [ObservableProperty]
    private string activeProfile = "local";

    [ObservableProperty]
    private bool isPublishing;

    [ObservableProperty]
    private int publishProgressPercent;

    [ObservableProperty]
    private string currentPipelineStep = "Idle";

    [ObservableProperty]
    private string? publishedPackagePath;

    [ObservableProperty]
    private bool validationPassed;

    [ObservableProperty]
    private string validationSummary = "Validation has not run.";

    [ObservableProperty]
    private string? releaseLibraryPath;

    [ObservableProperty]
    private DeploymentReadinessReport? readinessReport;

    [ObservableProperty]
    private int readinessScore;

    [ObservableProperty]
    private string readinessStatus = "Not validated";

    [ObservableProperty]
    private string? validationLogPath;

    public ObservableCollection<ValidationStageResult> ReadinessStages { get; } = [];

    public ObservableCollection<string> ReadinessRecommendations { get; } = [];

    public bool IsDeploymentReady => ReadinessReport?.IsReady == true;

    public bool IsDeploymentCertified => PipelineState?.IsDeploymentCertified == true;

    public bool CanAddToReleaseLibrary =>
        IsDeploymentCertified
        && IsDeploymentReady
        && !string.IsNullOrWhiteSpace(PublishedPackagePath);

    public ObservableCollection<PublishingProjectOption> Projects { get; } = [];

    public ObservableCollection<string> ProgressLog { get; } = [];

    public ObservableCollection<string> GeneratedFiles { get; } = [];

    public ObservableCollection<string> ValidationErrors { get; } = [];

    public ObservableCollection<string> ValidationWarnings { get; } = [];

    public ObservableCollection<ContractValidationDisplayItem> ContractValidationResults { get; } = [];

    public ObservableCollection<string> ContractCapabilities { get; } = [];

    public ObservableCollection<string> ContractDependencies { get; } = [];

    public ObservableCollection<string> ContractSecrets { get; } = [];

    public ObservableCollection<string> ContractFilesystemPaths { get; } = [];

    public ObservableCollection<PublishingStageExecutionResult> PipelineStageResults { get; } = [];

    /// <summary>Operator-facing failure cards for the last solution publish (raw diagnostics live behind each card's details panel).</summary>
    public ObservableCollection<Models.OperatorErrorItem> OperatorErrors { get; } = [];

    [ObservableProperty]
    private string pipelineStatus = "Pipeline has not run.";

    [ObservableProperty]
    private PublishingPipelineState? pipelineState;

    [ObservableProperty]
    private string contractRuntimeCompatibility = "Not evaluated";

    public ObservableCollection<QualityGate> QualityGates { get; } = [];

    public ObservableCollection<PublishingStageTiming> StageTimings { get; } = [];

    [ObservableProperty]
    private string failureDetails = string.Empty;

    [ObservableProperty]
    private bool isSolutionPublishing;

    [ObservableProperty]
    private string solutionPublishStatus = "Publish every discovered service with one command.";

    [ObservableProperty]
    private string preflightStatus = "Run preflight before publishing.";

    [ObservableProperty]
    private bool isPreflightRunning;

    [ObservableProperty]
    private bool preflightReady;

    [ObservableProperty]
    private string? solutionDeploymentManifestPath;

    [ObservableProperty]
    private string? demoDeploymentBundlePath;

    [ObservableProperty]
    private string? solutionSummaryLogPath;

    [ObservableProperty]
    private string deploymentValidationMode = "Balanced";

    [ObservableProperty]
    private string diskUsageSummary = string.Empty;

    public IReadOnlyList<string> DeploymentValidationModes { get; } = ["Fast", "Balanced", "Full"];

    public ObservableCollection<SolutionPublishProjectStatus> SolutionPublishRows { get; } = [];

    // ---------------------------------------------------------------------
    // Operator-controlled semantic versioning (single-service).
    // ---------------------------------------------------------------------

    [ObservableProperty]
    private string buildNumber = string.Empty;

    [ObservableProperty]
    private string revision = string.Empty;

    [ObservableProperty]
    private string suggestedVersion = "1.0.0";

    [ObservableProperty]
    private bool hasExistingVersion;

    [ObservableProperty]
    private string versionExistsMessage = string.Empty;

    public ObservableCollection<string> ExistingVersions { get; } = [];

    /// <summary>The version string actually sent to the publishing engine — <see cref="Version"/> with
    /// optional Revision/Build Number folded in. The Release Library treats this as one opaque string.</summary>
    public string ComposedVersion
    {
        get
        {
            var core = string.IsNullOrWhiteSpace(Version) ? "1.0.0" : Version.Trim();
            if (!string.IsNullOrWhiteSpace(Revision))
            {
                core = $"{core}.{Revision.Trim()}";
            }

            if (!string.IsNullOrWhiteSpace(BuildNumber))
            {
                core = $"{core}+{BuildNumber.Trim()}";
            }

            return core;
        }
    }

    [ObservableProperty]
    private bool showVersionConflict;

    [ObservableProperty]
    private string conflictRequestedVersion = string.Empty;

    [ObservableProperty]
    private string conflictExistingVersion = string.Empty;

    [ObservableProperty]
    private string conflictTechnicalDetails = string.Empty;

    [ObservableProperty]
    private bool showAlreadyPublishedNotice;

    [ObservableProperty]
    private bool showNewVersionPanel;

    [ObservableProperty]
    private string newVersionInput = string.Empty;

    [ObservableProperty]
    private string newVersionSuggestion = string.Empty;

    [ObservableProperty]
    private string newVersionValidationMessage = string.Empty;

    public bool HasNewVersionValidationMessage => !string.IsNullOrWhiteSpace(NewVersionValidationMessage);

    partial void OnNewVersionValidationMessageChanged(string value) => OnPropertyChanged(nameof(HasNewVersionValidationMessage));

    // ---------------------------------------------------------------------
    // Operator-controlled semantic versioning (bulk / Publish All Services).
    // ---------------------------------------------------------------------

    public sealed record ServiceVersionPreviewRow(string ServiceId, string ServiceName, string CurrentVersion, string NextVersion);

    [ObservableProperty]
    private string versionStrategy = "Increment Patch";

    [ObservableProperty]
    private string customVersionPrefix = string.Empty;

    [ObservableProperty]
    private bool isComputingVersionPreview;

    public IReadOnlyList<string> VersionStrategies { get; } =
        ["Keep Existing Versions", "Increment Patch", "Increment Minor", "Increment Major", "Custom Version Prefix"];

    public ObservableCollection<ServiceVersionPreviewRow> VersionPreview { get; } = [];

    // ---------------------------------------------------------------------
    // Bulk "Select Services" dashboard: per-service status/version/issue rows
    // built from real preflight + publish-run data. Never fabricates a
    // Ready/Warning/Blocked verdict before an actual check has produced one.
    // ---------------------------------------------------------------------

    public sealed class BulkServiceRow
    {
        public required string ServiceId { get; init; }
        public required string ServiceName { get; init; }
        public required string Type { get; init; }
        public required string CurrentVersion { get; init; }
        public required string NextVersion { get; init; }
        public required string Status { get; init; } = "Pending";
        public required string Readiness { get; init; } = "–";
        public string Issues { get; init; } = "–";
        public string IssueAction { get; init; } = string.Empty;
        public string RawIssueDetail { get; init; } = string.Empty;
        public bool HasIssueAction => !string.IsNullOrEmpty(IssueAction);
    }

    public sealed class BlockingIssueRow
    {
        public required string Title { get; init; }

        /// <summary>Short, single-line summary safe for the primary list — never a raw stack trace.</summary>
        public required string Detail { get; init; }

        /// <summary>Full raw diagnostic, only ever surfaced via an explicit "View Details" action.</summary>
        public required string FullDetail { get; init; }

        public required string Service { get; init; }
        public string ActionLabel { get; init; } = "View Details";
    }

    [ObservableProperty]
    private int bulkStep;

    [ObservableProperty]
    private string bulkServiceSearchText = string.Empty;

    [ObservableProperty]
    private string bulkStatusFilter = "All";

    [ObservableProperty]
    private bool isBlockingIssuesExpanded = true;

    [ObservableProperty]
    private int bulkReadyCount;

    [ObservableProperty]
    private int bulkWarningCount;

    [ObservableProperty]
    private int bulkBlockedCount;

    [ObservableProperty]
    private int bulkPlatformIssueCount;

    [ObservableProperty]
    private string bulkBannerTitle = "Not yet validated";

    [ObservableProperty]
    private string bulkBannerSubtitle = "Run preflight or publish to check for blocking issues.";

    [ObservableProperty]
    private string bulkBannerSeverity = "Info";

    public IReadOnlyList<string> BulkStatusFilters { get; } = ["All", "Ready", "Warnings", "Blocked"];

    public ObservableCollection<BulkServiceRow> BulkServices { get; } = [];

    public ICollectionView BulkServicesView { get; }

    public ObservableCollection<BlockingIssueRow> BlockingIssues { get; } = [];

    public ObservableCollection<string> PreflightBlockingReasons { get; } = [];

    public bool HasBlockingIssues => BlockingIssues.Count > 0;

    partial void OnBulkServiceSearchTextChanged(string value) => BulkServicesView.Refresh();

    partial void OnBulkStatusFilterChanged(string value) => BulkServicesView.Refresh();

    private bool FilterBulkService(object candidate)
    {
        if (candidate is not BulkServiceRow row)
        {
            return false;
        }

        if (BulkStatusFilter != "All")
        {
            var wanted = BulkStatusFilter switch
            {
                "Ready" => "Ready",
                "Warnings" => "Warning",
                "Blocked" => "Blocked",
                _ => null
            };

            if (wanted is not null && row.Status != wanted)
            {
                return false;
            }
        }

        return string.IsNullOrWhiteSpace(BulkServiceSearchText)
            || row.ServiceName.Contains(BulkServiceSearchText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Rebuilds the Select Services table and blocking-issues list from whatever real data
    /// currently exists (version preview, last preflight result, last/live publish-run rows). Rows
    /// default to "Pending" — a status is only ever Ready/Warning/Blocked once an actual check ran.</summary>
    private void RebuildBulkServiceRows()
    {
        var rows = new List<BulkServiceRow>();
        foreach (var preview in VersionPreview)
        {
            var runRow = SolutionPublishRows.FirstOrDefault(row =>
                string.Equals(row.ServiceId, preview.ServiceId, StringComparison.OrdinalIgnoreCase));

            string status;
            string readiness;
            var issues = "–";
            var issueAction = string.Empty;
            var rawIssueDetail = string.Empty;

            if (runRow is null)
            {
                status = "Pending";
                readiness = "–";
            }
            else if (runRow.Failed)
            {
                status = "Blocked";
                readiness = "Blocked";
                if (!string.IsNullOrWhiteSpace(runRow.ErrorMessage))
                {
                    var interpreted = OperatorErrorInterpreter.Interpret(preview.ServiceId, runRow.ErrorMessage);
                    issues = interpreted.Title;
                    rawIssueDetail = runRow.ErrorMessage;
                    issueAction = interpreted.Category switch
                    {
                        "Release Conflict" => "Open Release Library",
                        "Infrastructure" => "Check Docker",
                        "Build" => "View Build Log",
                        _ => "View Details"
                    };
                }
            }
            else if (runRow.DeploymentReady)
            {
                status = "Ready";
                readiness = "Ready";
            }
            else
            {
                status = "Warning";
                readiness = "In Progress";
                issues = "In progress";
            }

            rows.Add(new BulkServiceRow
            {
                ServiceId = preview.ServiceId,
                ServiceName = preview.ServiceName,
                Type = "Service",
                CurrentVersion = preview.CurrentVersion,
                NextVersion = preview.NextVersion,
                Status = status,
                Readiness = readiness,
                Issues = issues,
                IssueAction = issueAction,
                RawIssueDetail = rawIssueDetail
            });
        }

        BulkServices.Clear();
        foreach (var row in rows.OrderBy(row => row.ServiceName, StringComparer.OrdinalIgnoreCase))
        {
            BulkServices.Add(row);
        }

        BulkReadyCount = rows.Count(row => row.Status == "Ready");
        BulkWarningCount = rows.Count(row => row.Status == "Warning");
        BulkBlockedCount = rows.Count(row => row.Status == "Blocked");
        BulkPlatformIssueCount = PreflightBlockingReasons.Count;

        BlockingIssues.Clear();
        foreach (var reason in PreflightBlockingReasons)
        {
            BlockingIssues.Add(new BlockingIssueRow
            {
                Title = TrimForTitle(reason),
                Detail = TrimForDetail(reason),
                FullDetail = reason,
                Service = "All Services",
                ActionLabel = reason.Contains("docker", StringComparison.OrdinalIgnoreCase) ? "Check Docker" : "View Details"
            });
        }

        foreach (var row in rows.Where(row => row.Status == "Blocked"))
        {
            BlockingIssues.Add(new BlockingIssueRow
            {
                Title = row.Issues,
                Detail = row.Issues,
                FullDetail = string.IsNullOrWhiteSpace(row.RawIssueDetail) ? row.Issues : row.RawIssueDetail,
                Service = row.ServiceName,
                ActionLabel = string.IsNullOrEmpty(row.IssueAction) ? "View Details" : row.IssueAction
            });
        }

        var hasRunAnything = SolutionPublishRows.Count > 0 || PreflightBlockingReasons.Count > 0 || IsSolutionPublishing || PreflightReady;
        if (!hasRunAnything)
        {
            BulkBannerTitle = "Not yet validated";
            BulkBannerSubtitle = "Run preflight or publish to check for blocking issues.";
            BulkBannerSeverity = "Info";
        }
        else if (BulkBlockedCount > 0 || PreflightBlockingReasons.Count > 0)
        {
            BulkBannerTitle = "Publishing is blocked";
            BulkBannerSubtitle = "Fix the blocking issues below to continue.";
            BulkBannerSeverity = "Danger";
        }
        else if (BulkWarningCount > 0)
        {
            BulkBannerTitle = "Publishing has warnings";
            BulkBannerSubtitle = "Review warnings before continuing.";
            BulkBannerSeverity = "Warning";
        }
        else
        {
            BulkBannerTitle = "Ready to publish";
            BulkBannerSubtitle = "All services passed validation.";
            BulkBannerSeverity = "Success";
        }

        OnPropertyChanged(nameof(HasBlockingIssues));
        OnPropertyChanged(nameof(BulkServicesView));
        BulkServicesView.Refresh();
    }

    private static string TrimForTitle(string text)
    {
        var firstLine = text.Split(['\r', '\n'], 2)[0];
        return firstLine.Length <= 70 ? firstLine : firstLine[..70] + "…";
    }

    /// <summary>Single-line, length-capped summary for the Blocking Issues list — the mockup's "do
    /// not show raw stack traces" rule applies to this list, so multi-line diagnostics never render
    /// here in full; the complete text is only ever reachable via the row's "View Details" action.</summary>
    private static string TrimForDetail(string text)
    {
        var firstLine = text.Split(['\r', '\n'], 2)[0].Trim();
        return firstLine.Length <= 160 ? firstLine : firstLine[..160] + "…";
    }

    [RelayCommand]
    private void ExportBulkReport()
    {
        var reportPath = Path.Combine(Path.GetTempPath(), $"aovias-publish-report-{Guid.NewGuid():N}.txt");
        var lines = new List<string>
        {
            $"AOVIAS Publishing Report — {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            string.Empty,
            $"Total: {BulkServices.Count}   Ready: {BulkReadyCount}   Warnings: {BulkWarningCount}   Blocked: {BulkBlockedCount}",
            string.Empty
        };
        lines.AddRange(BulkServices.Select(row =>
            $"{row.ServiceName,-32} {row.Type,-10} {row.CurrentVersion,-10} -> {row.NextVersion,-10} {row.Status,-10} {row.Issues}"));

        File.WriteAllLines(reportPath, lines);
        Process.Start(new ProcessStartInfo(reportPath) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task RefreshBulkServicesAsync() => await RefreshVersionPreviewAsync();

    [RelayCommand]
    private void ContinueBulkStep()
    {
        if (BulkStep == 1 && !PreflightReady)
        {
            StatusMessage = "Run preflight and resolve blocking issues before continuing.";
            return;
        }

        BulkStep = Math.Min(BulkStep + 1, 4);
    }

    [RelayCommand]
    private void PreviousBulkStep() => BulkStep = Math.Max(BulkStep - 1, 0);

    [RelayCommand]
    private void HandleBlockingIssueAction(BlockingIssueRow? issue)
    {
        if (issue is null)
        {
            return;
        }

        if (issue.ActionLabel == "Open Release Library")
        {
            workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceReleaseLibrary);
        }
        else if (issue.ActionLabel == "Check Docker")
        {
            StatusMessage = "Verify Docker Desktop is running and reachable, then Validate Again.";
        }
        else
        {
            // "View Details" / "View Build Log": no dedicated raw-log viewer exists yet, so the
            // full diagnostic (never shown inline, per the "no raw stack traces" rule) goes to the
            // clipboard for the operator to paste wherever they need it.
            try
            {
                System.Windows.Clipboard.SetText(issue.FullDetail);
                StatusMessage = $"Full details for \"{issue.Title}\" copied to clipboard.";
            }
            catch (Exception exception)
            {
                StatusMessage = $"Could not copy details: {exception.Message}";
            }
        }
    }

    [RelayCommand]
    private void SelectBulkStatusFilter(string filter) => BulkStatusFilter = filter;

    [RelayCommand]
    private void OpenSolutionSummaryLog()
    {
        if (string.IsNullOrWhiteSpace(SolutionSummaryLogPath) || !File.Exists(SolutionSummaryLogPath))
        {
            StatusMessage = "No solution publish summary log is available yet.";
            return;
        }

        Process.Start(new ProcessStartInfo(SolutionSummaryLogPath) { UseShellExecute = true });
    }

    public bool CanPublishSolution => !IsPublishing && !IsSolutionPublishing && !IsPreflightRunning && PreflightReady;

    public bool CanRunPreflight => !IsPublishing && !IsSolutionPublishing && !IsPreflightRunning;

    public bool HasFailureDetails => !string.IsNullOrWhiteSpace(FailureDetails);

    partial void OnFailureDetailsChanged(string value) => OnPropertyChanged(nameof(HasFailureDetails));

    public IReadOnlyList<string> WizardSteps => WizardStepNames;

    public bool HasDiscovery => DiscoveryResult is not null;

    public bool CanStartPublish => !IsPublishing && DiscoveryResult is not null;

    // ---------------------------------------------------------------------
    // Pipeline-experience presentation state (UI-only; no engine behavior).
    // ---------------------------------------------------------------------

    /// <summary>File-system facts about the selected project, shown on the analysis step.</summary>
    public sealed class ProjectDetailsInfo
    {
        public string DirectoryPath { get; init; } = string.Empty;
        public string ProjectFile { get; init; } = string.Empty;
        public string TargetFramework { get; init; } = "–";
        public string LastModifiedDisplay { get; init; } = "–";
        public int FileCount { get; init; }
        public int DependencyCount { get; init; }
        public int ConfigurationCount { get; init; }
        public string DiskSizeDisplay { get; init; } = "–";
    }

    /// <summary>A kind-grouped slice of the discovered projects for the Publish All Services panel.</summary>
    public sealed record ServiceGroupInfo(string Kind, IReadOnlyList<PublishingProjectOption> Items)
    {
        public string Header => $"{Kind} ({Items.Count})";
    }

    [ObservableProperty]
    private bool isAllServicesMode;

    public bool IsSingleMode => !IsAllServicesMode;

    [ObservableProperty]
    private string projectSearchText = string.Empty;

    [ObservableProperty]
    private string selectedKindFilter = "All Types";

    public ObservableCollection<string> ProjectKindFilters { get; } = ["All Types"];

    public ICollectionView ProjectsView { get; }

    [ObservableProperty]
    private ProjectDetailsInfo? selectedProjectDetails;

    public ObservableCollection<ServiceGroupInfo> ServiceGroups { get; } = [];

    [ObservableProperty]
    private string lastAnalysisDisplay = "–";

    [ObservableProperty]
    private string lastPublishDisplay = "–";

    [ObservableProperty]
    private string lastReleaseDisplay = "–";

    [ObservableProperty]
    private string consoleSearchText = string.Empty;

    [ObservableProperty]
    private bool autoScrollConsole = true;

    [ObservableProperty]
    private bool isConsoleExpanded = true;

    public ICollectionView ConsoleView { get; }

    public bool HasQualityGates => QualityGates.Count > 0;

    [RelayCommand]
    private void SelectEntryMode(string mode)
    {
        IsAllServicesMode = string.Equals(mode, "All", StringComparison.OrdinalIgnoreCase);
        // Always re-raise both mode flags: clicking the already-active toggle must snap the
        // control's visual state back even when the underlying value did not change.
        OnPropertyChanged(nameof(IsAllServicesMode));
        OnPropertyChanged(nameof(IsSingleMode));
    }

    partial void OnProjectSearchTextChanged(string value) => ProjectsView.Refresh();

    partial void OnSelectedKindFilterChanged(string value) => ProjectsView.Refresh();

    partial void OnConsoleSearchTextChanged(string value) => ConsoleView.Refresh();

    partial void OnVersionChanged(string value)
    {
        OnPropertyChanged(nameof(ComposedVersion));
        UpdateVersionExistsWarning();
    }

    partial void OnRevisionChanged(string value)
    {
        OnPropertyChanged(nameof(ComposedVersion));
        UpdateVersionExistsWarning();
    }

    partial void OnBuildNumberChanged(string value)
    {
        OnPropertyChanged(nameof(ComposedVersion));
        UpdateVersionExistsWarning();
    }

    [RelayCommand]
    private void SelectVersionStrategy(string strategy) => VersionStrategy = strategy;

    partial void OnVersionStrategyChanged(string value) => _ = RefreshVersionPreviewAsync();

    partial void OnIsAllServicesModeChanged(bool value)
    {
        if (value && VersionPreview.Count == 0)
        {
            _ = RefreshVersionPreviewAsync();
        }
    }

    private void UpdateVersionExistsWarning()
    {
        var exists = ExistingVersions.Any(existing => string.Equals(existing, ComposedVersion, StringComparison.OrdinalIgnoreCase));
        HasExistingVersion = exists;
        VersionExistsMessage = exists
            ? $"Version {ComposedVersion} already exists in the Release Library for this service. " +
              "Publishing succeeds only if the content is identical to that release; otherwise it will be blocked as a version conflict."
            : string.Empty;
    }

    /// <summary>Queries the Release Library for this service's publish history and suggests the next
    /// version. Best-effort — a query failure must never block analyzing or publishing a project.
    /// <paramref name="applyAsDefault"/> is false when called just to refresh history after a publish
    /// that already succeeded, so the operator's just-published version stays visible on screen.</summary>
    private async Task RefreshExistingVersionsAsync(bool applyAsDefault = true)
    {
        ExistingVersions.Clear();

        if (string.IsNullOrWhiteSpace(ServiceId))
        {
            SuggestedVersion = "1.0.0";
            if (applyAsDefault)
            {
                Version = SuggestedVersion;
            }

            return;
        }

        try
        {
            var records = await publishingWorkspaceService.GetReleaseVersionsAsync(ServiceId);
            foreach (var record in records.OrderByDescending(record => record.GeneratedUtc))
            {
                ExistingVersions.Add(record.Version);
            }
        }
        catch
        {
            // Release Library query is presentation help only; fall through to project-detected version.
        }

        var latest = SemanticVersionHelper.FindLatest(ExistingVersions);
        SuggestedVersion = latest is not null
            ? SemanticVersionHelper.Bump(latest, SemanticVersionHelper.VersionPart.Patch)
            : "1.0.0";

        if (applyAsDefault)
        {
            Version = SuggestedVersion;
        }
        else
        {
            UpdateVersionExistsWarning();
        }
    }

    [RelayCommand]
    private async Task RefreshVersionPreviewAsync()
    {
        if (IsComputingVersionPreview)
        {
            return;
        }

        IsComputingVersionPreview = true;
        try
        {
            var deployables = await publishingWorkspaceService.DiscoverDeployableProjectsAsync();
            var rows = new List<ServiceVersionPreviewRow>();

            foreach (var project in deployables)
            {
                var current = project.Version;
                try
                {
                    var records = await publishingWorkspaceService.GetReleaseVersionsAsync(project.ServiceId);
                    current = SemanticVersionHelper.FindLatest(records.Select(record => record.Version)) ?? project.Version;
                }
                catch
                {
                    // Fall back to the project's own discovered version when the library can't be read.
                }

                var next = VersionStrategy switch
                {
                    "Keep Existing Versions" => current,
                    "Increment Minor" => SemanticVersionHelper.Bump(current, SemanticVersionHelper.VersionPart.Minor),
                    "Increment Major" => SemanticVersionHelper.Bump(current, SemanticVersionHelper.VersionPart.Major),
                    "Custom Version Prefix" => string.IsNullOrWhiteSpace(CustomVersionPrefix) ? current : CustomVersionPrefix.Trim(),
                    _ => SemanticVersionHelper.Bump(current, SemanticVersionHelper.VersionPart.Patch)
                };

                rows.Add(new ServiceVersionPreviewRow(project.ServiceId, project.Name, current, next));
            }

            VersionPreview.Clear();
            foreach (var row in rows.OrderBy(row => row.ServiceName, StringComparer.OrdinalIgnoreCase))
            {
                VersionPreview.Add(row);
            }

            RebuildBulkServiceRows();
        }
        finally
        {
            IsComputingVersionPreview = false;
        }
    }

    private bool FilterProject(object candidate)
    {
        if (candidate is not PublishingProjectOption project)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(SelectedKindFilter)
            && SelectedKindFilter != "All Types"
            && !string.Equals(project.Kind, SelectedKindFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(ProjectSearchText)
            || project.Name.Contains(ProjectSearchText, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterConsoleLine(object candidate)
        => candidate is not string line
            ? false
            : string.IsNullOrWhiteSpace(ConsoleSearchText)
                || line.Contains(ConsoleSearchText, StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private void ClearConsole() => ProgressLog.Clear();

    [RelayCommand]
    private void OpenPackagesFolder() => OpenFolder(OutputDirectory);

    [RelayCommand]
    private void OpenReleaseLibraryFolder() => OpenFolder(ReleaseLibraryPath);

    private void OpenFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var target = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(target) && Directory.Exists(target))
            {
                Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
            }
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not open folder: {exception.Message}";
        }
    }

    private void RebuildProjectPresentation()
    {
        var kinds = Projects.Select(project => project.Kind).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(kind => kind).ToList();
        ProjectKindFilters.Clear();
        ProjectKindFilters.Add("All Types");
        foreach (var kind in kinds)
        {
            ProjectKindFilters.Add(kind);
        }

        // Clearing the filter list detaches the ComboBox selection (WPF pushes null back into
        // SelectedKindFilter), which would silently filter out every project — restore the default.
        if (string.IsNullOrEmpty(SelectedKindFilter) || !ProjectKindFilters.Contains(SelectedKindFilter))
        {
            SelectedKindFilter = "All Types";
        }

        ServiceGroups.Clear();
        foreach (var group in Projects.GroupBy(project => project.Kind, StringComparer.OrdinalIgnoreCase).OrderBy(group => group.Key))
        {
            ServiceGroups.Add(new ServiceGroupInfo(group.Key, group.ToList()));
        }

        OnPropertyChanged(nameof(ProjectsView));
        ProjectsView.Refresh();
    }

    private async Task LoadProjectDetailsAsync(PublishingProjectOption project)
    {
        SelectedProjectDetails = await Task.Run(() =>
        {
            try
            {
                var directory = Path.GetDirectoryName(project.ProjectPath) ?? string.Empty;
                if (!Directory.Exists(directory))
                {
                    return new ProjectDetailsInfo { DirectoryPath = directory, ProjectFile = Path.GetFileName(project.ProjectPath) };
                }

                var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                    .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !path.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                long totalBytes = 0;
                foreach (var file in files)
                {
                    try { totalBytes += new FileInfo(file).Length; } catch { /* transient file */ }
                }

                var dependencyCount = 0;
                var targetFramework = "–";
                if (project.ProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && File.Exists(project.ProjectPath))
                {
                    var content = File.ReadAllText(project.ProjectPath);
                    dependencyCount = System.Text.RegularExpressions.Regex.Matches(content, "<PackageReference").Count;
                    var match = System.Text.RegularExpressions.Regex.Match(content, "<TargetFramework>([^<]+)</TargetFramework>");
                    if (match.Success)
                    {
                        targetFramework = match.Groups[1].Value;
                    }
                }
                else if (project.ProjectPath.EndsWith("package.json", StringComparison.OrdinalIgnoreCase) && File.Exists(project.ProjectPath))
                {
                    var content = File.ReadAllText(project.ProjectPath);
                    dependencyCount = System.Text.RegularExpressions.Regex.Matches(content, "\":\\s*\"\\^?~?\\d").Count;
                    targetFramework = "Node / Angular";
                }

                var dockerfilePresent = File.Exists(Path.Combine(directory, "Dockerfile"));
                return new ProjectDetailsInfo
                {
                    DirectoryPath = directory,
                    ProjectFile = Path.GetFileName(project.ProjectPath),
                    TargetFramework = targetFramework,
                    LastModifiedDisplay = File.Exists(project.ProjectPath)
                        ? File.GetLastWriteTime(project.ProjectPath).ToString("MM-dd-yyyy HH:mm")
                        : "–",
                    FileCount = files.Count,
                    DependencyCount = dependencyCount,
                    ConfigurationCount = dockerfilePresent ? 3 : 2,
                    DiskSizeDisplay = totalBytes >= 1024 * 1024
                        ? $"{totalBytes / (1024.0 * 1024.0):F0} MB"
                        : $"{totalBytes / 1024.0:F0} KB"
                };
            }
            catch
            {
                return new ProjectDetailsInfo { ProjectFile = Path.GetFileName(project.ProjectPath) };
            }
        });
    }

    public PublishingViewModel(
        IPublishingWorkspaceService publishingWorkspaceService,
        IWorkspaceNavigator workspaceNavigator)
    {
        this.publishingWorkspaceService = publishingWorkspaceService;
        this.workspaceNavigator = workspaceNavigator;

        ProjectsView = CollectionViewSource.GetDefaultView(Projects);
        ProjectsView.Filter = FilterProject;
        ConsoleView = new CollectionViewSource { Source = ProgressLog }.View;
        ConsoleView.Filter = FilterConsoleLine;
        QualityGates.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasQualityGates));
        BulkServicesView = CollectionViewSource.GetDefaultView(BulkServices);
        BulkServicesView.Filter = FilterBulkService;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        RuntimeTarget = publishingWorkspaceService.GetDefaultRuntimeTarget();
        OutputDirectory = publishingWorkspaceService.GetDefaultOutputDirectory();
        ActiveProfile = publishingWorkspaceService.GetActiveProfile();
        ReleaseLibraryPath = publishingWorkspaceService.GetReleaseLibraryPath();

        Projects.Clear();
        foreach (var project in await publishingWorkspaceService.DiscoverLocalProjectsAsync())
        {
            Projects.Add(project);
        }

        RebuildProjectPresentation();
        StatusMessage = $"{Projects.Count} project(s) discovered. Select a project or browse manually.";
    }

    [RelayCommand]
    private async Task SelectProjectAsync(PublishingProjectOption? project)
    {
        if (project is null)
        {
            return;
        }

        SelectedProject = project;
        ManualProjectPath = project.DiscoveryPath;
        _ = LoadProjectDetailsAsync(project);
        await AnalyzeProjectAsync();
    }

    [RelayCommand]
    private async Task BrowseProjectAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select project file",
            Filter = "Projects (*.csproj;package.json;angular.json)|*.csproj;package.json;angular.json|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ManualProjectPath = dialog.FileName;
        SelectedProject = null;
        await AnalyzeProjectAsync();
    }

    [RelayCommand]
    private async Task AnalyzeProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(ManualProjectPath))
        {
            StatusMessage = "Choose a project before continuing.";
            return;
        }

        var discoveryPath = ResolveDiscoveryPath(ManualProjectPath);
        DiscoveryResult = await publishingWorkspaceService.AnalyzeProjectAsync(discoveryPath);
        if (DiscoveryResult is null)
        {
            StatusMessage = "Unable to analyze the selected project.";
            CurrentWizardStep = 0;
            return;
        }

        ServiceId = DiscoveryResult.ServiceId;
        BuildNumber = string.Empty;
        Revision = string.Empty;
        await RefreshExistingVersionsAsync();
        CurrentWizardStep = 1;
        LastAnalysisDisplay = DateTime.Now.ToString("HH:mm:ss");
        StatusMessage = $"Detected {DiscoveryResult.ProjectType} project via {DiscoveryResult.ProviderName}.";
    }

    [RelayCommand]
    private void ContinueToProfile()
    {
        if (DiscoveryResult is null)
        {
            StatusMessage = "Analyze a project first.";
            return;
        }

        CurrentWizardStep = 2;
        StatusMessage = "Review the publishing profile before starting the pipeline.";
    }

    [RelayCommand]
    private void ContinueToPublish()
    {
        if (string.IsNullOrWhiteSpace(ServiceId) || string.IsNullOrWhiteSpace(Version))
        {
            StatusMessage = "Service ID and version are required.";
            return;
        }

        CurrentWizardStep = 3;
        StatusMessage = "Ready to publish the deployment package.";
    }

    [RelayCommand]
    private async Task RunPreflightAsync()
    {
        if (IsPublishing || IsSolutionPublishing || IsPreflightRunning)
        {
            return;
        }

        publishCancellation?.Cancel();
        publishCancellation = new CancellationTokenSource();
        IsPreflightRunning = true;
        PreflightReady = false;
        PreflightStatus = "Running publish preflight...";
        FailureDetails = string.Empty;
        ProgressLog.Clear();

        try
        {
            var result = await publishingWorkspaceService.RunSolutionPreflightAsync(
                new SolutionPublishingPreflightRequest
                {
                    Profile = string.IsNullOrWhiteSpace(ActiveProfile) ? "local" : ActiveProfile,
                    Version = Version,
                    OutputDirectory = OutputDirectory,
                    DeploymentValidationMode = DeploymentValidationMode
                },
                new Progress<SolutionPublishingPreflightProgress>(update =>
                {
                    PreflightStatus = $"[{update.Category}] {update.Message}";
                    ProgressLog.Add(update.Message);
                }),
                publishCancellation.Token);

            PreflightReady = result.IsReady;
            PreflightStatus = $"{result.Verdict}: {result.Summary}";
            StatusMessage = PreflightStatus;

            foreach (var check in result.Checks)
            {
                ProgressLog.Add($"{(check.Passed ? "PASS" : "FAIL")} {check.Category}");
                foreach (var detail in check.Details)
                {
                    ProgressLog.Add($"  {detail}");
                }
            }

            PreflightBlockingReasons.Clear();
            foreach (var reason in result.BlockingReasons)
            {
                PreflightBlockingReasons.Add(reason);
            }

            if (result.BlockingReasons.Count > 0)
            {
                FailureDetails = string.Join(Environment.NewLine, result.BlockingReasons);
            }

            RebuildBulkServiceRows();
        }
        catch (OperationCanceledException)
        {
            PreflightStatus = "Preflight cancelled.";
            StatusMessage = PreflightStatus;
        }
        finally
        {
            IsPreflightRunning = false;
            OnPropertyChanged(nameof(CanPublishSolution));
        }
    }

    [RelayCommand]
    private async Task PublishSolutionAsync()
    {
        if (IsPublishing || IsSolutionPublishing)
        {
            return;
        }

        if (!PreflightReady)
        {
            SolutionPublishStatus = "Run preflight and resolve blocking issues before publishing.";
            return;
        }

        publishCancellation?.Cancel();
        publishCancellation = new CancellationTokenSource();
        IsSolutionPublishing = true;
        SolutionPublishRows.Clear();
        SolutionDeploymentManifestPath = null;
        DemoDeploymentBundlePath = null;
        SolutionSummaryLogPath = null;
        FailureDetails = string.Empty;
        OperatorErrors.Clear();
        ProgressLog.Clear();
        BulkStep = 2;
        SolutionPublishStatus = "Discovering deployable projects...";

        try
        {
            var deployables = await publishingWorkspaceService.DiscoverDeployableProjectsAsync(publishCancellation.Token);
            foreach (var project in deployables)
            {
                SolutionPublishRows.Add(new SolutionPublishProjectStatus
                {
                    ProjectName = project.Name,
                    ServiceId = project.ServiceId,
                    CurrentStep = "Queued",
                    PercentComplete = 0
                });
            }

            if (VersionPreview.Count == 0)
            {
                await RefreshVersionPreviewAsync();
            }

            var versionOverrides = VersionPreview.ToDictionary(
                row => row.ServiceId,
                row => row.NextVersion,
                StringComparer.OrdinalIgnoreCase);

            var progress = new Progress<SolutionPublishingProgress>(UpdateSolutionProgress);
            var result = await publishingWorkspaceService.PublishSolutionAsync(
                new SolutionPublishingRequest
                {
                    Profile = string.IsNullOrWhiteSpace(ActiveProfile) ? "local" : ActiveProfile,
                    Version = Version,
                    OutputDirectory = OutputDirectory,
                    Publisher = Environment.UserName,
                    GenerateDemoBundle = true,
                    OrganizeReleaseLibrary = true,
                    DeploymentValidationMode = DeploymentValidationMode,
                    ServiceVersionOverrides = versionOverrides.Count > 0 ? versionOverrides : null
                },
                progress,
                publishCancellation.Token);

            SolutionDeploymentManifestPath = result.SolutionDeploymentManifestPath;
            DemoDeploymentBundlePath = result.DemoDeploymentBundlePath;
            SolutionSummaryLogPath = result.SummaryLogPath;
            DiskUsageSummary = FormatDiskUsage(result.ResourceSnapshot);
            ReleaseLibraryPath = publishingWorkspaceService.GetReleaseLibraryPath();
            SolutionPublishStatus = result.Message;
            StatusMessage = result.Message;
            BulkStep = result.DeploymentReadyCount == deployables.Count ? 4 : 3;
            ValidationPassed = result.DeploymentReadyCount == deployables.Count && result.FailedCount == 0;

            if (result.FailedCount > 0)
            {
                // Raw dump retained for log files; the UI renders categorized operator cards.
                FailureDetails = string.Join(
                    Environment.NewLine,
                    result.ProjectResults
                        .Where(project => !project.IsSuccess)
                        .Select(project => $"{project.Project.ServiceId}: {project.ErrorMessage}"));

                foreach (var failure in result.ProjectResults.Where(project => !project.IsSuccess))
                {
                    OperatorErrors.Add(Services.Presentation.OperatorErrorInterpreter.Interpret(
                        failure.Project.ServiceId,
                        failure.ErrorMessage ?? string.Empty));
                }
            }

            ProgressLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Solution publish complete. Deployment ready: {result.DeploymentReadyCount}/{deployables.Count}.");
            RebuildBulkServiceRows();
        }
        catch (OperationCanceledException)
        {
            SolutionPublishStatus = "Solution publishing was cancelled.";
            StatusMessage = SolutionPublishStatus;
        }
        finally
        {
            IsSolutionPublishing = false;
        }
    }

    [RelayCommand]
    private void CancelSolutionPublish() => publishCancellation?.Cancel();

    private void UpdateSolutionProgress(SolutionPublishingProgress progressUpdate)
    {
        SolutionPublishStatus = progressUpdate.Message;
        PublishProgressPercent = progressUpdate.TotalProjects == 0
            ? 0
            : (int)Math.Round(progressUpdate.CompletedProjects * 100.0 / progressUpdate.TotalProjects);

        if (progressUpdate.ProjectStatus is null)
        {
            return;
        }

        var row = SolutionPublishRows.FirstOrDefault(item =>
            item.ServiceId.Equals(progressUpdate.ProjectStatus.ServiceId, StringComparison.OrdinalIgnoreCase));
        if (row is null)
        {
            SolutionPublishRows.Add(progressUpdate.ProjectStatus);
        }
        else
        {
            var index = SolutionPublishRows.IndexOf(row);
            SolutionPublishRows[index] = progressUpdate.ProjectStatus;
        }

        RebuildBulkServiceRows();
    }

    [RelayCommand]
    private async Task PublishAsync()
    {
        if (DiscoveryResult is null || IsPublishing)
        {
            return;
        }

        publishCancellation?.Cancel();
        publishCancellation = new CancellationTokenSource();
        IsPublishing = true;
        PublishProgressPercent = 0;
        CurrentPipelineStep = "Starting";
        ProgressLog.Clear();
        GeneratedFiles.Clear();
        ValidationErrors.Clear();
        ValidationWarnings.Clear();
        ReadinessStages.Clear();
        ReadinessRecommendations.Clear();
        QualityGates.Clear();
        StageTimings.Clear();
        FailureDetails = string.Empty;
        ReadinessReport = null;
        ReadinessScore = 0;
        ReadinessStatus = "Not validated";
        ValidationLogPath = null;
        PublishedPackagePath = null;
        ValidationPassed = false;
        PipelineState = null;
        PipelineStageResults.Clear();
        ShowVersionConflict = false;
        ShowAlreadyPublishedNotice = false;
        ShowNewVersionPanel = false;
        NewVersionValidationMessage = string.Empty;

        // Captured before the run: whether the version being published already existed in the
        // Release Library. The pipeline itself decides same-content-idempotent vs different-content-
        // conflict; this flag only tells us which of those two outcomes to explain afterward.
        var wasPreExisting = HasExistingVersion;
        var requestedVersion = ComposedVersion;

        try
        {
            var progress = new Progress<PublishingProgress>(UpdateProgress);
            var result = await publishingWorkspaceService.PublishAsync(
                new PublishingWorkspaceRequest
                {
                    ProjectPath = ResolveDiscoveryPath(ManualProjectPath),
                    ServiceId = ServiceId,
                    Version = requestedVersion,
                    OutputDirectory = OutputDirectory,
                    Profile = ActiveProfile
                },
                progress,
                publishCancellation.Token);

            ApplyResultDiagnostics(result);

            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.PackagePath))
            {
                if (TryDetectReleaseConflict(result, out var conflict))
                {
                    ConflictRequestedVersion = requestedVersion;
                    ConflictExistingVersion = conflict.Version;
                    ConflictTechnicalDetails =
                        $"Artifact: {conflict.ArtifactId}\nExisting fingerprint: {conflict.ExistingFingerprint}\nNew fingerprint: {conflict.NewFingerprint}";
                    NewVersionSuggestion = SemanticVersionHelper.Bump(conflict.Version, SemanticVersionHelper.VersionPart.Patch);
                    NewVersionInput = NewVersionSuggestion;
                    ShowVersionConflict = true;
                    CurrentWizardStep = 3;
                    CurrentPipelineStep = "Version Conflict";
                    StatusMessage = $"Version {conflict.Version} already exists with different content.";
                    return;
                }

                StatusMessage = result.Message;
                CurrentPipelineStep = "Failed";
                PipelineState = result.PipelineState;
                ReadinessReport = result.ReadinessReport;
                if (result.ReadinessReport is not null)
                {
                    CurrentWizardStep = 4;
                }

                if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    ProgressLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Deployment Readiness: {result.Message}");
                }

                BuildFailureDetails(result);
                return;
            }

            PublishedPackagePath = result.PackagePath;
            PipelineState = result.PipelineState;
            ReadinessReport = result.ReadinessReport;
            LastPublishDisplay = DateTime.Now.ToString("HH:mm:ss");

            if (wasPreExisting)
            {
                // Same version, identical content: the Release Library treats re-registration as an
                // idempotent no-op (ADR-0011/D10) rather than a failure, so the pipeline succeeded —
                // this banner is purely informational, telling the operator why nothing "new" appeared.
                ConflictRequestedVersion = requestedVersion;
                ShowAlreadyPublishedNotice = true;
            }

            await RefreshExistingVersionsAsync(applyAsDefault: false);

            foreach (var file in result.GeneratedFiles)
            {
                GeneratedFiles.Add(file);
            }

            foreach (var warning in result.Warnings)
            {
                ValidationWarnings.Add(warning);
            }

            ValidationPassed = result.IsDeploymentReady;
            CurrentWizardStep = 4;
            StatusMessage = result.IsDeploymentCertified
                ? "Deployment Certified."
                : result.IsDeploymentReady
                    ? "Deployment Ready."
                    : result.Message;

            if (result.IsDeploymentCertified)
            {
                try
                {
                    ReleaseLibraryPath = await publishingWorkspaceService.AddToReleaseLibraryAsync(PublishedPackagePath);
                    CurrentWizardStep = 5;
                    StatusMessage = $"Deployment Certified. Package registered in Release Library at {ReleaseLibraryPath}.";
                }
                catch (InvalidOperationException exception) when (PresentReleaseConflict(exception.Message, requestedVersion))
                {
                    return;
                }
            }
            else if (result.IsDeploymentReady)
            {
                StatusMessage = "Deployment Ready. Package verification or certification did not complete; Release Library import is blocked.";
            }

            OnPropertyChanged(nameof(IsDeploymentCertified));
            OnPropertyChanged(nameof(CanAddToReleaseLibrary));
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Publishing was cancelled.";
            CurrentPipelineStep = "Cancelled";
        }
        finally
        {
            IsPublishing = false;
        }
    }

    [RelayCommand]
    private void CancelPublish()
    {
        publishCancellation?.Cancel();
    }

    /// <summary>Detects the ADR-0011/D10 "duplicate artifact ID, different fingerprint" failure,
    /// checking the top-level result message first and then every pipeline stage — the two backend
    /// layers that enforce this (pre-flight stage vs. registration) word the message differently.</summary>
    private static bool TryDetectReleaseConflict(PublishingResult result, out ReleaseConflictInfo conflict)
    {
        if (ReleaseConflictParser.TryParse(result.Message, out conflict))
        {
            return true;
        }

        if (result.PipelineState is not null)
        {
            foreach (var stage in result.PipelineState.Stages)
            {
                var candidate = stage.Errors.FirstOrDefault() ?? stage.Recommendation;
                if (ReleaseConflictParser.TryParse(candidate, out conflict))
                {
                    return true;
                }
            }
        }

        conflict = null!;
        return false;
    }

    [RelayCommand]
    private void StartPublishAsNewVersion() => ShowNewVersionPanel = true;

    [RelayCommand]
    private async Task ConfirmNewVersionAsync()
    {
        var candidate = NewVersionInput?.Trim() ?? string.Empty;
        if (!SemanticVersionHelper.IsValid(candidate))
        {
            NewVersionValidationMessage = "Enter a version in the form Major.Minor.Patch (for example 1.0.1).";
            return;
        }

        if (ExistingVersions.Any(existing => string.Equals(existing, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            NewVersionValidationMessage = $"Version {candidate} also already exists. Choose a different version.";
            return;
        }

        NewVersionValidationMessage = string.Empty;
        Version = candidate;
        Revision = string.Empty;
        BuildNumber = string.Empty;
        ShowNewVersionPanel = false;
        ShowVersionConflict = false;

        // Stay inside the wizard: re-invoke publish directly rather than sending the operator back
        // to Select Source / Project Analysis.
        await PublishAsync();
    }

    [RelayCommand]
    private void ChangeVersion()
    {
        ShowVersionConflict = false;
        ShowNewVersionPanel = false;
        CurrentWizardStep = 2;
        StatusMessage = "Update the version, then continue to publish.";
    }

    [RelayCommand]
    private void CompareReleases() => workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceReleaseLibrary);

    [RelayCommand]
    private void CancelVersionConflict()
    {
        ShowVersionConflict = false;
        ShowNewVersionPanel = false;
        CurrentWizardStep = 2;
        StatusMessage = "Publishing cancelled at the version conflict. Update the version to continue.";
    }

    [RelayCommand]
    private void OpenReleaseFromNotice()
    {
        ShowAlreadyPublishedNotice = false;
        workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceReleaseLibrary);
    }

    [RelayCommand]
    private void DismissAlreadyPublishedNotice() => ShowAlreadyPublishedNotice = false;

    private void ApplyResultDiagnostics(PublishingResult result)
    {
        QualityGates.Clear();
        if (result.QualityGates is not null)
        {
            foreach (var gate in result.QualityGates.Gates)
            {
                QualityGates.Add(gate);
            }
        }

        StageTimings.Clear();
        foreach (var timing in result.StageTimings)
        {
            StageTimings.Add(timing);
        }

        if (result.PipelineState is not null)
        {
            PipelineState = result.PipelineState;
        }
    }

    private void BuildFailureDetails(PublishingResult result)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            lines.Add(result.Message);
        }

        var failedGates = result.QualityGates?.FailedGates ?? [];
        foreach (var gate in failedGates)
        {
            lines.Add($"[GATE] {gate.Name}: {gate.Detail}");
        }

        if (result.ReadinessReport is not null)
        {
            foreach (var error in result.ReadinessReport.Errors)
            {
                lines.Add($"[ERROR] {error}");
            }

            foreach (var stage in result.ReadinessReport.StageResults.Where(stage => !stage.Passed && !stage.Deferred))
            {
                lines.Add($"[STAGE FAILED] {stage.StageName}: {stage.DiagnosticsSummary}");
            }
        }

        if (result.PipelineState is not null)
        {
            foreach (var stage in result.PipelineState.Stages.Where(stage => !stage.Passed))
            {
                var detail = stage.Errors.FirstOrDefault() ?? stage.Recommendation ?? stage.State.ToString();
                lines.Add($"[PIPELINE] {stage.StageName}: {detail}");
            }
        }

        FailureDetails = string.Join(Environment.NewLine, lines.Distinct());
    }

    [RelayCommand]
    private async Task ValidateArtifactAsync()
    {
        if (string.IsNullOrWhiteSpace(PublishedPackagePath))
        {
            ValidationSummary = "No package is available to validate.";
            return;
        }

        var validation = await publishingWorkspaceService.ValidatePackageAsync(PublishedPackagePath);
        ValidationErrors.Clear();
        ValidationWarnings.Clear();

        foreach (var error in validation.Errors)
        {
            ValidationErrors.Add(error);
        }

        foreach (var warning in validation.Warnings)
        {
            ValidationWarnings.Add(warning);
        }

        ValidationPassed = validation.IsValid;
        ValidationSummary = validation.IsValid
            ? "Deployment package passed validation."
            : "Deployment package validation failed.";
        StatusMessage = ValidationSummary;
    }

    [RelayCommand]
    private void OpenReadinessReport()
    {
        if (ReadinessReport is null)
        {
            StatusMessage = "No deployment readiness report is available.";
            return;
        }

        var reportPath = Path.Combine(Path.GetTempPath(), $"aovias-readiness-{Guid.NewGuid():N}.txt");
        var lines = new List<string>
        {
            ReadinessReport.StatusText,
            $"Score: {ReadinessReport.OverallScore}%",
            $"Duration: {ReadinessReport.ValidationDuration.TotalSeconds:F2}s",
            string.Empty
        };
        lines.AddRange(ReadinessReport.StageResults.Select(stage => $"{stage.StageName}: {(stage.Passed ? "PASS" : "FAIL")} ({stage.Duration.TotalSeconds:F2}s)"));
        File.WriteAllLines(reportPath, lines);
        Process.Start(new ProcessStartInfo(reportPath) { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenValidationLogs()
    {
        if (string.IsNullOrWhiteSpace(ValidationLogPath) || !File.Exists(ValidationLogPath))
        {
            StatusMessage = "Deployment validation log was not found.";
            return;
        }

        Process.Start(new ProcessStartInfo(ValidationLogPath) { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenPublishedPackage()
    {
        if (string.IsNullOrWhiteSpace(PublishedPackagePath) || !File.Exists(PublishedPackagePath))
        {
            StatusMessage = "Published package was not found.";
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{PublishedPackagePath}\"") { UseShellExecute = true });
    }

    private void ApplyReadinessReport(DeploymentReadinessReport? report)
    {
        ReadinessStages.Clear();
        ReadinessRecommendations.Clear();
        ValidationErrors.Clear();
        ContractValidationResults.Clear();
        ContractCapabilities.Clear();
        ContractDependencies.Clear();
        ContractSecrets.Clear();
        ContractFilesystemPaths.Clear();
        PipelineStageResults.Clear();
        ContractRuntimeCompatibility = "Not evaluated";
        PipelineStatus = "Pipeline has not run.";

        if (report is null)
        {
            ReadinessScore = 0;
            ReadinessStatus = "Not validated";
            ValidationSummary = "Publishing readiness validation did not run.";
            OnPropertyChanged(nameof(IsDeploymentReady));
            OnPropertyChanged(nameof(CanAddToReleaseLibrary));
            return;
        }

        ReadinessScore = report.OverallScore;
        ReadinessStatus = report.StatusText;
        ValidationLogPath = report.LogFilePath;
        ValidationSummary = report.IsReady
            ? report.StatusText.Contains("PUBLISHING", StringComparison.OrdinalIgnoreCase)
                ? $"Publishing Ready ({report.OverallScore}%)."
                : $"Deployment Ready ({report.OverallScore}%)."
            : report.StatusText.Contains("PUBLISHING", StringComparison.OrdinalIgnoreCase)
                ? $"Not publishing ready ({report.OverallScore}%)."
                : $"Not deployment ready ({report.OverallScore}%).";

        foreach (var stage in report.StageResults)
        {
            ReadinessStages.Add(stage);
        }

        foreach (var recommendation in report.Recommendations)
        {
            ReadinessRecommendations.Add(recommendation);
        }

        foreach (var error in report.Errors)
        {
            ValidationErrors.Add(error);
        }

        foreach (var warning in report.Warnings)
        {
            ValidationWarnings.Add(warning);
        }

        if (report.ContractValidation is not null)
        {
            foreach (var item in report.ContractValidation.Results)
            {
                ContractValidationResults.Add(new ContractValidationDisplayItem
                {
                    Name = item.Name,
                    Passed = item.Passed,
                    Detail = item.Passed
                        ? "PASS"
                        : string.Join("; ", item.Errors)
                });
            }

            ContractRuntimeCompatibility = report.ContractValidation.Passed
                ? "Compatible with declared runtime capabilities"
                : "Runtime compatibility or contract validation failed";
        }

        ValidationPassed = report.IsReady;
        OnPropertyChanged(nameof(IsDeploymentReady));
        OnPropertyChanged(nameof(IsDeploymentCertified));
        OnPropertyChanged(nameof(CanAddToReleaseLibrary));
    }

    partial void OnReadinessReportChanged(DeploymentReadinessReport? value) => ApplyReadinessReport(value);

    partial void OnPipelineStateChanged(PublishingPipelineState? value)
    {
        PipelineStageResults.Clear();
        if (value is null)
        {
            PipelineStatus = "Pipeline has not run.";
            OnPropertyChanged(nameof(IsDeploymentCertified));
            OnPropertyChanged(nameof(CanAddToReleaseLibrary));
            return;
        }

        PipelineStatus = value.IsDeploymentCertified
            ? "Deployment Certified"
            : value.IsSuccessful
                ? "Pipeline Passed"
                : $"Pipeline Failed at {value.TerminalFailureStage}";

        foreach (var stage in value.Stages)
        {
            PipelineStageResults.Add(stage);
        }

        OnPropertyChanged(nameof(IsDeploymentCertified));
        OnPropertyChanged(nameof(CanAddToReleaseLibrary));
    }

    [RelayCommand]
    private async Task AddToReleaseLibraryAsync()
    {
        if (string.IsNullOrWhiteSpace(PublishedPackagePath))
        {
            StatusMessage = "Publish a package before adding it to the Release Library.";
            return;
        }

        if (!IsDeploymentCertified)
        {
            StatusMessage = "Only deployment-certified packages can be added to the Release Library.";
            return;
        }

        if (!ValidationPassed || !IsDeploymentReady)
        {
            StatusMessage = "Deployment readiness must pass before adding the package to the Release Library.";
            return;
        }

        try
        {
            ReleaseLibraryPath = await publishingWorkspaceService.AddToReleaseLibraryAsync(PublishedPackagePath);
            CurrentWizardStep = 5;
            LastReleaseDisplay = DateTime.Now.ToString("HH:mm:ss");
            StatusMessage = $"Deployment-certified package copied to {ReleaseLibraryPath}.";
        }
        catch (InvalidOperationException exception) when (PresentReleaseConflict(exception.Message, Version))
        {
            // Version conflict UI is shown; do not surface as an unhandled exception.
        }
    }

    private bool PresentReleaseConflict(string message, string requestedVersion)
    {
        if (!ReleaseConflictParser.TryParse(message, out var conflict))
        {
            return false;
        }

        ConflictRequestedVersion = requestedVersion;
        ConflictExistingVersion = conflict.Version;
        ConflictTechnicalDetails =
            $"Artifact: {conflict.ArtifactId}\nExisting fingerprint: {conflict.ExistingFingerprint}\nNew fingerprint: {conflict.NewFingerprint}";
        NewVersionSuggestion = SemanticVersionHelper.Bump(conflict.Version, SemanticVersionHelper.VersionPart.Patch);
        NewVersionInput = NewVersionSuggestion;
        ShowVersionConflict = true;
        CurrentWizardStep = 3;
        CurrentPipelineStep = "Version Conflict";
        StatusMessage =
            $"Version {conflict.Version} is already registered in the Release Library with different content. "
            + $"Publish as {NewVersionSuggestion} instead.";
        return true;
    }

    [RelayCommand]
    private void OpenReleaseCenter()
    {
        workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceReleaseCenter);
    }

    [RelayCommand]
    private void OpenDeploymentWorkspace()
    {
        workspaceNavigator.NavigateTo(ShellViewModel.WorkspaceDeployment);
    }

    private void UpdateProgress(PublishingProgress progress)
    {
        CurrentPipelineStep = progress.StepName;
        PublishProgressPercent = progress.PercentComplete;
        ProgressLog.Insert(0, $"[{progress.Timestamp.LocalDateTime:HH:mm:ss}] {progress.StepName}: {progress.Message}");

        while (ProgressLog.Count > 40)
        {
            ProgressLog.RemoveAt(ProgressLog.Count - 1);
        }
    }

    private static string ResolveDiscoveryPath(string path)
    {
        if (Directory.Exists(path))
        {
            var csproj = Directory.EnumerateFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (csproj is not null)
            {
                return csproj;
            }

            if (File.Exists(Path.Combine(path, "angular.json")))
            {
                return path;
            }

            return path;
        }

        if (path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(path) ?? path;
        }

        return path;
    }

    partial void OnDiscoveryResultChanged(ProjectDiscoveryResult? value)
    {
        OnPropertyChanged(nameof(HasDiscovery));
        OnPropertyChanged(nameof(CanStartPublish));
    }

    partial void OnIsPublishingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStartPublish));
        OnPropertyChanged(nameof(CanPublishSolution));
        OnPropertyChanged(nameof(CanRunPreflight));
    }

    partial void OnIsSolutionPublishingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanPublishSolution));
        OnPropertyChanged(nameof(CanRunPreflight));
    }

    partial void OnIsPreflightRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanPublishSolution));
        OnPropertyChanged(nameof(CanRunPreflight));
    }

    partial void OnPreflightReadyChanged(bool value) => OnPropertyChanged(nameof(CanPublishSolution));

    private static string FormatDiskUsage(PublishingResourceSnapshot? snapshot)
        => snapshot is null
            ? string.Empty
            : $"Current {FormatBytes(snapshot.CurrentAvailableBytes)} | Peak {FormatBytes(snapshot.PeakConsumedBytes)} | Freed {FormatBytes(snapshot.FreedBytes)} | Remaining {FormatBytes(snapshot.RemainingBytes)}";

    private static string FormatBytes(long bytes)
    {
        double size = bytes;
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }
}

public sealed class ContractValidationDisplayItem
{
    public required string Name { get; init; }

    public bool Passed { get; init; }

    public required string Detail { get; init; }

    public string StatusText => Passed ? "PASS" : "FAIL";
}
