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

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "aovias-config"))
                && (Directory.Exists(Path.Combine(current.FullName, "ComplaincePlatform"))
                    || Directory.Exists(Path.Combine(current.FullName, "Aovias.Runtime.Agent"))))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    public static string ResolveWebAppRoot(string platformRoot)
    {
        var candidate = Path.Combine(platformRoot, "WebApp");
        return Directory.Exists(candidate) ? candidate : string.Empty;
    }

    public static string ResolveRuntimeAgentProjectPath(string platformRoot)
        => Path.Combine(
            platformRoot,
            "Aovias.Runtime.Agent",
            "src",
            "Aovias.Runtime.Agent.Api",
            "Aovias.Runtime.Agent.Api.csproj");
}
