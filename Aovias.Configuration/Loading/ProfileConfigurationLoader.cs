using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Aovias.Configuration.Loading;

/// <summary>
/// Loads profile-scoped JSON settings files into configuration using section mapping conventions.
/// </summary>
public static class ProfileConfigurationLoader
{
    private static readonly IReadOnlyDictionary<string, string> FileSectionMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["settings.runtime.json"] = Settings.RuntimeSettings.SectionName,
            ["settings.infrastructure.json"] = Settings.InfrastructureSettings.SectionName,
            ["settings.docker.json"] = Settings.DockerSettings.SectionName,
            ["settings.nginx.json"] = Settings.NginxSettings.SectionName,
            ["settings.certificate.json"] = Settings.CertificateSettings.SectionName,
            ["settings.deployment.json"] = Settings.DeploymentSettings.SectionName,
            ["settings.monitoring.json"] = Settings.MonitoringSettings.SectionName,
            ["settings.logging.json"] = Settings.LoggingSettings.SectionName,
            ["settings.security.json"] = Settings.SecuritySettings.SectionName,
            ["settings.api.json"] = Settings.ApiSettings.SectionName,
            ["settings.network.json"] = Settings.NetworkSettings.SectionName,
            ["settings.storage.json"] = Settings.StorageSettings.SectionName,
            ["settings.runtime-manifest.json"] = Settings.RuntimeManifestSettings.SectionName,
            ["settings.release-management.json"] = Settings.ReleaseManagementSettings.SectionName,
            ["settings.orchestration.json"] = Settings.OrchestrationSettings.SectionName,
            ["settings.signalr.json"] = Settings.SignalRSettings.SectionName,
            ["settings.providers.json"] = Settings.ProviderSettings.SectionName,
            ["settings.observability.json"] = Settings.ObservabilitySettings.SectionName,
            ["settings.publishing.json"] = Settings.PublishingSettings.SectionName,
            ["settings.features.json"] = Settings.PlatformFeatureSettings.SectionName,
            ["settings.deployment-host.json"] = Settings.DeploymentHostSettings.SectionName,
            ["settings.deployment-storage.json"] = Settings.DeploymentStorageSettings.SectionName,
            // Sprint M3.3: EnvironmentRegistrationSettings (Sprint M4.3) was never added to this map,
            // so no settings.*.json file could ever populate it via the profile-configuration
            // convention every other settings class uses — the only way to enable automatic
            // environment self-registration was environment variables or a hand-edited
            // appsettings.{env}.json. Genuine integration gap found tracing the Bootstrap ->
            // Runtime Agent -> Registration pipeline end to end; fixed here alongside
            // RuntimeConfigurationScaffoldBuilder actually generating the file (see that class).
            ["settings.environment-registration.json"] = Settings.EnvironmentRegistrationSettings.SectionName,
            ["settings.secrets.json"] = Secrets.SecretsSettings.SectionName
        };

    /// <summary>
    /// Adds profile-based configuration sources to the builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="bootstrap">Resolved bootstrap settings.</param>
    /// <returns>The configuration builder.</returns>
    public static IConfigurationBuilder AddAoviasProfileConfiguration(
        this IConfigurationBuilder builder,
        AoviasPlatformBootstrapOptions bootstrap)
    {
        ArgumentNullException.ThrowIfNull(bootstrap);
        return builder.AddAoviasProfileConfiguration(
            bootstrap.ConfigRoot,
            bootstrap.ActiveProfile,
            bootstrap.ProfilesDirectory,
            bootstrap.SecretsDirectory);
    }

    /// <summary>
    /// Adds profile-based configuration sources to the builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configRootPath">The AOVIAS configuration root path.</param>
    /// <param name="activeProfile">The active profile name.</param>
    /// <param name="profilesDirectory">The profiles directory name relative to the configuration root.</param>
    /// <param name="secretsDirectory">The secrets directory name relative to the configuration root.</param>
    /// <returns>The configuration builder.</returns>
    public static IConfigurationBuilder AddAoviasProfileConfiguration(
        this IConfigurationBuilder builder,
        string configRootPath,
        string activeProfile,
        string profilesDirectory = AoviasPlatformBootstrapResolver.DefaultProfilesDirectory,
        string secretsDirectory = AoviasPlatformBootstrapResolver.DefaultSecretsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeProfile);
        ArgumentException.ThrowIfNullOrWhiteSpace(profilesDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretsDirectory);

        var bootstrap = new AoviasPlatformBootstrapOptions(
            configRootPath,
            activeProfile,
            profilesDirectory,
            secretsDirectory);

        AoviasPlatformBootstrapResolver.Validate(bootstrap);

        var profileDirectory = bootstrap.ActiveProfilePath;
        foreach (var mapping in FileSectionMap)
        {
            var filePath = Path.Combine(profileDirectory, mapping.Key);
            if (!File.Exists(filePath))
            {
                continue;
            }

            builder.Add(new SectionJsonConfigurationSource
            {
                Path = filePath,
                Section = mapping.Value,
                Optional = false,
                ReloadOnChange = true
            });
        }

        var secretsFile = Path.Combine(bootstrap.SecretsPath, "settings.secrets.json");
        if (File.Exists(secretsFile))
        {
            builder.Add(new SectionJsonConfigurationSource
            {
                Path = secretsFile,
                Section = Secrets.SecretsSettings.SectionName,
                Optional = true,
                ReloadOnChange = true
            });
        }

        return builder;
    }

    /// <summary>
    /// Resolves the configuration root path from bootstrap configuration and environment.
    /// </summary>
    /// <param name="bootstrapConfiguration">The bootstrap configuration.</param>
    /// <returns>The resolved configuration root path.</returns>
    public static string ResolveConfigRoot(IConfiguration bootstrapConfiguration)
        => AoviasPlatformBootstrapResolver.ResolveConfigRoot(bootstrapConfiguration);

    /// <summary>
    /// Resolves the active profile from bootstrap configuration and environment.
    /// </summary>
    /// <param name="bootstrapConfiguration">The bootstrap configuration.</param>
    /// <returns>The active profile name.</returns>
    public static string ResolveActiveProfile(IConfiguration bootstrapConfiguration)
        => AoviasPlatformBootstrapResolver.ResolveActiveProfile(bootstrapConfiguration);

    /// <summary>
    /// Lists available profile names under the configuration root.
    /// </summary>
    /// <param name="configRootPath">The configuration root path.</param>
    /// <param name="profilesDirectory">The profiles directory name relative to the configuration root.</param>
    /// <returns>Available profile names.</returns>
    public static IReadOnlyList<string> ListProfiles(
        string configRootPath,
        string profilesDirectory = AoviasPlatformBootstrapResolver.DefaultProfilesDirectory)
    {
        var profilesPath = Path.Combine(configRootPath, profilesDirectory);
        if (!Directory.Exists(profilesPath))
        {
            return [];
        }

        return Directory.GetDirectories(profilesPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

internal sealed class SectionJsonConfigurationSource : IConfigurationSource
{
    public required string Path { get; init; }

    public required string Section { get; init; }

    public bool Optional { get; init; }

    public bool ReloadOnChange { get; init; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new SectionJsonConfigurationProvider(this);
}

internal sealed class SectionJsonConfigurationProvider : ConfigurationProvider
{
    private readonly SectionJsonConfigurationSource source;

    public SectionJsonConfigurationProvider(SectionJsonConfigurationSource source)
    {
        this.source = source;
    }

    public override void Load()
    {
        if (!File.Exists(source.Path))
        {
            if (source.Optional)
            {
                return;
            }

            throw new FileNotFoundException($"Required configuration file was not found: {source.Path}");
        }

        using var stream = File.OpenRead(source.Path);
        using var document = JsonDocument.Parse(stream);
        var flattened = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        FlattenJson(document.RootElement, source.Section, flattened);

        Data.Clear();
        foreach (var entry in flattened)
        {
            Data[entry.Key] = entry.Value ?? string.Empty;
        }
    }

    private static void FlattenJson(JsonElement element, string prefix, IDictionary<string, string?> data)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                    FlattenJson(property.Value, key, data);
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenJson(item, $"{prefix}:{index}", data);
                    index++;
                }

                break;
            default:
                data[prefix] = element.ToString();
                break;
        }
    }
}
