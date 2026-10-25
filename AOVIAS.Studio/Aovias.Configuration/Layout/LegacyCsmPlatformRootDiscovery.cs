namespace Aovias.Configuration.Layout;

/// <summary>
/// Legacy platform-root walk used by <c>ComplianceServiceManager.Services.RootPathResolver.ResolvePlatformRoot</c>.
/// Requires both <c>aovias-config</c> and <c>Aovias.Runtime.Agent</c> at the same repository root.
/// </summary>
public static class LegacyCsmPlatformRootDiscovery
{
    public static bool TryResolve(string baseDirectory, out string repositoryRoot)
        => RepositoryLayoutPlatformRootDiscovery.TryResolveCsmPlatformRoot(baseDirectory, out repositoryRoot);

    public static string Resolve(string baseDirectory)
    {
        if (TryResolve(baseDirectory, out var repositoryRoot))
        {
            return repositoryRoot;
        }

        throw RepositoryLayoutPlatformRootDiscovery.CreateCsmNotFoundException(baseDirectory);
    }
}
