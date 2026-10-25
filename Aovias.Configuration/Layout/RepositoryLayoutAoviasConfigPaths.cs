namespace Aovias.Configuration.Layout;

/// <summary>
/// Repository-relative paths for the AOVIAS configuration tree (W1 infrastructure layout).
/// </summary>
public static class RepositoryLayoutAoviasConfigPaths
{
    public const string InfrastructureRelativePath = "AOVIAS.Infrastructure/aovias-config";

    public const string LegacyRootRelativeDirectory = "aovias-config";

    /// <summary>
    /// True when the post-W1 infrastructure config directory exists at the repository root.
    /// </summary>
    public static bool InfrastructureDirectoryExists(string repositoryRoot)
        => Directory.Exists(CombineInfrastructureDirectory(repositoryRoot));

    /// <summary>
    /// True when the pre-W1 root-level config directory exists (rollback / transition).
    /// </summary>
    public static bool LegacyRootDirectoryExists(string repositoryRoot)
        => Directory.Exists(Path.Combine(repositoryRoot, LegacyRootRelativeDirectory));

    /// <summary>
    /// Dual-marker check: infrastructure path preferred; legacy root path supported during transition.
    /// </summary>
    public static bool ConfigDirectoryExistsAtRepositoryRoot(string repositoryRoot)
        => InfrastructureDirectoryExists(repositoryRoot) || LegacyRootDirectoryExists(repositoryRoot);

    public static string CombineInfrastructureDirectory(string platformRoot)
        => Path.Combine(platformRoot, "AOVIAS.Infrastructure", "aovias-config");

    /// <summary>
    /// Resolves the on-disk config root; prefers infrastructure layout when present.
    /// </summary>
    public static string ResolveConfigRootDirectory(string platformRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformRoot);

        if (InfrastructureDirectoryExists(platformRoot))
        {
            return CombineInfrastructureDirectory(platformRoot);
        }

        var legacy = Path.Combine(platformRoot, LegacyRootRelativeDirectory);
        if (Directory.Exists(legacy))
        {
            return legacy;
        }

        return CombineInfrastructureDirectory(platformRoot);
    }
}
