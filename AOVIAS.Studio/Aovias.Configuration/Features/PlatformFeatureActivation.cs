using Aovias.Configuration.Features;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;

namespace Aovias.Configuration.Features;

/// <summary>
/// Resolved platform feature activation used for modular configuration and service registration.
/// </summary>
public sealed class PlatformFeatureActivation
{
    public bool Runtime { get; init; } = true;

    public bool ReleaseManagement { get; init; }

    public bool DockerProvider { get; init; }

    public bool SignalR { get; init; }

    public bool Publishing { get; init; }

    public bool Certificates { get; init; }

    public bool Monitoring { get; init; }

    public bool Observability { get; init; }

    public bool Ai { get; init; }

    public bool RequiresSecretsValidation
        => Publishing || Certificates;

    /// <summary>
    /// Resolves feature activation from explicit feature flags or legacy module enabled settings.
    /// </summary>
    public static PlatformFeatureActivation Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var featuresSection = configuration.GetSection(PlatformFeatureSettings.SectionName);
        if (featuresSection.Exists() && featuresSection.GetChildren().Any())
        {
            var explicitFeatures = featuresSection.Get<PlatformFeatureSettings>() ?? new PlatformFeatureSettings();
            return FromSettings(explicitFeatures);
        }

        return InferFromConfiguration(configuration);
    }

    /// <summary>
    /// Resolves runtime-agent feature activation with safe defaults for optional modules.
    /// Optional modules remain disabled unless explicitly enabled in <c>settings.features.json</c>.
    /// </summary>
    public static PlatformFeatureActivation ResolveRuntimeAgent(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var featuresSection = configuration.GetSection(PlatformFeatureSettings.SectionName);
        if (featuresSection.Exists() && featuresSection.GetChildren().Any())
        {
            var explicitFeatures = featuresSection.Get<PlatformFeatureSettings>() ?? new PlatformFeatureSettings();
            return FromSettings(explicitFeatures);
        }

        return InferRuntimeAgentCoreCapabilities(configuration);
    }

    private static PlatformFeatureActivation FromSettings(PlatformFeatureSettings settings)
        => new()
        {
            Runtime = settings.Runtime,
            ReleaseManagement = settings.ReleaseManagement,
            DockerProvider = settings.DockerProvider,
            SignalR = settings.SignalR,
            Publishing = settings.Publishing,
            Certificates = settings.Certificates,
            Monitoring = settings.Monitoring,
            Observability = settings.Observability,
            Ai = settings.Ai
        };

    private static PlatformFeatureActivation InferFromConfiguration(IConfiguration configuration)
    {
        var dockerEnabled = configuration.GetValue<bool>($"{ProviderSettings.SectionName}:DockerEnabled")
                            || configuration.GetValue<bool>($"{DockerSettings.SectionName}:Enabled");
        var signalREnabled = configuration.GetValue<bool>($"{SignalRSettings.SectionName}:Enabled");
        var publishingEnabled = HasPublishingConfiguration(configuration);
        var certificatesEnabled = configuration.GetValue<bool>($"{ProviderSettings.SectionName}:CertificateEnabled")
                                  || configuration.GetValue<bool>($"{CertificateSettings.SectionName}:Enabled");
        var monitoringEnabled = configuration.GetValue<bool>($"{MonitoringSettings.SectionName}:Enabled");
        var observabilityEnabled = configuration.GetValue<bool>($"{ObservabilitySettings.SectionName}:Enabled");
        var releaseEnabled = configuration.GetValue<bool>($"{ReleaseManagementSettings.SectionName}:Enabled")
                               || configuration.GetValue<bool>($"{DeploymentSettings.SectionName}:Enabled");

        return new PlatformFeatureActivation
        {
            Runtime = true,
            ReleaseManagement = releaseEnabled,
            DockerProvider = dockerEnabled,
            SignalR = signalREnabled,
            Publishing = publishingEnabled,
            Certificates = certificatesEnabled,
            Monitoring = monitoringEnabled,
            Observability = observabilityEnabled
        };
    }

    private static PlatformFeatureActivation InferRuntimeAgentCoreCapabilities(IConfiguration configuration)
    {
        var dockerEnabled = configuration.GetValue<bool>($"{ProviderSettings.SectionName}:DockerEnabled")
                            || configuration.GetValue<bool>($"{DockerSettings.SectionName}:Enabled");
        var signalREnabled = configuration.GetValue<bool>($"{SignalRSettings.SectionName}:Enabled");
        var releaseEnabled = configuration.GetValue<bool>($"{ReleaseManagementSettings.SectionName}:Enabled")
                               || configuration.GetValue<bool>($"{DeploymentSettings.SectionName}:Enabled");

        return new PlatformFeatureActivation
        {
            Runtime = true,
            ReleaseManagement = releaseEnabled,
            DockerProvider = dockerEnabled,
            SignalR = signalREnabled,
            Publishing = false,
            Certificates = false,
            Monitoring = false,
            Observability = false,
            Ai = false
        };
    }

    private static bool HasPublishingConfiguration(IConfiguration configuration)
    {
        var publishRoot = configuration[$"{PublishingSettings.SectionName}:PublishRoot"];
        return !string.IsNullOrWhiteSpace(publishRoot);
    }
}
