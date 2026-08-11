using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Models.LocalPullRequests;
using ComplianceServiceManager.Models.LocalResources;
using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services;
using ComplianceServiceManager.Services.LocalDev;
using ComplianceServiceManager.Services.LocalPullRequests;
using ComplianceServiceManager.Services.LocalResources;
using ComplianceServiceManager.Services.RuntimeProfiles;
using ComplianceServiceManager.Services.Workspaces;
using ComplianceServiceManager.Views.Development;

namespace ComplianceServiceManager.ViewModels.Development;

/// <summary>
/// Development → Local Development hub. Hosts D1 workspaces, D2 runtime profiles,
/// S3 local resource telemetry, D3 local PRs, and Local Dev Services.
/// </summary>
public sealed partial class LocalDevServicesViewModel : ObservableObject
{
    private readonly SolutionFolderRegistry folderRegistry;
    private readonly IDevelopmentWorkspaceService workspaceService;
    private readonly IRuntimeProfileOrchestrator profileOrchestrator;
    private readonly ServiceManagerService serviceManager;
    private readonly ILocalResourceMonitoringService resourceMonitoring;
    private readonly ILocalPullRequestService pullRequestService;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string workspaceStatusMessage = string.Empty;

    [ObservableProperty]
    private string profileStatusMessage = string.Empty;

    [ObservableProperty]
    private string pullRequestStatusMessage = string.Empty;

    [ObservableProperty]
    private string? selectedFolder;

    [ObservableProperty]
    private bool isRefreshingWorkspaces;

    [ObservableProperty]
    private bool isProfileBusy;

    [ObservableProperty]
    private bool isPullRequestBusy;

    [ObservableProperty]
    private RuntimeProfileItem? selectedProfile;

    [ObservableProperty]
    private LocalPullRequestItem? selectedPullRequest;

    [ObservableProperty]
    private string reviewComment = string.Empty;

    [ObservableProperty]
    private bool canMergeSelectedPullRequest;

    public MainViewModel Services { get; }

    public LocalResourceMonitorItem LocalResources { get; } = new();

    public ObservableCollection<string> SolutionFolders { get; } = [];

    public ObservableCollection<DevelopmentWorkspaceItem> Workspaces { get; } = [];

    public ObservableCollection<RuntimeProfileItem> RuntimeProfiles { get; } = [];

    public ObservableCollection<LocalServiceResourceRow> ServiceResourceRows { get; } = [];

    public ObservableCollection<LocalPullRequestItem> LocalPullRequests { get; } = [];

    public LocalDevServicesViewModel(
        SolutionFolderRegistry folderRegistry,
        MainViewModel servicesViewModel,
        IDevelopmentWorkspaceService workspaceService,
        IRuntimeProfileOrchestrator profileOrchestrator,
        ServiceManagerService serviceManager,
        ILocalResourceMonitoringService resourceMonitoring,
        ILocalPullRequestService pullRequestService)
    {
        this.folderRegistry = folderRegistry;
        this.workspaceService = workspaceService;
        this.profileOrchestrator = profileOrchestrator;
        this.serviceManager = serviceManager;
        this.resourceMonitoring = resourceMonitoring;
        this.pullRequestService = pullRequestService;
        Services = servicesViewModel;
        SyncFolders();
        folderRegistry.FoldersChanged += SyncFolders;
        profileOrchestrator.ProfilesChanged += SyncProfiles;
        SyncProfiles();
        pullRequestService.PullRequestsChanged += SyncPullRequests;
        SyncPullRequests();
        resourceMonitoring.SnapshotUpdated += OnResourceSnapshotUpdated;
        resourceMonitoring.Start();
        if (resourceMonitoring.LatestSnapshot is not null)
        {
            ApplyResourceSnapshot(resourceMonitoring.LatestSnapshot);
        }

        _ = RefreshWorkspacesAsync();
        _ = RefreshLocalPullRequestsAsync();
    }

    partial void OnSelectedPullRequestChanged(LocalPullRequestItem? value)
    {
        UpdateMergeAvailability();
        ReviewComment = value?.LastReview?.Comment ?? string.Empty;
    }

    [RelayCommand]
    private void RefreshLocalResources()
    {
        var snapshot = resourceMonitoring.CaptureSnapshot();
        ApplyResourceSnapshot(snapshot);
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
        profileOrchestrator.ValidateAll();
        SyncProfiles();
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
        ProfileStatusMessage = result.IsValid
            ? $"{SelectedProfile.Name}: valid ({result.StartupOrder.Count} services in order)."
            : $"{SelectedProfile.Name}: {result.Issues.Count} issue(s).";
    }

    [RelayCommand]
    private void ValidateAllProfiles()
    {
        profileOrchestrator.ValidateAll();
        SyncProfiles();
        ProfileStatusMessage = "All profiles revalidated against current discovery.";
    }

    [RelayCommand]
    private async Task StartSelectedProfileAsync()
    {
        if (SelectedProfile is null || IsProfileBusy)
        {
            return;
        }

        IsProfileBusy = true;
        try
        {
            if (Services.ServiceCount == 0)
            {
                await Services.RefreshServicesCommand.ExecuteAsync(null);
            }

            profileOrchestrator.Validate(SelectedProfile);
            await profileOrchestrator.StartAsync(SelectedProfile);
            ProfileStatusMessage = SelectedProfile.ProgressMessage ?? SelectedProfile.StatusDisplay;
        }
        finally
        {
            IsProfileBusy = false;
        }
    }

    [RelayCommand]
    private async Task StopSelectedProfileAsync()
    {
        if (SelectedProfile is null || IsProfileBusy)
        {
            return;
        }

        IsProfileBusy = true;
        try
        {
            await profileOrchestrator.StopAsync(SelectedProfile);
            ProfileStatusMessage = SelectedProfile.ProgressMessage ?? SelectedProfile.StatusDisplay;
        }
        finally
        {
            IsProfileBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestartSelectedProfileAsync()
    {
        if (SelectedProfile is null || IsProfileBusy)
        {
            return;
        }

        IsProfileBusy = true;
        try
        {
            await profileOrchestrator.RestartAsync(SelectedProfile);
            ProfileStatusMessage = SelectedProfile.ProgressMessage ?? SelectedProfile.StatusDisplay;
        }
        finally
        {
            IsProfileBusy = false;
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
            && SelectedPullRequest.Comparison is { ApprovalIsStale: false, HasConflicts: false, SourceClean: true, TargetClean: true };
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
                Note = service.AssociationNote
            });
        }
    }
}
