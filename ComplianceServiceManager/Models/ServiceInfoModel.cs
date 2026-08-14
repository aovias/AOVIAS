using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.Models;

public sealed partial class ServiceInfoModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string projectPath = string.Empty;

    [ObservableProperty]
    private string workingDirectory = string.Empty;

    [ObservableProperty]
    private string? launchUrl;

    [ObservableProperty]
    private int? port;

    [ObservableProperty]
    private string? launchProfileName;

    [ObservableProperty]
    private string? httpsUrl;

    [ObservableProperty]
    private int? httpsPort;

    [ObservableProperty]
    private string? httpUrl;

    [ObservableProperty]
    private int? httpPort;

    [ObservableProperty]
    private string environmentName = "Development";

    [ObservableProperty]
    private ServiceStatus status = ServiceStatus.Stopped;

    [ObservableProperty]
    private HealthStatus health = HealthStatus.Unknown;

    [ObservableProperty]
    private int? processId;

    [ObservableProperty]
    private DateTimeOffset? runningSince;

    [ObservableProperty]
    private string lastAction = "Discovered";

    [ObservableProperty]
    private string environment = "Development";

    [ObservableProperty]
    private ServiceKind kind = ServiceKind.WebApi;

    [ObservableProperty]
    private RepairStage repairStage = RepairStage.None;

    [ObservableProperty]
    private DateTimeOffset? lastRepairAt;

    [ObservableProperty]
    private string repairSummary = "No repair activity";

    [ObservableProperty]
    private bool isRepairing;

    [ObservableProperty]
    private int repairAttempts;

    [ObservableProperty]
    private FailureCategory lastFailureCategory = FailureCategory.Unknown;

    public IReadOnlyDictionary<string, string> LaunchEnvironmentVariables { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// When true, <c>dotnet run</c> must pass <c>--no-launch-profile</c> so
    /// ASPNETCORE_URLS is authoritative. Workspace Runtime sets this; Shared Runtime does not.
    /// </summary>
    public bool SuppressLaunchSettings { get; set; }

    internal Process? Process { get; set; }

    internal CancellationTokenSource? OutputCancellation { get; set; }

    public string DisplayPort => Port?.ToString() ?? "-";

    public string DisplayHttpsUrl => HttpsUrl ?? LaunchUrl ?? "-";

    public string DisplayLaunchProfileName => LaunchProfileName ?? "Default";

    public string DisplayEnvironmentName => EnvironmentName;

    public string DisplayKind => Kind == ServiceKind.AngularWebApp ? "Angular" : "Web API";

    public string? OpenUrl => Kind == ServiceKind.AngularWebApp ? LaunchUrl ?? HttpUrl : SwaggerUrl;

    public string? SwaggerUrl
    {
        get
        {
            if (Kind == ServiceKind.AngularWebApp)
            {
                return null;
            }

            var baseUrl = HttpsUrl ?? HttpUrl ?? LaunchUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            return $"{baseUrl.TrimEnd('/')}/swagger";
        }
    }

    public string DisplayProcessId => ProcessId?.ToString() ?? "-";

    public string DisplayRunningSince => RunningSince?.ToLocalTime().ToString("HH:mm:ss") ?? "-";

    public string DisplayRepairAt => LastRepairAt?.ToLocalTime().ToString("HH:mm:ss") ?? "-";

    partial void OnPortChanged(int? value) => OnPropertyChanged(nameof(DisplayPort));

    partial void OnHttpsUrlChanged(string? value)
    {
        OnPropertyChanged(nameof(DisplayHttpsUrl));
        OnPropertyChanged(nameof(SwaggerUrl));
    }

    partial void OnHttpUrlChanged(string? value)
    {
        OnPropertyChanged(nameof(SwaggerUrl));
        OnPropertyChanged(nameof(OpenUrl));
    }

    partial void OnLaunchUrlChanged(string? value)
    {
        OnPropertyChanged(nameof(DisplayHttpsUrl));
        OnPropertyChanged(nameof(SwaggerUrl));
    }

    partial void OnLaunchProfileNameChanged(string? value) => OnPropertyChanged(nameof(DisplayLaunchProfileName));

    partial void OnEnvironmentNameChanged(string value) => OnPropertyChanged(nameof(DisplayEnvironmentName));

    partial void OnKindChanged(ServiceKind value)
    {
        OnPropertyChanged(nameof(DisplayKind));
        OnPropertyChanged(nameof(SwaggerUrl));
        OnPropertyChanged(nameof(OpenUrl));
    }

    partial void OnProcessIdChanged(int? value) => OnPropertyChanged(nameof(DisplayProcessId));

    partial void OnRunningSinceChanged(DateTimeOffset? value) => OnPropertyChanged(nameof(DisplayRunningSince));

    partial void OnLastRepairAtChanged(DateTimeOffset? value) => OnPropertyChanged(nameof(DisplayRepairAt));
}
