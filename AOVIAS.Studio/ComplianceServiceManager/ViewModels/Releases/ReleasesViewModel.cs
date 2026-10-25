using System.Collections.ObjectModel;
using Aovias.Release.Contracts.Releases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;

namespace ComplianceServiceManager.ViewModels.Releases;

/// <summary>
/// Releases workspace presentation model.
/// </summary>
public sealed partial class ReleasesViewModel : ObservableObject
{
    private readonly IDeploymentService deploymentService;

    [ObservableProperty]
    private string statusMessage = "Loading releases...";

    public ObservableCollection<ReleaseListItem> Releases { get; } = [];

    public ObservableCollection<ReleaseHistoryEntry> History { get; } = [];

    public ReleasesViewModel(IDeploymentService deploymentService)
    {
        this.deploymentService = deploymentService;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            Releases.Clear();
            foreach (var release in await deploymentService.GetReleasesAsync())
            {
                Releases.Add(release);
            }

            History.Clear();
            foreach (var entry in await deploymentService.GetHistoryAsync())
            {
                History.Add(entry);
            }

            StatusMessage = Releases.Count == 0 ? "No releases yet." : $"{Releases.Count} releases available.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }
}
