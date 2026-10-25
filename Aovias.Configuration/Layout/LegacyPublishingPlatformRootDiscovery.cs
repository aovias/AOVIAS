namespace Aovias.Configuration.Layout;

/// <summary>
/// Legacy platform-root walk used by <c>Aovias.Publishing.Core.Discovery.PlatformRootResolver</c> before manifest integration.
/// Kept for deterministic fallback and compatibility tests.
/// </summary>
public static class LegacyPublishingPlatformRootDiscovery
{
    public static string Resolve(string baseDirectory)
    {
        var current = new DirectoryInfo(baseDirectory);
        while (current is not null)
        {
            if (RepositoryLayoutAoviasConfigPaths.ConfigDirectoryExistsAtRepositoryRoot(current.FullName)
                && (Directory.Exists(Path.Combine(current.FullName, "ComplaincePlatform"))
                    || Directory.Exists(Path.Combine(current.FullName, "Aovias.Runtime.Agent"))))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", ".."));
    }
}
