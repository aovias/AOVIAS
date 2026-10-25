using System.Collections.ObjectModel;
using Aovias.Release.Contracts.RemoteDeployment;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Deployment;

namespace ComplianceServiceManager.ViewModels.Platform;

/// <summary>
/// Platform → Environments workspace: the registered deployment environments,
/// their health, readiness and capabilities. Master-detail: select an environment
/// on the left, inspect its detail on the right.
/// </summary>
public sealed partial class EnvironmentsViewModel : ObservableObject
{
    private readonly IEnvironmentManager environmentManager;

    [ObservableProperty]
    private string statusMessage = "Loading environments…";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private RemoteEnvironment? selectedEnvironment;

    [ObservableProperty]
    private EnvironmentHealthSnapshot? selectedHealth;

    public ObservableCollection<RemoteEnvironment> Environments { get; } = [];

    public EnvironmentsViewModel(IEnvironmentManager environmentManager)
    {
        this.environmentManager = environmentManager;
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
            Environments.Clear();
            foreach (var environment in await environmentManager.GetEnvironmentsAsync())
            {
                Environments.Add(environment);
            }

            StatusMessage = Environments.Count == 0
                ? "No environments are registered yet. Bootstrap a VPS to register your first environment."
                : $"{Environments.Count} environment(s) registered.";
            SelectedEnvironment ??= Environments.FirstOrDefault();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Unable to load environments: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshHealthAsync()
    {
        if (SelectedEnvironment is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            SelectedHealth = await environmentManager.RefreshHealthAsync(SelectedEnvironment.Id);
            StatusMessage = SelectedHealth is null
                ? $"No health snapshot available for {SelectedEnvironment.Name}."
                : $"Health refreshed for {SelectedEnvironment.Name}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Health refresh failed: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedEnvironmentChanged(RemoteEnvironment? value)
    {
        SelectedHealth = null;
        if (value is not null)
        {
            _ = RefreshHealthAsync();
        }
    }
}
