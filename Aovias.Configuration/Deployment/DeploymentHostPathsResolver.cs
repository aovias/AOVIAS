using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;

namespace Aovias.Configuration.Deployment;

/// <summary>
/// Resolves deployment host paths from the selected publishing profile.
/// </summary>
public static class DeploymentHostPathsResolver
{
    /// <summary>
    /// Resolves deployment host settings for the active publishing target profile.
    /// </summary>
    public static DeploymentHostSettings Resolve(PublishingSettings publishingSettings)
    {
        ArgumentNullException.ThrowIfNull(publishingSettings);

        var profileName = publishingSettings.DeploymentActiveProfile;
        var configRoot = publishingSettings.ValidationConfigRoot;
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new InvalidOperationException("Publishing deploymentActiveProfile is required to resolve deployment host paths.");
        }

        if (string.IsNullOrWhiteSpace(configRoot))
        {
            throw new InvalidOperationException("Publishing validationConfigRoot is required to resolve deployment host paths.");
        }

        var profileDirectory = Path.Combine(configRoot, "profiles", profileName);
        var deploymentHostFile = Path.Combine(profileDirectory, "settings.deployment-host.json");
        if (!File.Exists(deploymentHostFile))
        {
            throw new FileNotFoundException(
                $"Deployment host settings were not found for profile '{profileName}': {deploymentHostFile}");
        }

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(deploymentHostFile, optional: false)
            .Build();

        var settings = configuration.Get<DeploymentHostSettings>()
                       ?? throw new InvalidOperationException(
                           $"Deployment host settings could not be loaded for profile '{profileName}'.");

        ValidateResolvedSettings(settings, profileName);
        return settings;
    }

    /// <summary>
    /// Loads deployment host settings from a configuration root and profile name.
    /// </summary>
    public static DeploymentHostSettings Resolve(string validationConfigRoot, string deploymentProfile)
    {
        return Resolve(new PublishingSettings
        {
            ValidationConfigRoot = validationConfigRoot,
            DeploymentActiveProfile = deploymentProfile,
            DefaultNetworkName = "aovias-network"
        });
    }

    private static void ValidateResolvedSettings(DeploymentHostSettings settings, string profileName)
    {
        var errors = new List<string>();

        if (ContainsPlaceholder(settings.ConfigRoot))
        {
            errors.Add(nameof(settings.ConfigRoot));
        }

        if (ContainsPlaceholder(settings.PackagesRoot))
        {
            errors.Add(nameof(settings.PackagesRoot));
        }

        if (ContainsPlaceholder(settings.LogsRoot))
        {
            errors.Add(nameof(settings.LogsRoot));
        }

        if (ContainsPlaceholder(settings.HostConfigRoot))
        {
            errors.Add(nameof(settings.HostConfigRoot));
        }

        if (ContainsPlaceholder(settings.HostPackagesRoot))
        {
            errors.Add(nameof(settings.HostPackagesRoot));
        }

        if (ContainsPlaceholder(settings.HostLogsRoot))
        {
            errors.Add(nameof(settings.HostLogsRoot));
        }

        if (ContainsPlaceholder(settings.DockerNetwork))
        {
            errors.Add(nameof(settings.DockerNetwork));
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Deployment host settings for profile '{profileName}' contain unresolved placeholders: {string.Join(", ", errors)}.");
        }
    }

    public static bool ContainsPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Contains("/path/to/", StringComparison.OrdinalIgnoreCase)
               || value.Contains("{{", StringComparison.Ordinal)
               || value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
    }
}
