using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Services.LocalDev;

namespace ComplianceServiceManager.ViewModels.Development;

/// <summary>
/// Development → Local Dev Services workspace. Manages the solution folders whose services are
/// discoverable, and hosts the existing service manager (start/stop/restart, health, logs and
/// repair) through the long-lived <see cref="MainViewModel"/>. Solution-agnostic by design:
/// any .NET or Angular solution folder can be added.
/// </summary>
public sealed partial class LocalDevServicesViewModel : ObservableObject
{
    private readonly SolutionFolderRegistry folderRegistry;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string? selectedFolder;

    public MainViewModel Services { get; }

    public ObservableCollection<string> SolutionFolders { get; } = [];

    public LocalDevServicesViewModel(SolutionFolderRegistry folderRegistry, MainViewModel servicesViewModel)
    {
        this.folderRegistry = folderRegistry;
        Services = servicesViewModel;
        SyncFolders();
        folderRegistry.FoldersChanged += SyncFolders;
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
        StatusMessage = $"{Services.ServiceCount} service(s) discovered across {SolutionFolders.Count} folder(s).";
    }

    private void SyncFolders()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            SolutionFolders.Clear();
            foreach (var folder in folderRegistry.Folders)
            {
                SolutionFolders.Add(folder);
            }
        });
    }
}
