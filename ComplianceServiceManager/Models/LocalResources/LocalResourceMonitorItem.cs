using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.Models.LocalResources;

/// <summary>UI-facing local resource monitor state (observation only).</summary>
public sealed partial class LocalResourceMonitorItem : ObservableObject
{
    [ObservableProperty]
    private DateTimeOffset? capturedAt;

    [ObservableProperty]
    private string statusMessage = "Waiting for first sample…";

    [ObservableProperty]
    private double cpuPercent;

    [ObservableProperty]
    private string cpuDisplay = "—";

    [ObservableProperty]
    private LocalResourceStatus cpuStatus = LocalResourceStatus.Unknown;

    [ObservableProperty]
    private double memoryPercent;

    [ObservableProperty]
    private string memoryDisplay = "—";

    [ObservableProperty]
    private LocalResourceStatus memoryStatus = LocalResourceStatus.Unknown;

    [ObservableProperty]
    private double diskPercent;

    [ObservableProperty]
    private string diskDisplay = "—";

    [ObservableProperty]
    private string diskRootDisplay = "—";

    [ObservableProperty]
    private LocalResourceStatus diskStatus = LocalResourceStatus.Unknown;

    [ObservableProperty]
    private string measurementNotes = string.Empty;
}
