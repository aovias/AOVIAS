namespace Aovias.Configuration.Layout;

/// <summary>
/// Legacy platform-root walk used by <c>Aovias.Publishing.Core.Discovery.PlatformRootResolver</c> before manifest integration.
/// Kept for deterministic fallback and compatibility tests.
/// </summary>
public static class LegacyPublishingPlatformRootDiscovery
{
    public static bool TryResolve(string baseDirectory, out string repositoryRoot)
        => RepositoryLayoutPlatformRootDiscovery.TryResolvePublishingPlatformRoot(baseDirectory, out repositoryRoot);

    public static string Resolve(string baseDirectory)
    {
        if (TryResolve(baseDirectory, out var repositoryRoot))
        {
            return repositoryRoot;
        }

        throw RepositoryLayoutPlatformRootDiscovery.CreatePublishingNotFoundException(baseDirectory);
    }
}
