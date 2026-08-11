using System.Collections.ObjectModel;
using Aovias.Release.Contracts.RemoteDeployment;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;

namespace ComplianceServiceManager.ViewModels.Operations;

/// <summary>
/// Operations → Reconciliation workspace: run drift detection against a registered
/// environment and inspect the findings (expected vs actual, severity, category).
/// </summary>
public sealed partial class ReconciliationViewModel : ObservableObject
{
    private readonly IEnvironmentManager environmentManager;

    [ObservableProperty]
    private string statusMessage = "Select an environment and run drift detection.";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private RemoteEnvironment? selectedEnvironment;

    [ObservableProperty]
    private EnvironmentDriftReport? lastReport;

    [ObservableProperty]
    private string lastRunDisplay = string.Empty;

    public ObservableCollection<RemoteEnvironment> Environments { get; } = [];

    public ObservableCollection<EnvironmentDriftFinding> Findings { get; } = [];

    public ReconciliationViewModel(IEnvironmentManager environmentManager)
    {
        this.environmentManager = environmentManager;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            Environments.Clear();
            foreach (var environment in await environmentManager.GetEnvironmentsAsync())
            {
                Environments.Add(environment);
            }

            SelectedEnvironment ??= Environments.FirstOrDefault();
            if (Environments.Count == 0)
            {
                StatusMessage = "No environments are registered. Register an environment before running reconciliation.";
            }
        }
        catch (Exception exception)
        {
            StatusMessage = $"Unable to load environments: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task DetectDriftAsync()
    {
        if (SelectedEnvironment is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = $"Detecting drift on {SelectedEnvironment.Name}…";
        try
        {
            var report = await environmentManager.DetectDriftAsync(SelectedEnvironment.Id);
            LastReport = report;
            LastRunDisplay = $"Last run {report.GeneratedUtc.ToLocalTime():HH:mm:ss}";

            Findings.Clear();
            foreach (var finding in report.Findings)
            {
                Findings.Add(finding);
            }

            StatusMessage = report.Findings.Count == 0
                ? $"No drift detected on {SelectedEnvironment.Name}. Desired and observed state match."
                : report.RequiresReconciliation
                    ? $"{report.Findings.Count} drift finding(s) — reconciliation required."
                    : $"{report.Findings.Count} drift finding(s) recorded.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Drift detection failed: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
