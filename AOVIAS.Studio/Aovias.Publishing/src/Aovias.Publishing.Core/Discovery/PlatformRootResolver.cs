using Aovias.Configuration.Layout;

namespace Aovias.Publishing.Core.Discovery;

/// <summary>
/// Locates the platform repository root containing deployable projects.
/// </summary>
public static class PlatformRootResolver
{
    public static string Resolve(string? configuredRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredRoot) && Directory.Exists(configuredRoot))
        {
            return Path.GetFullPath(configuredRoot);
        }

        return RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(AppContext.BaseDirectory);
    }

    public static string ResolveWebAppRoot(string platformRoot)
    {
        var commandWebApp = Path.Combine(platformRoot, "AOVIAS.Command", "WebApp");
        if (Directory.Exists(commandWebApp))
        {
            return commandWebApp;
        }

        var legacyWebApp = Path.Combine(platformRoot, "WebApp");
        return Directory.Exists(legacyWebApp) ? legacyWebApp : string.Empty;
    }

    public static string ResolveRuntimeAgentProjectPath(string platformRoot)
        => Path.Combine(
            platformRoot,
            "AOVIAS.Studio",
            "Aovias.Runtime.Agent",
            "src",
            "Aovias.Runtime.Agent.Api",
            "Aovias.Runtime.Agent.Api.csproj");
}
