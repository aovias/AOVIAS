using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using ComplianceServiceManager.AI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Services;

namespace ComplianceServiceManager.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ServiceManagerService serviceManager;
    private readonly IAIFixOrchestrator aiFixOrchestrator;
    private readonly IRepairHistoryStore repairHistoryStore;
    private readonly IRepairAnalyticsService repairAnalyticsService;
    private readonly ILogFileService logFileService;
    private readonly DispatcherTimer logTailTimer;
    private readonly CancellationTokenSource shutdownToken = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedEnvironment = "Development";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isLogAutoScrollEnabled = true;

    [ObservableProperty]
    private string selectedLogLevelFilter = "All";

    [ObservableProperty]
    private string aiAnalysisText = "AI analysis will appear here after running AI Fix.";

    [ObservableProperty]
    private bool isFixPanelVisible;

    [ObservableProperty]
    private bool isAIFixBusy;

    [ObservableProperty]
    private string aiFixStatus = string.Empty;

    [ObservableProperty]
    private bool dryRunMode;

    [ObservableProperty]
    private bool autoApplyFixes = true;

    [ObservableProperty]
    private int maxRepairRetries = 2;

    [ObservableProperty]
    private string analyticsSummary = "No repair analytics yet.";

    [ObservableProperty]
    private string? selectedLogFileService;

    [ObservableProperty]
    private string fileLogSearchText = string.Empty;

    [ObservableProperty]
    private string selectedFileLogLevel = "All";

    [ObservableProperty]
    private bool isLogTailEnabled;

    [ObservableProperty]
    private string fileLogStatus = "No log files loaded.";

    [ObservableProperty]
    private string logCopyStatus = string.Empty;

    public MainViewModel(
        ServiceManagerService serviceManager,
        ILoggingService loggingService,
        IAIFixOrchestrator aiFixOrchestrator,
        IRepairHistoryStore repairHistoryStore,
        IRepairAnalyticsService repairAnalyticsService,
        ILogFileService logFileService)
    {
        this.serviceManager = serviceManager;
        this.aiFixOrchestrator = aiFixOrchestrator;
        this.repairHistoryStore = repairHistoryStore;
        this.repairAnalyticsService = repairAnalyticsService;
        this.logFileService = logFileService;
        Logs = loggingService.Entries;
        LogsView = CollectionViewSource.GetDefaultView(Logs);
        LogsView.Filter = FilterLog;
        ServicesView = CollectionViewSource.GetDefaultView(serviceManager.Services);
        ServicesView.Filter = FilterService;
        serviceManager.Services.CollectionChanged += OnServicesChanged;
        RepairHistory = repairHistoryStore.Entries;

        FileLogEntries = new ObservableCollection<ServiceLogEntry>();
        FileLogsView = CollectionViewSource.GetDefaultView(FileLogEntries);
        FileLogsView.Filter = FilterFileLog;

        logTailTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        logTailTimer.Tick += async (_, _) => await LoadServiceLogsAsync();

        RefreshLogServices();

        _ = RefreshServicesAsync();
    }

    public ObservableCollection<LogEntryModel> Logs { get; }

    public ICollectionView ServicesView { get; }

    public ICollectionView LogsView { get; }

    public ObservableCollection<RepairHistoryEntry> RepairHistory { get; }

    public ObservableCollection<ServiceLogEntry> FileLogEntries { get; }

    public ICollectionView FileLogsView { get; }

    public ObservableCollection<string> LogFileServiceNames { get; } = new();

    public IReadOnlyList<string> Environments { get; } = new[] { "Development", "QA", "UAT" };

    public IReadOnlyList<string> LogLevelFilters { get; } = new[] { "All", "INFO", "WARNING", "ERROR", "BUILD", "PROCESS" };

    public IReadOnlyList<string> FileLogLevelFilters { get; } = new[] { "All", "Information", "Warning", "Error", "Fatal" };

    public int ServiceCount => serviceManager.Services.Count;

    public int RunningServicesCount => serviceManager.Services.Count(service => service.Status == ServiceStatus.Running);

    public int ErrorServicesCount => serviceManager.Services.Count(service => service.Status == ServiceStatus.Error);

    public int FailedRepairCount => serviceManager.Services.Count(service => service.Status == ServiceStatus.FailedRepair);

    [RelayCommand]
    private async Task RefreshServicesAsync()
    {
        await RunBusyAsync(async token =>
        {
            await serviceManager.RefreshAsync(token);
            ApplyEnvironmentToServices();
            ServicesView.Refresh();
        });
    }

    [RelayCommand]
    private async Task StartServiceAsync(ServiceInfoModel? service)
    {
        if (service is null)
        {
            return;
        }

        service.EnvironmentName = SelectedEnvironment;
        await serviceManager.StartAsync(service, shutdownToken.Token);
    }

    [RelayCommand]
    private async Task StopServiceAsync(ServiceInfoModel? service)
    {
        if (service is null)
        {
            return;
        }

        await serviceManager.StopAsync(service, shutdownToken.Token);
    }

    [RelayCommand]
    private async Task RestartServiceAsync(ServiceInfoModel? service)
    {
        if (service is null)
        {
            return;
        }

        service.EnvironmentName = SelectedEnvironment;
        await serviceManager.RestartAsync(service, shutdownToken.Token);
    }

    [RelayCommand]
    private async Task StartAllAsync()
    {
        ApplyEnvironmentToServices();
        await RunBusyAsync(serviceManager.StartAllAsync);
    }

    [RelayCommand]
    private async Task StopAllAsync()
    {
        await RunBusyAsync(serviceManager.StopAllAsync);
    }

    [RelayCommand]
    private async Task RestartAllAsync()
    {
        ApplyEnvironmentToServices();
        await RunBusyAsync(serviceManager.RestartAllAsync);
    }

    [RelayCommand]
    private void OpenSwagger(ServiceInfoModel? service)
    {
        if (service?.OpenUrl is null)
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = service.OpenUrl,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
        LogCopyStatus = string.Empty;
    }

    [RelayCommand]
    private void CopyLogs(IList? selectedItems)
    {
        var selectedLogs = selectedItems?
            .OfType<LogEntryModel>()
            .ToList() ?? new List<LogEntryModel>();

        var logsToCopy = selectedLogs.Count > 0
            ? selectedLogs
            : LogsView.Cast<LogEntryModel>().ToList();

        if (logsToCopy.Count == 0)
        {
            LogCopyStatus = "No logs to copy.";
            return;
        }

        Clipboard.SetText(string.Join(Environment.NewLine, logsToCopy.Select(log => log.DisplayText)));
        LogCopyStatus = selectedLogs.Count > 0
            ? $"Copied {selectedLogs.Count} selected log line(s)."
            : $"Copied {logsToCopy.Count} visible log line(s).";
    }

    [RelayCommand]
    private async Task AIFixServiceAsync(ServiceInfoModel? service)
    {
        if (service is null || IsAIFixBusy)
        {
            return;
        }

        IsFixPanelVisible = true;
        IsAIFixBusy = true;
        AiFixStatus = $"Starting AI fix for {service.Name}...";
        var progress = new Progress<string>(message => AiFixStatus = message);
        try
        {
            var options = CreateFixOptions();
            // Offload the whole fix pipeline (diagnostics, git, build, restart) to a background
            // thread so the UI stays responsive and the spinner keeps animating.
            var result = await Task.Run(
                () => aiFixOrchestrator.RunAsync(service, options, progress, shutdownToken.Token),
                shutdownToken.Token);
            AiAnalysisText = BuildFixSummary(service, result);
            RefreshAnalytics();
            NotifyServiceCountersChanged();
        }
        catch (OperationCanceledException)
        {
            AiAnalysisText = "AI fix was cancelled.";
        }
        catch (Exception ex)
        {
            AiAnalysisText = $"AI fix failed: {ex.Message}";
        }
        finally
        {
            IsAIFixBusy = false;
            AiFixStatus = string.Empty;
        }
    }

    [RelayCommand]
    private async Task FixFailedServicesAsync()
    {
        if (IsAIFixBusy)
        {
            return;
        }

        var failedServices = serviceManager.Services
            .Where(service => service.Status is ServiceStatus.Error or ServiceStatus.FailedRepair)
            .ToList();
        if (failedServices.Count == 0)
        {
            AiAnalysisText = "No failed services found.";
            IsFixPanelVisible = true;
            return;
        }

        IsAIFixBusy = true;
        IsFixPanelVisible = true;
        AiFixStatus = $"Starting AI fix for {failedServices.Count} service(s)...";
        var progress = new Progress<string>(message => AiFixStatus = message);
        try
        {
            var options = CreateFixOptions();
            var results = await Task.Run(
                () => aiFixOrchestrator.FixAllFailedAsync(failedServices, options, progress, shutdownToken.Token),
                shutdownToken.Token);
            var successCount = results.Count(result => result.Success);
            AiAnalysisText = $"Fix All complete: {successCount}/{results.Count} service repairs succeeded.";
            RefreshAnalytics();
            NotifyServiceCountersChanged();
        }
        catch (OperationCanceledException)
        {
            AiAnalysisText = "AI fix was cancelled.";
        }
        catch (Exception ex)
        {
            AiAnalysisText = $"AI fix failed: {ex.Message}";
        }
        finally
        {
            IsAIFixBusy = false;
            AiFixStatus = string.Empty;
        }
    }

    [RelayCommand]
    private void RefreshLogServices()
    {
        var services = logFileService.ListServices();
        LogFileServiceNames.Clear();
        foreach (var name in services)
        {
            LogFileServiceNames.Add(name);
        }

        if (SelectedLogFileService is null || !services.Contains(SelectedLogFileService))
        {
            SelectedLogFileService = services.FirstOrDefault();
        }

        FileLogStatus = services.Count == 0
            ? $"No log folders found under {logFileService.ResolveLogRoot()}."
            : $"{services.Count} service log folder(s) under {logFileService.ResolveLogRoot()}.";
    }

    [RelayCommand]
    private async Task LoadServiceLogsAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedLogFileService))
        {
            return;
        }

        try
        {
            var entries = await logFileService.ReadEntriesAsync(SelectedLogFileService, 1000, shutdownToken.Token);
            ReplaceFileLogEntries(entries);
            FileLogStatus = $"{entries.Count} entries from {SelectedLogFileService} ({logFileService.ListLogFiles(SelectedLogFileService).Count} file(s)).";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            FileLogStatus = $"Failed to read logs: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ShowLatestFailuresAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedLogFileService))
        {
            return;
        }

        try
        {
            var failures = await logFileService.GetLatestFailuresAsync(SelectedLogFileService, 100, shutdownToken.Token);
            ReplaceFileLogEntries(failures.OrderBy(e => e.Timestamp));
            SelectedFileLogLevel = "All";
            FileLogStatus = $"{failures.Count} recent failure(s) for {SelectedLogFileService}.";
        }
        catch (Exception ex)
        {
            FileLogStatus = $"Failed to read failures: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DownloadServiceLogsAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedLogFileService))
        {
            return;
        }

        try
        {
            var archive = await logFileService.ExportAsync(SelectedLogFileService, shutdownToken.Token);
            if (archive is null)
            {
                FileLogStatus = "No log files to download.";
                return;
            }

            FileLogStatus = $"Exported to {archive}";
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.GetDirectoryName(archive) ?? archive,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            FileLogStatus = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        var path = string.IsNullOrWhiteSpace(SelectedLogFileService)
            ? logFileService.ResolveLogRoot()
            : Path.Combine(logFileService.ResolveLogRoot(), SelectedLogFileService);

        if (!Directory.Exists(path))
        {
            FileLogStatus = $"Folder does not exist: {path}";
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void ReplaceFileLogEntries(IEnumerable<ServiceLogEntry> entries)
    {
        FileLogEntries.Clear();
        foreach (var entry in entries)
        {
            FileLogEntries.Add(entry);
        }

        FileLogsView.Refresh();
    }

    private bool FilterFileLog(object item)
    {
        if (item is not ServiceLogEntry entry)
        {
            return false;
        }

        if (SelectedFileLogLevel != "All"
            && !entry.Level.Equals(SelectedFileLogLevel, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(FileLogSearchText))
        {
            return true;
        }

        return entry.Message.Contains(FileLogSearchText, StringComparison.OrdinalIgnoreCase)
            || (entry.EventCode?.Contains(FileLogSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            || (entry.CorrelationId?.Contains(FileLogSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            || (entry.Exception?.Contains(FileLogSearchText, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    partial void OnSearchTextChanged(string value) => ServicesView.Refresh();

    partial void OnSelectedLogLevelFilterChanged(string value) => LogsView.Refresh();

    partial void OnSelectedEnvironmentChanged(string value) => ApplyEnvironmentToServices();

    partial void OnSelectedFileLogLevelChanged(string value) => FileLogsView.Refresh();

    partial void OnFileLogSearchTextChanged(string value) => FileLogsView.Refresh();

    partial void OnSelectedLogFileServiceChanged(string? value) => _ = LoadServiceLogsAsync();

    partial void OnIsLogTailEnabledChanged(bool value)
    {
        if (value)
        {
            logTailTimer.Start();
        }
        else
        {
            logTailTimer.Stop();
        }
    }

    public void Dispose()
    {
        logTailTimer.Stop();
        shutdownToken.Cancel();
        serviceManager.Services.CollectionChanged -= OnServicesChanged;
        foreach (var service in serviceManager.Services)
        {
            service.PropertyChanged -= OnServicePropertyChanged;
        }

        serviceManager.Dispose();
        shutdownToken.Dispose();
    }

    private bool FilterService(object item)
    {
        if (item is not ServiceInfoModel service)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return service.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || service.ProjectPath.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || (service.LaunchUrl?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private async Task RunBusyAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action(shutdownToken.Token);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyEnvironmentToServices()
    {
        foreach (var service in serviceManager.Services)
        {
            service.EnvironmentName = SelectedEnvironment;
        }
    }

    private bool FilterLog(object item)
    {
        if (item is not LogEntryModel logEntry)
        {
            return false;
        }

        return SelectedLogLevelFilter == "All"
            || logEntry.LevelText.Equals(SelectedLogLevelFilter, StringComparison.OrdinalIgnoreCase);
    }

    private void OnServicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ServiceInfoModel service in e.OldItems)
            {
                service.PropertyChanged -= OnServicePropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (ServiceInfoModel service in e.NewItems)
            {
                service.PropertyChanged += OnServicePropertyChanged;
            }
        }

        NotifyServiceCountersChanged();
    }

    private void OnServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ServiceInfoModel.Status))
        {
            NotifyServiceCountersChanged();
        }
    }

    private void NotifyServiceCountersChanged()
    {
        OnPropertyChanged(nameof(ServiceCount));
        OnPropertyChanged(nameof(RunningServicesCount));
        OnPropertyChanged(nameof(ErrorServicesCount));
        OnPropertyChanged(nameof(FailedRepairCount));
    }

    private AIFixOptions CreateFixOptions()
    {
        return new AIFixOptions
        {
            DryRun = DryRunMode,
            AutoApply = AutoApplyFixes,
            MaxRetries = Math.Max(1, MaxRepairRetries),
            StageTimeout = TimeSpan.FromMinutes(3)
        };
    }

    private string BuildFixSummary(ServiceInfoModel service, FixExecutionResult result)
    {
        var lines = new List<string>
        {
            $"Service: {service.Name}",
            $"Result: {(result.Success ? "Success" : "Failed")}",
            $"Summary: {result.Summary}",
            $"Checkpoint: {result.CheckpointReference}"
        };

        if (result.ModifiedFiles.Count > 0)
        {
            lines.Add("Modified Files:");
            lines.AddRange(result.ModifiedFiles.Select(file => $"- {file}"));
        }

        if (result.Actions.Count > 0)
        {
            lines.Add("Actions:");
            lines.AddRange(result.Actions.Select(action => $"- {action}"));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private void RefreshAnalytics()
    {
        var snapshot = repairAnalyticsService.GetSnapshot();
        AnalyticsSummary = $"Attempts: {snapshot.TotalAttempts} | Success: {snapshot.SuccessRate:0.00}% | Avg: {snapshot.AverageRepairDuration.TotalSeconds:0.0}s{Environment.NewLine}{string.Join(Environment.NewLine, snapshot.Recommendations)}";
    }
}
