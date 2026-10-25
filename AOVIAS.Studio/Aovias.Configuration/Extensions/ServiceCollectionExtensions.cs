using Aovias.Configuration.Extensions;
using Aovias.Configuration.Features;
using Aovias.Configuration.Loading;
using Aovias.Configuration.Profiles;
using Aovias.Configuration.Secrets;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aovias.Configuration.Extensions;

/// <summary>
/// Dependency injection extensions for the AOVIAS configuration platform.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers strongly typed AOVIAS configuration options using feature-aware modular registration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The merged application configuration.</param>
    /// <param name="features">Optional pre-resolved feature activation.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAoviasConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation? features = null)
    {
        features ??= PlatformFeatureActivation.Resolve(configuration);
        services.AddSingleton(features);

        services.Configure<AoviasPlatformOptions>(configuration.GetSection(AoviasPlatformOptions.SectionName));
        services.AddRuntimeConfiguration(configuration, features);
        services.AddReleaseConfiguration(configuration, features);
        services.AddDockerConfiguration(configuration, features);
        services.AddSignalRConfiguration(configuration, features);
        services.AddPublishingConfiguration(configuration, features);
        services.AddMonitoringConfiguration(configuration, features);
        services.AddCertificateConfiguration(configuration, features);
        services.AddObservabilityConfiguration(configuration, features);
        services.AddSecretsConfiguration(configuration, features);

        services.AddSingleton<IProfileManager, ProfileManager>();
        services.AddSingleton<IConfigurationWriter, ConfigurationWriter>();
        return services;
    }

    /// <summary>
    /// Registers runtime-agent configuration using safe capability defaults for optional modules.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The merged application configuration.</param>
    /// <param name="features">Optional pre-resolved runtime-agent feature activation.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRuntimeAgentConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        PlatformFeatureActivation? features = null)
    {
        features ??= PlatformFeatureActivation.ResolveRuntimeAgent(configuration);
        services.AddSingleton(features);

        services.Configure<AoviasPlatformOptions>(configuration.GetSection(AoviasPlatformOptions.SectionName));
        services.AddRuntimeConfiguration(configuration, features);
        services.AddReleaseConfiguration(configuration, features);
        services.AddDockerConfiguration(configuration, features);
        services.AddSignalRConfiguration(configuration, features);
        services.AddPublishingConfiguration(configuration, features);
        services.AddMonitoringConfiguration(configuration, features);
        services.AddCertificateConfiguration(configuration, features);
        services.AddObservabilityConfiguration(configuration, features);
        services.AddSecretsConfiguration(configuration, features);

        services.AddSingleton<IProfileManager, ProfileManager>();
        services.AddSingleton<IConfigurationWriter, ConfigurationWriter>();
        return services;
    }

    /// <summary>
    /// Builds merged AOVIAS configuration using defined precedence rules.
    /// </summary>
    /// <param name="bootstrapConfiguration">Bootstrap configuration containing Aovias platform keys.</param>
    /// <param name="hostEnvironmentName">The host environment name.</param>
    /// <param name="additionalAppSettingsPaths">Optional appsettings file paths.</param>
    /// <returns>The merged configuration.</returns>
    public static IConfiguration BuildAoviasConfiguration(
        IConfiguration bootstrapConfiguration,
        string hostEnvironmentName,
        params string[] additionalAppSettingsPaths)
    {
        var bootstrap = AoviasPlatformBootstrapResolver.Resolve(bootstrapConfiguration);
        AoviasPlatformBootstrapResolver.Validate(bootstrap);

        var builder = new ConfigurationBuilder();
        builder.AddAoviasProfileConfiguration(bootstrap);

        foreach (var path in additionalAppSettingsPaths.Where(File.Exists))
        {
            builder.AddJsonFile(path, optional: true, reloadOnChange: true);
        }

        var environmentAppSettings = additionalAppSettingsPaths
            .Select(path => Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, $"appsettings.{hostEnvironmentName}.json"))
            .FirstOrDefault(File.Exists);

        if (!string.IsNullOrWhiteSpace(environmentAppSettings))
        {
            builder.AddJsonFile(environmentAppSettings!, optional: true, reloadOnChange: true);
        }

        builder.AddEnvironmentVariables(prefix: "AOVIAS_");
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{AoviasPlatformOptions.SectionName}:{nameof(AoviasPlatformOptions.ConfigRoot)}"] = bootstrap.ConfigRoot,
            [$"{AoviasPlatformOptions.SectionName}:{nameof(AoviasPlatformOptions.ActiveProfile)}"] = bootstrap.ActiveProfile,
            [$"{AoviasPlatformOptions.SectionName}:{nameof(AoviasPlatformOptions.ProfilesDirectory)}"] = bootstrap.ProfilesDirectory,
            [$"{AoviasPlatformOptions.SectionName}:{nameof(AoviasPlatformOptions.SecretsDirectory)}"] = bootstrap.SecretsDirectory
        });

        return builder.Build();
    }
}
