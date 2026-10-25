using System.Collections.ObjectModel;
using System.Windows;
using Aovias.Release.Contracts.Releases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;

namespace ComplianceServiceManager.ViewModels.Applications;

/// <summary>
/// Applications → Rollback &amp; History workspace: the durable deployment history with
/// operator-confirmed rollback of a selected release. Read path and rollback both go through
/// the existing deployment service — no new pipeline behavior.
/// </summary>
public sealed partial class RollbackHistoryViewModel : ObservableObject
{
    private readonly IDeploymentService deploymentService;

    [ObservableProperty]
    private string statusMessage = "Loading deployment history…";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ReleaseHistoryEntry? selectedEntry;

    public ObservableCollection<ReleaseHistoryEntry> History { get; } = [];

    public RollbackHistoryViewModel(IDeploymentService deploymentService)
    {
        this.deploymentService = deploymentService;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            History.Clear();
            foreach (var entry in await deploymentService.GetHistoryAsync())
            {
                History.Add(entry);
            }

            StatusMessage = History.Count == 0
                ? "No deployments have been recorded yet."
                : $"{History.Count} deployment record(s).";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Unable to load history: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RollbackSelectedAsync()
    {
        if (SelectedEntry is null || IsBusy)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            $"Roll back {SelectedEntry.PackageId} {SelectedEntry.PackageVersion} on '{SelectedEntry.Environment}'?\n\n" +
            "The rollback runs through the same admission and execution pipeline as a deployment.",
            "Confirm Rollback",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await deploymentService.RollbackAsync(SelectedEntry.ReleaseId, Environment.UserName);
            StatusMessage = $"Rollback requested for {SelectedEntry.PackageId} {SelectedEntry.PackageVersion}.";
            await InitializeAsync();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Rollback failed: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
