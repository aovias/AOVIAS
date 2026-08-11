using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;

namespace Aovias.Configuration.Deployment;

/// <summary>
/// Resolves a strongly-typed <see cref="DeploymentProfile"/> from deployment-host
/// settings for deploy-time validation and workspace preparation. Publishing no
/// longer embeds profile values in generated package artifacts.
/// </summary>
public static class DeploymentPathResolver
{
    /// <summary>
    /// Identifiers that must never appear in any deployment host path regardless
    /// of profile (developer repo, user home, personal folders).
    /// </summary>
    private static readonly string[] ForbiddenIdentityFragments =
    [
        "DevelopmentRepo",
        "\\Users\\",
        "/Users/",
        "\\Desktop",
        "/Desktop",
        "\\Downloads",
        "/Downloads",
        "\\Documents",
        "/Documents"
    ];

    /// <summary>
    /// Resolves the deployment profile for the active publishing target.
    /// </summary>
    public static DeploymentProfile Resolve(PublishingSettings publishingSettings)
    {
        ArgumentNullException.ThrowIfNull(publishingSettings);
        return Resolve(publishingSettings.ValidationConfigRoot, publishingSettings.DeploymentActiveProfile);
    }

    /// <summary>
    /// Resolves the deployment profile from a configuration root and profile name.
    /// </summary>
    public static DeploymentProfile Resolve(string configRoot, string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new InvalidOperationException("A deployment profile name is required to resolve deployment paths.");
        }

        if (string.IsNullOrWhiteSpace(configRoot))
        {
            throw new InvalidOperationException("A configuration root is required to resolve deployment paths.");
        }

        var deploymentHostFile = Path.Combine(configRoot, "profiles", profileName, "settings.deployment-host.json");
        if (!File.Exists(deploymentHostFile))
        {
            throw new FileNotFoundException(
                $"Deployment host settings were not found for profile '{profileName}': {deploymentHostFile}");
        }

        var host = new ConfigurationBuilder()
            .AddJsonFile(deploymentHostFile, optional: false)
            .Build()
            .Get<DeploymentHostSettings>()
            ?? throw new InvalidOperationException($"Deployment host settings could not be loaded for profile '{profileName}'.");

        var (deploymentsRoot, hostDeploymentsRoot) = ResolveDeploymentStorageRoots(configRoot, profileName);

        var profile = new DeploymentProfile
        {
            Name = profileName,
            ActiveProfile = profileName,
            DotNetEnvironment = host.DotNetEnvironment,
            ConfigRoot = host.ConfigRoot,
            PackagesRoot = host.PackagesRoot,
            LogsRoot = host.LogsRoot,
            HostConfigRoot = host.HostConfigRoot,
            HostPackagesRoot = host.HostPackagesRoot,
            HostLogsRoot = host.HostLogsRoot,
            DeploymentsRoot = deploymentsRoot,
            HostDeploymentsRoot = hostDeploymentsRoot,
            DockerNetwork = host.DockerNetwork,
            ProfilesDirectory = host.ProfilesDirectory,
            SecretsDirectory = host.SecretsDirectory
        };

