using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.Models.LocalResources;

/// <summary>UI row for a service-associated process resource sample.</summary>
public sealed partial class LocalServiceResourceRow : ObservableObject
{
    [ObservableProperty]
    private string serviceName = string.Empty;

    [ObservableProperty]
    private string processIdDisplay = "—";

    [ObservableProperty]
    private string cpuDisplay = "—";

    [ObservableProperty]
    private string memoryDisplay = "—";

    [ObservableProperty]
    private string note = string.Empty;
}
