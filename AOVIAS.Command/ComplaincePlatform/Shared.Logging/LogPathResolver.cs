using Microsoft.Extensions.Configuration;

namespace Shared.Logging;

/// <summary>
/// Resolves the root directory under which every service writes its dedicated
/// log folder. The same resolution order must be mirrored by the Compliance
/// Service Manager so it can always locate the files.
///
/// Resolution order:
///   1. Configuration key <c>Logging:FileRoot</c>
///   2. Environment variable <c>COMPLIANCE_LOG_ROOT</c>
///   3. Default per environment:
///        - Development: &lt;solution root&gt;/logs (walks up to find ComplaincePlatform)
///        - Other:       %ProgramData%/PramanCompliance/logs (or /var/log/praman on non-Windows)
/// </summary>
public static class LogPathResolver
{
    public static string Resolve(IConfiguration configuration, string environmentName)
    {
        var configured = configuration[LoggingConstants.FileRootConfigKey];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        var fromEnv = Environment.GetEnvironmentVariable(LoggingConstants.FileRootEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return Path.GetFullPath(fromEnv);
        }

        return ResolveDefault(environmentName);
    }

    public static string ResolveDefault(string environmentName)
    {
        var isDevelopment = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
        if (isDevelopment)
        {
            return Path.Combine(FindSolutionRoot(), "logs");
        }

        if (OperatingSystem.IsWindows())
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, "PramanCompliance", "logs");
        }

        return "/var/log/praman";
    }

    /// <summary>
    /// Resolves the log root before the host configuration is available.
    /// Honors <see cref="LoggingConstants.FileRootEnvVar"/> so container deployments
    /// can write to mounted AOVIAS log volumes during bootstrap logging.
    /// </summary>
    public static string ResolveBootstrapRoot(string environmentName)
    {
        var fromEnv = Environment.GetEnvironmentVariable(LoggingConstants.FileRootEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return Path.GetFullPath(fromEnv);
        }

        return ResolveDefault(environmentName);
    }

    /// <summary>Directory for a specific service's operational logs.</summary>
    public static string ServiceLogDirectory(string root, string serviceName)
        => Path.Combine(root, serviceName);

    /// <summary>Directory for the cross-service audit stream.</summary>
    public static string AuditLogDirectory(string root)
        => Path.Combine(root, "_audit");

    /// <summary>
    /// Walks up from the current base directory to find the repository's
    /// "ComplaincePlatform" folder; falls back to the current directory.
    /// </summary>
    private static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (string.Equals(current.Name, "ComplaincePlatform", StringComparison.OrdinalIgnoreCase))
            {
                return current.FullName;
            }

            var candidate = Path.Combine(current.FullName, "ComplaincePlatform");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
