using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.Models.ResourceIntelligence;

/// <summary>UI-facing resource intelligence snapshot (advisory only).</summary>
public sealed partial class ResourceIntelligenceItem : ObservableObject
{
    [ObservableProperty]
    private string overallPressureDisplay = "Unknown";

    [ObservableProperty]
    private ResourcePressureLevel overallPressure = ResourcePressureLevel.Unknown;

    [ObservableProperty]
    private string cpuDisplay = "—";

    [ObservableProperty]
    private string cpuPressureDisplay = "Unknown";

    [ObservableProperty]
    private string memoryDisplay = "—";

    [ObservableProperty]
    private string memoryPressureDisplay = "Unknown";

    [ObservableProperty]
    private string diskDisplay = "—";

    [ObservableProperty]
    private string diskPressureDisplay = "Unknown";

    [ObservableProperty]
    private string summary = "Waiting for telemetry…";

    [ObservableProperty]
    private string primaryRecommendationTitle = string.Empty;

    [ObservableProperty]
    private string primaryRecommendationExplanation = string.Empty;

    [ObservableProperty]
    private string? suggestedProfileId;

    [ObservableProperty]
    private string? suggestedProfileName;

    [ObservableProperty]
    private bool hasSuggestedProfile;

    public System.Collections.ObjectModel.ObservableCollection<SessionResourceContribution> ActiveSessions { get; } = [];

    public System.Collections.ObjectModel.ObservableCollection<ResourceRecommendation> Recommendations { get; } = [];

    public System.Collections.ObjectModel.ObservableCollection<RuntimeProfileResourceEstimate> ProfileEstimates { get; } = [];
}
