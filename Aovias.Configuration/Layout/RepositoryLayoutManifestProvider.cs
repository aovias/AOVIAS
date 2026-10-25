namespace Aovias.Configuration.Layout;

/// <summary>
/// Locates and caches <c>repository-layout.json</c> by walking upward from a start directory.
/// </summary>
public static class RepositoryLayoutManifestProvider
{
    private static readonly object Gate = new();
    private static string? cachedManifestPath;
    private static RepositoryLayoutManifest? cachedManifest;
    private static bool cacheInitialized;

    /// <summary>
    /// Resets the in-process cache (for tests only).
    /// </summary>
    public static void ResetCacheForTesting()
    {
        lock (Gate)
        {
            cachedManifestPath = null;
            cachedManifest = null;
            cacheInitialized = false;
        }
    }

    public static string? TryFindManifestPath(string startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return null;
        }

        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "repository-layout.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Attempts to load a validated manifest for production use. Returns false when the file is missing or invalid.
    /// </summary>
    public static bool TryLoadForProduction(string startDirectory, out RepositoryLayoutManifest manifest)
    {
        manifest = null!;

        var path = TryFindManifestPath(startDirectory);
        if (path is null)
        {
            return false;
        }

        lock (Gate)
        {
            if (cacheInitialized && cachedManifest is not null && string.Equals(cachedManifestPath, path, StringComparison.OrdinalIgnoreCase))
            {
                manifest = cachedManifest;
                return true;
            }
        }

        if (!RepositoryLayoutLoader.TryLoadFromFile(path, out var loaded, out _))
        {
            return false;
        }

        lock (Gate)
        {
            cachedManifestPath = path;
            cachedManifest = loaded;
            cacheInitialized = true;
            manifest = loaded;
        }

        return true;
    }
}
