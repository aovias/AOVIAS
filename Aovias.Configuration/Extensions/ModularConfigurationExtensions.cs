using Aovias.Configuration.Deployment;
using Aovias.Configuration.Features;
using Aovias.Configuration.Secrets;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aovias.Configuration.Extensions;

/// <summary>
/// Feature-aware modular configuration registration extensions.
/// </summary>
public static class ModularConfigurationExtensions
{
    public static IServiceCollection AddRuntimeConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation features)
    {
        if (!features.Runtime)
        {
            return services;
        }

        RegisterValidatedModuleOptions<RuntimeSettings>(services, configuration, RuntimeSettings.SectionName);
        RegisterValidatedModuleOptions<StorageSettings>(services, configuration, StorageSettings.SectionName);
        RegisterValidatedModuleOptions<RuntimeManifestSettings>(services, configuration, RuntimeManifestSettings.SectionName);
        RegisterValidatedLoggingOptions(services, configuration);
        RegisterValidatedModuleOptions<ApiSettings>(services, configuration, ApiSettings.SectionName);
        RegisterValidatedModuleOptions<InfrastructureSettings>(services, configuration, InfrastructureSettings.SectionName);
        RegisterValidatedSecurityOptions(services, configuration);
        return services;
    }

    public static IServiceCollection AddReleaseConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation features)
    {
        if (!features.ReleaseManagement)
        {
            return services;
        }

        services.AddOptions<ReleaseManagementSettings>()
            .Bind(configuration.GetSection(ReleaseManagementSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.ReleaseRoot)
                            && !string.IsNullOrWhiteSpace(settings.IncomingFolder),
                "Release management paths must be resolved from DeploymentStorage settings.")
            .ValidateOnStart();
        RegisterValidatedModuleOptions<DeploymentSettings>(services, configuration, DeploymentSettings.SectionName);
        services.AddOptions<DeploymentStorageSettings>()
            .Bind(configuration.GetSection(DeploymentStorageSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.Root),
                "DeploymentStorage:Root must be configured in settings.deployment-storage.json or derivable from ReleaseManagement:ReleaseRoot.")
            .ValidateOnStart();
        services.AddSingleton<IConfigureOptions<DeploymentStorageSettings>, DeploymentStorageOptionsConfigurator>();
        RegisterValidatedModuleOptions<DeploymentHostSettings>(services, configuration, DeploymentHostSettings.SectionName);
        RegisterValidatedModuleOptions<OrchestrationSettings>(services, configuration, OrchestrationSettings.SectionName);
        RegisterValidatedModuleOptions<RemoteDeploymentSettings>(services, configuration, RemoteDeploymentSettings.SectionName);
        RegisterValidatedEnvironmentRegistrationOptions(services, configuration);
        services.AddSingleton<IDeploymentPathResolver, ConfiguredDeploymentPathResolver>();
        services.AddSingleton<IConfigureOptions<ReleaseManagementSettings>, ReleaseManagementPathOptionsConfigurator>();
        return services;
    }

    public static IServiceCollection AddDockerConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation features)
    {
        if (!features.DockerProvider)
        {
            return services;
        }

        RegisterValidatedModuleOptions<DockerSettings>(services, configuration, DockerSettings.SectionName);
        RegisterValidatedModuleOptions<ProviderSettings>(services, configuration, ProviderSettings.SectionName);
        RegisterValidatedNetworkOptions(services, configuration);
        return services;
    }

    public static IServiceCollection AddSignalRConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation features)
    {
        if (!features.SignalR)
        {
            return services;
        }

        RegisterValidatedModuleOptions<SignalRSettings>(services, configuration, SignalRSettings.SectionName);
        return services;
    }

    public static IServiceCollection AddPublishingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation features)
    {
        if (!features.Publishing)
        {
            return services;
        }

        RegisterValidatedModuleOptions<PublishingSettings>(services, configuration, PublishingSettings.SectionName);
        return services;
    }

    public static IServiceCollection AddMonitoringConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation features)
    {
        if (!features.Monitoring)
        {
            return services;
        }

        RegisterValidatedModuleOptions<MonitoringSettings>(services, configuration, MonitoringSettings.SectionName);
        return services;
    }

    public static IServiceCollection AddCertificateConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation features)
    {
        if (!features.Certificates)
        {
            return services;
        }

        RegisterValidatedModuleOptions<CertificateSettings>(services, configuration, CertificateSettings.SectionName);
        RegisterValidatedModuleOptions<NginxSettings>(services, configuration, NginxSettings.SectionName);
        return services;
    }

    public static IServiceCollection AddObservabilityConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation features)
    {
        if (!features.Observability)
        {
            return services;
        }

        RegisterValidatedModuleOptions<ObservabilitySettings>(services, configuration, ObservabilitySettings.SectionName);
        return services;
    }

    public static IServiceCollection AddSecretsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation features)
    {
        if (!features.RequiresSecretsValidation)
        {
            return services;
        }

        services.AddOptions<SecretsSettings>()
            .Bind(configuration.GetSection(SecretsSettings.SectionName))
            .Validate(
                secrets => ValidateSecrets(secrets, features),
                "Secrets configuration is incomplete for enabled platform features.")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    private static void RegisterValidatedModuleOptions<TOptions>(
        IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void RegisterValidatedLoggingOptions(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<LoggingSettings>()
            .Bind(configuration.GetSection(LoggingSettings.SectionName))
            .Validate(LoggingSettingsBinder.IsValid, "Logging settings must define logRoot or logDirectory.")
            .ValidateOnStart();
    }

    private static void RegisterValidatedNetworkOptions(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<NetworkSettings>()
            .Bind(configuration.GetSection(NetworkSettings.SectionName))
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.DockerNetwork),
                "Network settings must define dockerNetwork when the Docker provider is enabled.")
            .ValidateOnStart();
    }

    /// <summary>Sprint M4.3: <see cref="EnvironmentRegistrationSettings"/> is only meaningfully required when <c>Enabled</c> — see that class's remarks. A plain <c>RegisterValidatedModuleOptions</c> call would force every profile (including ones that never enable this feature) to supply a Command Center address.</summary>
    private static void RegisterValidatedEnvironmentRegistrationOptions(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EnvironmentRegistrationSettings>()
            .Bind(configuration.GetSection(EnvironmentRegistrationSettings.SectionName))
            .Validate(
                settings => !settings.Enabled
                            || (!string.IsNullOrWhiteSpace(settings.CommandCenterBaseUrl)
                                && !string.IsNullOrWhiteSpace(settings.RegistrationToken)),
                "EnvironmentRegistration:CommandCenterBaseUrl and RegistrationToken are required when EnvironmentRegistration:Enabled is true.")
            .ValidateOnStart();
    }

    private static void RegisterValidatedSecurityOptions(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SecuritySettings>()
            .Bind(configuration.GetSection(SecuritySettings.SectionName))
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.JwtIssuer)
                            && !string.IsNullOrWhiteSpace(settings.JwtAudience),
                "Security settings must define JWT issuer and audience when the Runtime module is enabled.")
            .ValidateOnStart();
    }

    private static bool ValidateSecrets(SecretsSettings secrets, PlatformFeatureActivation features)
    {
        if (features.Publishing && string.IsNullOrWhiteSpace(secrets.DockerRegistryPassword))
        {
            return false;
        }

        if (features.Certificates && string.IsNullOrWhiteSpace(secrets.CertificatePassword))
        {
            return false;
        }

        return true;
    }
}
