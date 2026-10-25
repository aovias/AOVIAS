namespace Aovias.Configuration.Layout;

/// <summary>
/// Shared marker checks and fail-fast resolution for legacy platform-root walks (Phase 1G-D).
/// </summary>
public static class RepositoryLayoutPlatformRootDiscovery
{
    public static bool MatchesPublishingLegacyMarkers(string repositoryRoot)
        => RepositoryLayoutAoviasConfigPaths.ConfigDirectoryExistsAtRepositoryRoot(repositoryRoot)
           && (Directory.Exists(Path.Combine(repositoryRoot, "ComplaincePlatform"))
               || Directory.Exists(Path.Combine(repositoryRoot, "Aovias.Runtime.Agent")));

    public static bool MatchesCsmLegacyMarkers(string repositoryRoot)
        => Directory.Exists(Path.Combine(repositoryRoot, "Aovias.Runtime.Agent"))
           && RepositoryLayoutAoviasConfigPaths.ConfigDirectoryExistsAtRepositoryRoot(repositoryRoot);

    public static bool TryResolvePublishingPlatformRoot(string startDirectory, out string repositoryRoot)
    {
        repositoryRoot = string.Empty;
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return false;
        }

        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (current is not null)
        {
            if (MatchesPublishingLegacyMarkers(current.FullName))
            {
                repositoryRoot = current.FullName;
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    public static bool TryResolveCsmPlatformRoot(string startDirectory, out string repositoryRoot)
    {
        repositoryRoot = string.Empty;
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return false;
        }

        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (current is not null)
        {
            if (MatchesCsmLegacyMarkers(current.FullName))
            {
                repositoryRoot = current.FullName;
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    internal static InvalidOperationException CreatePublishingNotFoundException(string startDirectory)
        => new(
            "Could not locate the AOVIAS platform repository root from '"
            + Path.GetFullPath(startDirectory)
            + "'. Required markers: configuration directory at '"
            + RepositoryLayoutAoviasConfigPaths.InfrastructureRelativePath
            + "' or '"
            + RepositoryLayoutAoviasConfigPaths.LegacyRootRelativeDirectory
            + "', plus 'ComplaincePlatform' or 'Aovias.Runtime.Agent'.");

    internal static InvalidOperationException CreateCsmNotFoundException(string startDirectory)
        => new(
            "Could not locate the AOVIAS CSM platform repository root from '"
            + Path.GetFullPath(startDirectory)
            + "'. Required markers: configuration directory at '"
            + RepositoryLayoutAoviasConfigPaths.InfrastructureRelativePath
            + "' or '"
            + RepositoryLayoutAoviasConfigPaths.LegacyRootRelativeDirectory
            + "', and 'Aovias.Runtime.Agent'.");
}
