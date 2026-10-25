using Aovias.Configuration.Layout;
using Xunit;

namespace Aovias.Configuration.Tests.Layout;

public sealed class RepositoryLayoutW1TransitionTests
{
    [Fact]
    public void Legacy_publishing_resolves_when_only_infrastructure_config_exists()
    {
        var root = CreateRoot(infrastructureConfig: true, legacyConfig: false, complaincePlatform: true, runtimeAgent: false);
        try
        {
            var resolved = LegacyPublishingPlatformRootDiscovery.Resolve(Path.Combine(root, "Aovias.Publishing", "bin"));
            Assert.Equal(root, resolved);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Legacy_publishing_resolves_when_only_legacy_root_config_exists()
    {
        var root = CreateRoot(infrastructureConfig: false, legacyConfig: true, complaincePlatform: true, runtimeAgent: false);
        try
        {
            var resolved = LegacyPublishingPlatformRootDiscovery.Resolve(Path.Combine(root, "Aovias.Publishing", "bin"));
            Assert.Equal(root, resolved);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Legacy_csm_requires_runtime_agent_with_either_config_layout()
    {
        var infraRoot = CreateRoot(infrastructureConfig: true, legacyConfig: false, complaincePlatform: false, runtimeAgent: true);
        var legacyRoot = CreateRoot(infrastructureConfig: false, legacyConfig: true, complaincePlatform: false, runtimeAgent: true);
        try
        {
            Assert.Equal(infraRoot, LegacyCsmPlatformRootDiscovery.Resolve(Path.Combine(infraRoot, "ComplianceServiceManager", "bin")));
            Assert.Equal(legacyRoot, LegacyCsmPlatformRootDiscovery.Resolve(Path.Combine(legacyRoot, "ComplianceServiceManager", "bin")));
        }
        finally
        {
            Directory.Delete(infraRoot, recursive: true);
            Directory.Delete(legacyRoot, recursive: true);
        }
    }

    [Fact]
    public void ResolveConfigRootDirectory_prefers_infrastructure_when_both_exist()
    {
        var root = CreateRoot(infrastructureConfig: true, legacyConfig: true, complaincePlatform: true, runtimeAgent: false);
        try
        {
            var configRoot = RepositoryLayoutAoviasConfigPaths.ResolveConfigRootDirectory(root);
            Assert.Equal(RepositoryLayoutAoviasConfigPaths.CombineInfrastructureDirectory(root), configRoot);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Integration_manifest_current_markers_match_post_w1_repository_layout()
    {
        var manifestPath = TryFindManifestPath();
        if (manifestPath is null)
        {
            return;
        }

        var manifest = RepositoryLayoutLoader.LoadFromFile(manifestPath);
        var repoRoot = Path.GetDirectoryName(manifestPath)!;
        Assert.True(
            RepositoryLayoutPathResolver.MatchesPlatformRoot(repoRoot, manifest.Discovery.Current.PlatformRoot),
            "Current publishing markers must match the post-W1 repository.");
        Assert.True(
            RepositoryLayoutPathResolver.MatchesPlatformRoot(repoRoot, manifest.Discovery.Current.CsmPlatformRoot),
            "Current CSM markers must match the post-W1 repository.");
        Assert.False(Directory.Exists(Path.Combine(repoRoot, "aovias-config")), "Obsolete root-level aovias-config must not remain after W1.");
    }

    private static string CreateRoot(
        bool infrastructureConfig,
        bool legacyConfig,
        bool complaincePlatform,
        bool runtimeAgent)
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-w1-" + Guid.NewGuid().ToString("N"));
        if (infrastructureConfig)
        {
            Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Infrastructure", "aovias-config"));
        }

        if (legacyConfig)
        {
            Directory.CreateDirectory(Path.Combine(root, "aovias-config"));
        }

        if (complaincePlatform)
        {
            Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Command", "ComplaincePlatform"));
        }

        if (runtimeAgent)
        {
            Directory.CreateDirectory(Path.Combine(root, "Aovias.Runtime.Agent"));
        }

        Directory.CreateDirectory(Path.Combine(root, "Aovias.Publishing", "bin"));
        Directory.CreateDirectory(Path.Combine(root, "ComplianceServiceManager", "bin"));
        return root;
    }

    private static string? TryFindManifestPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
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
}
