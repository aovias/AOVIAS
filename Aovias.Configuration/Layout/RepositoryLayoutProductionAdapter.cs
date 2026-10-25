namespace Aovias.Configuration.Layout;

/// <summary>
/// Production entry point for repository layout contract integration (Phase 1C-B).
/// Only the <see cref="RepositoryLayoutProfile.Current"/> profile is permitted.
/// </summary>
public static class RepositoryLayoutProductionAdapter
{
    /// <summary>
    /// Production resolution never activates the target layout profile.
    /// </summary>
    public static bool IsPermittedProductionProfile(RepositoryLayoutProfile profile)
        => profile == RepositoryLayoutProfile.Current;

    /// <summary>
    /// Attempts to resolve the platform repository root using the layout manifest (current profile only).
    /// Returns false when the manifest is missing, invalid, or does not match on-disk markers — callers must use legacy discovery.
    /// </summary>
    public static bool TryResolvePlatformRepositoryRoot(string startDirectory, out string repositoryRoot)
    {
        repositoryRoot = string.Empty;

        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return false;
        }

        if (!RepositoryLayoutManifestProvider.TryLoadForProduction(startDirectory, out var manifest))
        {
            return false;
        }

        var resolved = RepositoryLayoutPathResolver.TryResolveRepositoryRoot(
            startDirectory,
            manifest,
            RepositoryLayoutProfile.Current);

        if (string.IsNullOrWhiteSpace(resolved))
        {
            return false;
        }

        repositoryRoot = resolved;
        return true;
    }

    /// <summary>
    /// Resolves the platform repository root for publishing using manifest-first, legacy-fallback semantics.
    /// </summary>
    public static string ResolvePublishingPlatformRoot(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        if (TryResolvePlatformRepositoryRoot(baseDirectory, out var manifestRoot))
        {
            return manifestRoot;
        }

        return LegacyPublishingPlatformRootDiscovery.Resolve(baseDirectory);
    }

    /// <summary>
    /// Attempts to resolve the CSM platform repository root (current profile, <see cref="RepositoryLayoutDiscoveryProfile.CsmPlatformRoot"/> markers only).
    /// </summary>
    public static bool TryResolveCsmPlatformRepositoryRoot(string startDirectory, out string repositoryRoot)
    {
        repositoryRoot = string.Empty;

        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return false;
        }

        if (!RepositoryLayoutManifestProvider.TryLoadForProduction(startDirectory, out var manifest))
        {
            return false;
        }

        var resolved = RepositoryLayoutPathResolver.TryResolveCsmRepositoryRoot(
            startDirectory,
            manifest,
            RepositoryLayoutProfile.Current);

        if (string.IsNullOrWhiteSpace(resolved))
        {
            return false;
        }

        repositoryRoot = resolved;
        return true;
    }

    /// <summary>
    /// Resolves the platform repository root for CSM using manifest-first, legacy-fallback semantics.
    /// </summary>
    public static string ResolveCsmPlatformRoot(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        if (TryResolveCsmPlatformRepositoryRoot(baseDirectory, out var manifestRoot))
        {
            return manifestRoot;
        }

        return LegacyCsmPlatformRootDiscovery.Resolve(baseDirectory);
    }

}
