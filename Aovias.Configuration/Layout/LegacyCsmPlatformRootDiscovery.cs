namespace Aovias.Configuration.Layout;

/// <summary>
/// Legacy platform-root walk used by <c>ComplianceServiceManager.Services.RootPathResolver.ResolvePlatformRoot</c>.
/// Requires both <c>aovias-config</c> and <c>Aovias.Runtime.Agent</c> at the same repository root.
/// </summary>
public static class LegacyCsmPlatformRootDiscovery
{
    public static string Resolve(string baseDirectory)
    {
        var current = new DirectoryInfo(baseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Aovias.Runtime.Agent"))
                && RepositoryLayoutAoviasConfigPaths.ConfigDirectoryExistsAtRepositoryRoot(current.FullName))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
    }
}