        Validate(profile);
        return profile;
    }

    private static (string DeploymentsRoot, string HostDeploymentsRoot) ResolveDeploymentStorageRoots(
        string configRoot,
        string profileName)
    {
        var deploymentStorageFile = Path.Combine(configRoot, "profiles", profileName, "settings.deployment-storage.json");
        var configuredRoot = string.Empty;
        if (File.Exists(deploymentStorageFile))
        {
            var storageConfig = new ConfigurationBuilder()
                .AddJsonFile(deploymentStorageFile, optional: false)
                .Build();
            configuredRoot = ReadSetting(
                storageConfig,
                DeploymentStorageSettings.SectionName,
                nameof(DeploymentStorageSettings.Root),
                "root");
        }

        var legacyReleaseRoot = string.Empty;
        var releaseManagementFile = Path.Combine(configRoot, "profiles", profileName, "settings.release-management.json");
        if (File.Exists(releaseManagementFile))
        {
            var releaseConfig = new ConfigurationBuilder()
                .AddJsonFile(releaseManagementFile, optional: false)
                .Build();
            legacyReleaseRoot = ReadSetting(
                releaseConfig,
                ReleaseManagementSettings.SectionName,
                nameof(ReleaseManagementSettings.ReleaseRoot),
                "releaseRoot");
        }

        var deploymentsRoot = string.IsNullOrWhiteSpace(configuredRoot) && string.IsNullOrWhiteSpace(legacyReleaseRoot)
            ? "/opt/aovias/deployments"
            : ConfiguredDeploymentPathResolver.NormalizeLegacyDeploymentRoot(
                configuredRoot,
                legacyReleaseRoot);

        var hostDeploymentsRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? deploymentsRoot
            : NormalizeHostRoot(configuredRoot, deploymentsRoot);

        return (deploymentsRoot, hostDeploymentsRoot);
    }

    private static string NormalizeHostRoot(string configuredRoot, string normalizedLinuxRoot)
    {
        if (configuredRoot.StartsWith('/') || configuredRoot.StartsWith('\\'))
        {
            return normalizedLinuxRoot;
        }

        return Path.GetFullPath(configuredRoot);
    }

    private static string ReadSetting(
        IConfiguration configuration,
        string sectionName,
        string propertyName,
        params string[] flatKeys)
    {
        var sectionValue = configuration[$"{sectionName}:{propertyName}"];
        if (!string.IsNullOrWhiteSpace(sectionValue))
        {
            return sectionValue;
        }

        foreach (var flatKey in flatKeys)
        {
            var flatValue = configuration[flatKey];
            if (!string.IsNullOrWhiteSpace(flatValue))
            {
                return flatValue;
            }
        }

        return string.Empty;
    }

    private static void Validate(DeploymentProfile profile)
    {
        var errors = new List<string>();

        var named = new (string Name, string Value)[]
        {
            (nameof(profile.DotNetEnvironment), profile.DotNetEnvironment),
            (nameof(profile.ConfigRoot), profile.ConfigRoot),
            (nameof(profile.PackagesRoot), profile.PackagesRoot),
            (nameof(profile.LogsRoot), profile.LogsRoot),
            (nameof(profile.HostConfigRoot), profile.HostConfigRoot),
            (nameof(profile.HostPackagesRoot), profile.HostPackagesRoot),
            (nameof(profile.HostLogsRoot), profile.HostLogsRoot),
            (nameof(profile.DeploymentsRoot), profile.DeploymentsRoot),
            (nameof(profile.HostDeploymentsRoot), profile.HostDeploymentsRoot),
            (nameof(profile.DockerNetwork), profile.DockerNetwork)
        };

        foreach (var (name, value) in named)
        {
            if (DeploymentHostPathsResolver.ContainsPlaceholder(value))
            {
                errors.Add($"{name} is empty or contains an unresolved placeholder.");
                continue;
            }

            foreach (var fragment in ForbiddenIdentityFragments)
            {
                if (value.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"{name} contains forbidden developer/machine identifier '{fragment.Trim('\\', '/')}'.");
                }
            }
        }

        // Container-side roots must always be Linux absolute paths.
        foreach (var (name, value) in new[]
                 {
                     (nameof(profile.ConfigRoot), profile.ConfigRoot),
                     (nameof(profile.PackagesRoot), profile.PackagesRoot),
                     (nameof(profile.LogsRoot), profile.LogsRoot),
                     (nameof(profile.DeploymentsRoot), profile.DeploymentsRoot)
                 })
        {
            if (!value.StartsWith('/'))
            {
                errors.Add($"Container path {name} must be an absolute Linux path but was '{value}'.");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Deployment profile '{profile.Name}' is invalid: {string.Join("; ", errors)}.");
        }
    }
}
