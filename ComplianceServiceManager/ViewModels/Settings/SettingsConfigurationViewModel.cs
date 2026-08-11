using System.Collections.ObjectModel;
using Aovias.Configuration.Profiles;
using Aovias.Configuration.Secrets;
using Aovias.Configuration.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;

namespace ComplianceServiceManager.ViewModels.Settings;

/// <summary>
/// Presents profile-based runtime configuration editing for the Operations Console.
/// </summary>
public sealed partial class SettingsConfigurationViewModel : ObservableObject
{
    private readonly IOptionsMonitor<RuntimeSettings> runtimeOptions;
    private readonly IOptionsMonitor<InfrastructureSettings> infrastructureOptions;
    private readonly IOptionsMonitor<DockerSettings> dockerOptions;
    private readonly IOptionsMonitor<ApiSettings> apiOptions;
    private readonly IOptionsMonitor<SecuritySettings> securityOptions;
    private readonly IOptionsMonitor<LoggingSettings> loggingOptions;
    private readonly IOptionsMonitor<CertificateSettings> certificateOptions;
    private readonly IOptionsMonitor<DeploymentSettings> deploymentOptions;
    private readonly IOptionsMonitor<MonitoringSettings> monitoringOptions;
    private readonly IOptionsMonitor<OrchestrationSettings> orchestrationOptions;
    private readonly IOptionsMonitor<SignalRSettings> signalROptions;
    private readonly IOptionsMonitor<ProviderSettings> providerOptions;
    private readonly IOptionsMonitor<SecretsSettings> secretsOptions;
    private readonly IConfigurationWriter configurationWriter;
    private readonly IProfileManager profileManager;

    [ObservableProperty]
    private string selectedSection = "Runtime";

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string activeProfile = string.Empty;

    [ObservableProperty]
    private string selectedProfile = string.Empty;

    public ObservableCollection<string> AvailableProfiles { get; } = [];

    public RuntimeSettings Runtime { get; }
    public InfrastructureSettings Infrastructure { get; }
    public DockerSettings Docker { get; }
    public ApiSettings Api { get; }
    public SecuritySettings Security { get; }
    public LoggingSettings Logging { get; }
    public CertificateSettings Certificate { get; }
    public DeploymentSettings Deployment { get; }
    public MonitoringSettings Monitoring { get; }
    public OrchestrationSettings Orchestration { get; }
    public SignalRSettings SignalR { get; }
    public ProviderSettings Providers { get; }
    public SecretsSettings Secrets { get; }

    public SettingsConfigurationViewModel(
        IOptionsMonitor<RuntimeSettings> runtimeOptions,
        IOptionsMonitor<InfrastructureSettings> infrastructureOptions,
        IOptionsMonitor<DockerSettings> dockerOptions,
        IOptionsMonitor<ApiSettings> apiOptions,
        IOptionsMonitor<SecuritySettings> securityOptions,
        IOptionsMonitor<LoggingSettings> loggingOptions,
        IOptionsMonitor<CertificateSettings> certificateOptions,
        IOptionsMonitor<DeploymentSettings> deploymentOptions,
        IOptionsMonitor<MonitoringSettings> monitoringOptions,
        IOptionsMonitor<OrchestrationSettings> orchestrationOptions,
        IOptionsMonitor<SignalRSettings> signalROptions,
        IOptionsMonitor<ProviderSettings> providerOptions,
        IOptionsMonitor<SecretsSettings> secretsOptions,
        IConfigurationWriter configurationWriter,
        IProfileManager profileManager)
    {
        this.runtimeOptions = runtimeOptions;
        this.infrastructureOptions = infrastructureOptions;
        this.dockerOptions = dockerOptions;
        this.apiOptions = apiOptions;
        this.securityOptions = securityOptions;
        this.loggingOptions = loggingOptions;
        this.certificateOptions = certificateOptions;
        this.deploymentOptions = deploymentOptions;
        this.monitoringOptions = monitoringOptions;
        this.orchestrationOptions = orchestrationOptions;
        this.signalROptions = signalROptions;
        this.providerOptions = providerOptions;
        this.secretsOptions = secretsOptions;
        this.configurationWriter = configurationWriter;
        this.profileManager = profileManager;

        Runtime = Clone(runtimeOptions.CurrentValue);
        Infrastructure = Clone(infrastructureOptions.CurrentValue);
        Docker = Clone(dockerOptions.CurrentValue);
        Api = Clone(apiOptions.CurrentValue);
        Security = Clone(securityOptions.CurrentValue);
        Logging = Clone(loggingOptions.CurrentValue);
        Certificate = Clone(certificateOptions.CurrentValue);
        Deployment = Clone(deploymentOptions.CurrentValue);
        Monitoring = Clone(monitoringOptions.CurrentValue);
        Orchestration = Clone(orchestrationOptions.CurrentValue);
        SignalR = Clone(signalROptions.CurrentValue);
        Providers = Clone(providerOptions.CurrentValue);
        Secrets = Clone(secretsOptions.CurrentValue);

        ActiveProfile = profileManager.ActiveProfile;
        SelectedProfile = profileManager.ActiveProfile;
        foreach (var profile in profileManager.AvailableProfiles)
        {
            AvailableProfiles.Add(profile);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await configurationWriter.SaveRuntimeSettingsAsync(Runtime);
        await configurationWriter.SaveInfrastructureSettingsAsync(Infrastructure);
        await configurationWriter.SaveDockerSettingsAsync(Docker);
        await configurationWriter.SaveApiSettingsAsync(Api);
        await configurationWriter.SaveSecuritySettingsAsync(Security);
        await configurationWriter.SaveLoggingSettingsAsync(Logging);
        await configurationWriter.SaveCertificateSettingsAsync(Certificate);
        await configurationWriter.SaveDeploymentSettingsAsync(Deployment);
        await configurationWriter.SaveMonitoringSettingsAsync(Monitoring);
        await configurationWriter.SaveOrchestrationSettingsAsync(Orchestration);
        await configurationWriter.SaveSignalRSettingsAsync(SignalR);
        await configurationWriter.SaveProviderSettingsAsync(Providers);
        StatusMessage = "Configuration saved to active profile.";
    }

    [RelayCommand]
    private async Task ActivateProfileAsync()
    {
        await profileManager.ActivateProfileAsync(SelectedProfile);
        ActiveProfile = SelectedProfile;
        StatusMessage = $"Profile '{SelectedProfile}' activated. Runtime connection profiles: Local / Demo / UAT / Production. Restart the Operations Console to apply connection changes.";
    }

    private static T Clone<T>(T source) where T : new()
    {
        var clone = new T();
        foreach (var property in typeof(T).GetProperties().Where(property => property.CanRead && property.CanWrite))
        {
            property.SetValue(clone, property.GetValue(source));
        }

        return clone;
    }
}
