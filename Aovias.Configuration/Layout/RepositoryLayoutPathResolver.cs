namespace Aovias.Configuration.Layout;

/// <summary>
/// Resolves repository-relative paths from a layout manifest. Does not replace legacy CSM or Publishing path resolvers in Phase 1C-A.
/// </summary>
public static class RepositoryLayoutPathResolver
{
    public static RepositoryLayoutDiscoveryProfile GetDiscoveryProfile(
        RepositoryLayoutManifest manifest,
        RepositoryLayoutProfile profile)
        => profile switch
        {
            RepositoryLayoutProfile.Current => manifest.Discovery.Current,
            RepositoryLayoutProfile.Target => manifest.Discovery.Target,
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null)
        };

    /// <summary>
    /// Walks upward from <paramref name="startDirectory"/> and returns the deepest directory whose markers match the profile.
    /// When multiple ancestors match, the nearest to <paramref name="startDirectory"/> wins (deterministic).
    /// </summary>
    public static string? TryResolveRepositoryRoot(
        string startDirectory,
        RepositoryLayoutManifest manifest,
        RepositoryLayoutProfile profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);
        ArgumentNullException.ThrowIfNull(manifest);

        var discovery = GetDiscoveryProfile(manifest, profile);
        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));

        while (current is not null)
        {
            if (MatchesPlatformRoot(current.FullName, discovery.PlatformRoot))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Walks upward using <see cref="RepositoryLayoutDiscoveryProfile.CsmPlatformRoot"/> markers (Compliance Service Manager).
    /// </summary>
    public static string? TryResolveCsmRepositoryRoot(
        string startDirectory,
        RepositoryLayoutManifest manifest,
        RepositoryLayoutProfile profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startDirectory);
        ArgumentNullException.ThrowIfNull(manifest);

        var discovery = GetDiscoveryProfile(manifest, profile);
        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));

        while (current is not null)
        {
            if (MatchesPlatformRoot(current.FullName, discovery.CsmPlatformRoot))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    public static bool MatchesPlatformRoot(string repositoryRoot, RepositoryLayoutPlatformRootMarkers markers)
    {
        foreach (var required in markers.RequiredAll)
        {
            if (!Directory.Exists(Path.Combine(repositoryRoot, NormalizeRelative(required))))
            {
                return false;
            }
        }

        if (markers.RequiredAny.Count == 0)
        {
            return markers.RequiredAll.Count > 0;
        }

        return markers.RequiredAny.Any(candidate =>
            Directory.Exists(Path.Combine(repositoryRoot, NormalizeRelative(candidate))));
    }

    public static IReadOnlyList<string> GetDefaultApplicationRoots(
        string repositoryRoot,
        RepositoryLayoutManifest manifest,
        RepositoryLayoutProfile profile)
    {
        var discovery = GetDiscoveryProfile(manifest, profile);
        var roots = new List<string>();

        foreach (var relative in discovery.DefaultApplicationRoots)
        {
            var absolute = CombineUnderRepositoryRoot(repositoryRoot, relative);
            if (Directory.Exists(absolute))
            {
                roots.Add(absolute);
            }
        }

        return roots;
    }

    public static IReadOnlyList<string> GetDeployableProjectScanRoots(
        string repositoryRoot,
        RepositoryLayoutManifest manifest,
        RepositoryLayoutProfile profile)
    {
        var discovery = GetDiscoveryProfile(manifest, profile);
        return discovery.DeployableProjectScanRoots
            .Select(relative => CombineUnderRepositoryRoot(repositoryRoot, relative))
            .ToList();
    }

    public static string GetRuntimeAgentProjectPath(
        string repositoryRoot,
        RepositoryLayoutManifest manifest,
        RepositoryLayoutProfile profile)
    {
        var discovery = GetDiscoveryProfile(manifest, profile);
        return CombineUnderRepositoryRoot(repositoryRoot, discovery.RuntimeAgentProjectRelative);
    }

    public static string GetWebAppAngularManifestPath(
        string repositoryRoot,
        RepositoryLayoutManifest manifest,
        RepositoryLayoutProfile profile)
    {
        var discovery = GetDiscoveryProfile(manifest, profile);
        return CombineUnderRepositoryRoot(repositoryRoot, discovery.WebAppAngularManifestRelative);
    }

    public static string CombineUnderRepositoryRoot(string repositoryRoot, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var normalizedRepo = Path.GetFullPath(repositoryRoot);
        var normalizedRelative = NormalizeRelative(relativePath);

        if (Path.IsPathRooted(normalizedRelative))
        {
            throw new InvalidOperationException("Relative path must not be absolute.");
        }

        if (normalizedRelative.Split('/', '\\').Any(segment => segment == ".."))
        {
            throw new InvalidOperationException("Relative path must not traverse above the repository root.");
        }

        var combined = Path.GetFullPath(Path.Combine(normalizedRepo, normalizedRelative));

        if (!combined.StartsWith(normalizedRepo.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !combined.Equals(normalizedRepo, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved path escapes the repository root boundary.");
        }

        return combined;
    }

    private static string NormalizeRelative(string relativePath)
        => relativePath.Replace('\\', '/').Trim('/');
}
