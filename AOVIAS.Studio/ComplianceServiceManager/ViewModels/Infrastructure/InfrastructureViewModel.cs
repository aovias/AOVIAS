using System.Collections.ObjectModel;
using Aovias.Runtime.Contracts.Observability;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ComplianceServiceManager.Contracts.Observability;

namespace ComplianceServiceManager.ViewModels.Infrastructure;

/// <summary>
/// Infrastructure workspace presentation model.
/// </summary>
public sealed partial class InfrastructureViewModel : ObservableObject
{
    private readonly IObservabilityService observabilityService;

    [ObservableProperty]
    private string statusMessage = "Loading infrastructure...";

    [ObservableProperty]
    private RuntimeStateDto? runtimeState;

    public ObservableCollection<ContainerObservationDto> Containers { get; } = [];

    public ObservableCollection<NetworkObservationDto> Networks { get; } = [];

    public ObservableCollection<VolumeObservationDto> Volumes { get; } = [];

    public ObservableCollection<string> Capabilities { get; } = [];

    public InfrastructureViewModel(IObservabilityService observabilityService)
    {
        this.observabilityService = observabilityService;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            RuntimeState = await observabilityService.GetStateAsync();
            Containers.Clear();
            foreach (var container in await observabilityService.GetContainersAsync())
            {
                Containers.Add(container);
            }

            Networks.Clear();
            foreach (var network in await observabilityService.GetNetworksAsync())
            {
                Networks.Add(network);
            }

            Volumes.Clear();
            if (RuntimeState?.Volumes is { } volumes)
            {
                foreach (var volume in volumes)
                {
                    Volumes.Add(volume);
                }
            }

            Capabilities.Clear();
            if (RuntimeState?.Capabilities is { } capabilities)
            {
                foreach (var capability in capabilities)
                {
                    Capabilities.Add(capability);
                }
            }

            StatusMessage = "Infrastructure snapshot refreshed.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }
}
