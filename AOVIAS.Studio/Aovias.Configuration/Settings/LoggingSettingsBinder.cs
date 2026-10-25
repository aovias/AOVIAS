using Aovias.Configuration.Loading;
using Microsoft.Extensions.Configuration;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Binds, validates, and prepares logging settings for runtime bootstrap.
/// </summary>
public static class LoggingSettingsBinder
{
    public const string ExpectedSettingName = "logRoot";

    /// <summary>
    /// Binds logging settings using the options pattern.
    /// </summary>
    public static LoggingSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = configuration.GetSection(LoggingSettings.SectionName).Get<LoggingSettings>();
        return settings ?? new LoggingSettings();
    }

    /// <summary>
    /// Validates logging settings, creates the log directory, and returns resolved values.
    /// </summary>
    public static ResolvedLoggingSettings BindAndPrepare(
        IConfiguration configuration,
        AoviasPlatformBootstrapOptions? bootstrap = null)
    {
        var settings = Bind(configuration);
        var configurationFilePath = ResolveConfigurationFilePath(bootstrap);
        var resolvedLogRoot = ResolveLogRoot(settings);

        if (string.IsNullOrWhiteSpace(resolvedLogRoot))
        {
            throw CreateInvalidConfigurationException("<empty>", configurationFilePath);
        }

        if (!IsValidLogRoot(resolvedLogRoot))
        {
            throw CreateInvalidConfigurationException(resolvedLogRoot, configurationFilePath, invalidPath: true);
        }

        try
        {
            Directory.CreateDirectory(resolvedLogRoot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new AoviasConfigurationBootstrapException(
                $"Logging configuration is invalid.{Environment.NewLine}{Environment.NewLine}" +
                $"Expected setting:{Environment.NewLine}{ExpectedSettingName}{Environment.NewLine}{Environment.NewLine}" +
                $"Resolved value:{Environment.NewLine}{resolvedLogRoot}{Environment.NewLine}{Environment.NewLine}" +
                $"Configuration file:{Environment.NewLine}{configurationFilePath ?? "<unknown>"}{Environment.NewLine}{Environment.NewLine}" +
                $"Unable to create log directory: {exception.Message}",
                bootstrap,
                resolvedLogRoot);
        }

        return new ResolvedLoggingSettings
        {
            LogDirectory = resolvedLogRoot,
            FileNamePrefix = ResolveFileNamePrefix(settings),
            RetainedFileCountLimit = ResolveRetentionDays(settings),
            MinimumLevel = ResolveMinimumLevel(settings),
            ConfigurationFilePath = configurationFilePath
        };
    }

    /// <summary>
    /// Validates that at least one supported log root setting is present.
    /// </summary>
    public static bool IsValid(LoggingSettings settings)
        => !string.IsNullOrWhiteSpace(ResolveLogRoot(settings));

    internal static string ResolveLogRoot(LoggingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!string.IsNullOrWhiteSpace(settings.LogRoot))
        {
            return settings.LogRoot.Trim();
        }

        if (!string.IsNullOrWhiteSpace(settings.LogDirectory))
        {
            return settings.LogDirectory.Trim();
        }

        return string.Empty;
    }

    private static string ResolveFileNamePrefix(LoggingSettings settings)
        => string.IsNullOrWhiteSpace(settings.FileNamePrefix)
            ? LoggingSettings.DefaultFileNamePrefix
            : settings.FileNamePrefix.Trim();

    private static int ResolveRetentionDays(LoggingSettings settings)
    {
        if (settings.RetentionDays > 0)
        {
            return settings.RetentionDays;
        }

        if (settings.RetainedFileCountLimit > 0)
        {
            return settings.RetainedFileCountLimit;
        }

        return LoggingSettings.DefaultRetentionDays;
    }

    private static string ResolveMinimumLevel(LoggingSettings settings)
        => string.IsNullOrWhiteSpace(settings.MinimumLevel)
            ? "Information"
            : settings.MinimumLevel.Trim();

    private static string? ResolveConfigurationFilePath(AoviasPlatformBootstrapOptions? bootstrap)
    {
        if (bootstrap is null)
        {
            return null;
        }

        return Path.Combine(
            bootstrap.ConfigRoot,
            bootstrap.ProfilesDirectory,
            bootstrap.ActiveProfile,
            "settings.logging.json");
    }

    private static bool IsValidLogRoot(string logRoot)
    {
        if (logRoot.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return false;
        }

        try
        {
            _ = Path.GetFullPath(logRoot);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static AoviasConfigurationBootstrapException CreateInvalidConfigurationException(
        string resolvedValue,
        string? configurationFilePath,
        bool invalidPath = false)
    {
        var reason = invalidPath
            ? "The resolved log path is invalid."
            : "The log root setting is missing or empty.";

        return new AoviasConfigurationBootstrapException(
            $"Logging configuration is invalid. {reason}{Environment.NewLine}{Environment.NewLine}" +
            $"Expected setting:{Environment.NewLine}{ExpectedSettingName}{Environment.NewLine}{Environment.NewLine}" +
            $"Resolved value:{Environment.NewLine}{resolvedValue}{Environment.NewLine}{Environment.NewLine}" +
            $"Configuration file:{Environment.NewLine}{configurationFilePath ?? "<unknown>"}");
    }
}
