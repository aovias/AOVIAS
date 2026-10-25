using System.IO;
using Aovias.Configuration.Layout;

namespace ComplianceServiceManager.Services;

public static class RootPathResolver
{
    public static string ResolveCompliancePlatformRoot()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDirectory);

        while (current is not null)
        {
            var commandCandidate = Path.Combine(current.FullName, "AOVIAS.Command", "ComplaincePlatform");
            if (Directory.Exists(commandCandidate))
            {
                return commandCandidate;
            }

            var legacyCandidate = Path.Combine(current.FullName, "ComplaincePlatform");
            if (Directory.Exists(legacyCandidate))
            {
                return legacyCandidate;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "AOVIAS.Command", "ComplaincePlatform"));
    }
    public static string ResolveWebAppRoot()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDirectory);

        while (current is not null)
        {
            var commandCandidate = Path.Combine(current.FullName, "AOVIAS.Command", "WebApp");
            if (Directory.Exists(commandCandidate))
            {
                return commandCandidate;
            }

            var legacyCandidate = Path.Combine(current.FullName, "WebApp");
            if (Directory.Exists(legacyCandidate))
            {
                return legacyCandidate;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "AOVIAS.Command", "WebApp"));
    }

    public static string ResolvePlatformRoot()
        => RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot(AppContext.BaseDirectory);

    public static string ResolveRuntimeAgentProjectPath()
        => Path.Combine(
            ResolvePlatformRoot(),
            "AOVIAS.Studio",
            "Aovias.Runtime.Agent",
            "src",
            "Aovias.Runtime.Agent.Api",
            "Aovias.Runtime.Agent.Api.csproj");

    public static string ResolveBootstrapHostProjectPath()
        => Path.Combine(
            ResolvePlatformRoot(),
            "AOVIAS.Studio",
            "Aovias.Bootstrap",
            "src",
            "Aovias.Bootstrap.Host",
            "Aovias.Bootstrap.Host.csproj");

    /// <summary>Working directory the Bootstrap CLI writes its <c>bootstrap-state/*.json</c>
    /// resumability files under — must match what the CLI itself defaults to (its own current
    /// directory when launched with this as <see cref="System.Diagnostics.ProcessStartInfo.WorkingDirectory"/>) so polling reads the same files it writes.</summary>
    public static string ResolveBootstrapWorkingDirectory()
        => Path.Combine(ResolvePlatformRoot(), "AOVIAS.Studio", "Aovias.Bootstrap");
}
