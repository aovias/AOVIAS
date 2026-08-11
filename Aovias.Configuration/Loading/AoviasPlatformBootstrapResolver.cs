using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;

namespace Aovias.Configuration.Loading;

/// <summary>
/// Resolves environment-neutral AOVIAS platform bootstrap settings.
/// </summary>
public static class AoviasPlatformBootstrapResolver
{
    public const string ConfigRootEnvironmentVariable = "AOVIAS_CONFIG_ROOT";
    public const string ActiveProfileEnvironmentVariable = "AOVIAS_ACTIVE_PROFILE";
    public const string ProfilesDirectoryEnvironmentVariable = "AOVIAS_PROFILES_DIRECTORY";
    public const string SecretsDirectoryEnvironmentVariable = "AOVIAS_SECRETS_DIRECTORY";

    public const string DefaultConfigRoot = "/opt/aovias/runtime/config";
    public const string DefaultActiveProfile = "default";
    public const string DefaultProfilesDirectory = "profiles";
    public const string DefaultSecretsDirectory = "secrets";

    /// <summary>
    /// Resolves bootstrap settings using environment variables, bootstrap configuration, then safe defaults.
    /// </summary>
    public static AoviasPlatformBootstrapOptions Resolve(IConfiguration bootstrapConfiguration)
    {
        ArgumentNullException.ThrowIfNull(bootstrapConfiguration);

        var section = AoviasPlatformOptions.SectionName;
        var configRoot = ResolveValue(
            ConfigRootEnvironmentVariable,
            bootstrapConfiguration,
            $"{section}:{nameof(AoviasPlatformOptions.ConfigRoot)}",
            DefaultConfigRoot,
            normalizePath: true);

        var activeProfile = ResolveValue(
            ActiveProfileEnvironmentVariable,
            bootstrapConfiguration,
            $"{section}:{nameof(AoviasPlatformOptions.ActiveProfile)}",
            DefaultActiveProfile,
            normalizePath: false);

        var profilesDirectory = ResolveValue(
            ProfilesDirectoryEnvironmentVariable,
            bootstrapConfiguration,
            $"{section}:{nameof(AoviasPlatformOptions.ProfilesDirectory)}",
            DefaultProfilesDirectory,
            normalizePath: false);

        var secretsDirectory = ResolveValue(
            SecretsDirectoryEnvironmentVariable,
            bootstrapConfiguration,
            $"{section}:{nameof(AoviasPlatformOptions.SecretsDirectory)}",
            DefaultSecretsDirectory,
            normalizePath: false);

        return new AoviasPlatformBootstrapOptions(
            configRoot,
            activeProfile,
            profilesDirectory,
            secretsDirectory);
    }

    /// <summary>
    /// Validates bootstrap paths before profile configuration is loaded.
    /// </summary>
    public static void Validate(AoviasPlatformBootstrapOptions bootstrap)
    {
        ArgumentNullException.ThrowIfNull(bootstrap);

        if (!Directory.Exists(bootstrap.ConfigRoot))
        {
            throw new AoviasConfigurationBootstrapException(
                BuildMissingConfigRootMessage(bootstrap),
                bootstrap,
                bootstrap.ConfigRoot);
        }

        if (!Directory.Exists(bootstrap.ProfilesPath))
        {
            throw new AoviasConfigurationBootstrapException(
                BuildMissingProfilesDirectoryMessage(bootstrap),
                bootstrap,
                bootstrap.ProfilesPath);
        }

        if (!Directory.Exists(bootstrap.ActiveProfilePath))
        {
            throw new AoviasConfigurationBootstrapException(
                BuildMissingActiveProfileMessage(bootstrap),
                bootstrap,
                bootstrap.ActiveProfilePath);
        }
    }

    /// <summary>
    /// Resolves the configuration root path from bootstrap configuration and environment.
    /// </summary>
    public static string ResolveConfigRoot(IConfiguration bootstrapConfiguration)
        => Resolve(bootstrapConfiguration).ConfigRoot;

    /// <summary>
    /// Resolves the active profile from bootstrap configuration and environment.
    /// </summary>
    public static string ResolveActiveProfile(IConfiguration bootstrapConfiguration)
        => Resolve(bootstrapConfiguration).ActiveProfile;

    /// <summary>
    /// Resolves the profiles directory name from bootstrap configuration and environment.
    /// </summary>
    public static string ResolveProfilesDirectory(IConfiguration bootstrapConfiguration)
        => Resolve(bootstrapConfiguration).ProfilesDirectory;

    /// <summary>
    /// Resolves the secrets directory name from bootstrap configuration and environment.
    /// </summary>
    public static string ResolveSecretsDirectory(IConfiguration bootstrapConfiguration)
        => Resolve(bootstrapConfiguration).SecretsDirectory;

    internal static string ResolveValue(
        string environmentVariable,
        IConfiguration bootstrapConfiguration,
        string configurationKey,
        string defaultValue,
        bool normalizePath)
    {
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return normalizePath
                ? NormalizeConfigRoot(environmentValue)
                : environmentValue.Trim();
        }

        var configuredValue = bootstrapConfiguration[configurationKey];
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            return normalizePath
                ? NormalizeConfigRoot(configuredValue)
                : configuredValue.Trim();
        }

        return normalizePath
            ? NormalizeConfigRoot(defaultValue)
            : defaultValue;
    }

    /// <summary>
    /// Normalizes configuration root paths, preserving Linux absolute paths.
    /// </summary>
    public static string NormalizeConfigRoot(string path)
    {
        var trimmed = path.Trim();
        if (trimmed.StartsWith('/'))
        {
            return trimmed.TrimEnd('/');
        }

        return Path.GetFullPath(trimmed);
    }

    private static string BuildMissingConfigRootMessage(AoviasPlatformBootstrapOptions bootstrap)
        => $"""
            Configuration root not found:
            {bootstrap.ConfigRoot}

            Active profile:
            {bootstrap.ActiveProfile}

            Expected profiles directory:
            {bootstrap.ProfilesPath}

            Runtime Agent cannot continue.
            """;

    private static string BuildMissingProfilesDirectoryMessage(AoviasPlatformBootstrapOptions bootstrap)
        => $"""
            Configuration root:
            {bootstrap.ConfigRoot}

            Profiles directory not found:
            {bootstrap.ProfilesPath}

            Active profile:
            {bootstrap.ActiveProfile}

            Runtime Agent cannot continue.
            """;

    private static string BuildMissingActiveProfileMessage(AoviasPlatformBootstrapOptions bootstrap)
        => $"""
            Configuration root:
            {bootstrap.ConfigRoot}

            Active profile:
            {bootstrap.ActiveProfile}

            Expected:
            {bootstrap.ActiveProfilePath}

            Runtime Agent cannot continue.
            """;
}
