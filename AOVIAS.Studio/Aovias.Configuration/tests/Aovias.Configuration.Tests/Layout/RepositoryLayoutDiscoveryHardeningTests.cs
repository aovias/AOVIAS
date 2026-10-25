using Aovias.Configuration.Layout;
using Xunit;

namespace Aovias.Configuration.Tests.Layout;

public sealed class RepositoryLayoutDiscoveryHardeningTests
{
    [Fact]
    public void Publishing_resolves_from_nested_start_directory()
    {
        var root = CreatePublishingRoot(infrastructureConfig: true, legacyConfig: false);
        var nested = Path.Combine(root, "Aovias.Publishing", "src", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(nested);

        try
        {
            Assert.True(
                RepositoryLayoutPlatformRootDiscovery.TryResolvePublishingPlatformRoot(nested, out var resolved));
            Assert.Equal(root, resolved);
            Assert.Equal(resolved, RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(nested));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Csm_resolves_from_nested_start_directory()
    {
        var root = CreateCsmRoot(infrastructureConfig: true, legacyConfig: false);
        var nested = Path.Combine(root, "ComplianceServiceManager", "bin");
        Directory.CreateDirectory(nested);

        try
        {
            Assert.True(RepositoryLayoutPlatformRootDiscovery.TryResolveCsmPlatformRoot(nested, out var resolved));
            Assert.Equal(root, resolved);
            Assert.Equal(resolved, RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot(nested));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Publishing_rejects_temp_directory_without_markers()
    {
        var temp = Path.Combine(Path.GetTempPath(), "aovias-discovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);

        try
        {
            Assert.False(RepositoryLayoutPlatformRootDiscovery.TryResolvePublishingPlatformRoot(temp, out _));
            var ex = Assert.Throws<InvalidOperationException>(
                () => RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(temp));
            Assert.Contains("Could not locate the AOVIAS platform repository root", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void Csm_rejects_temp_directory_without_markers()
    {
        var temp = Path.Combine(Path.GetTempPath(), "aovias-discovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);

        try
        {
            Assert.False(RepositoryLayoutPlatformRootDiscovery.TryResolveCsmPlatformRoot(temp, out _));
            var ex = Assert.Throws<InvalidOperationException>(
                () => RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot(temp));
            Assert.Contains("Could not locate the AOVIAS CSM platform repository root", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void Legacy_publishing_supports_infrastructure_and_legacy_config_layouts()
    {
        var infraRoot = CreatePublishingRoot(infrastructureConfig: true, legacyConfig: false);
        var legacyRoot = CreatePublishingRoot(infrastructureConfig: false, legacyConfig: true);
        try
        {
            Assert.Equal(infraRoot, LegacyPublishingPlatformRootDiscovery.Resolve(Path.Combine(infraRoot, "bin")));
            Assert.Equal(legacyRoot, LegacyPublishingPlatformRootDiscovery.Resolve(Path.Combine(legacyRoot, "bin")));
        }
        finally
        {
            Directory.Delete(infraRoot, recursive: true);
            Directory.Delete(legacyRoot, recursive: true);
        }
    }

    private static string CreatePublishingRoot(bool infrastructureConfig, bool legacyConfig)
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-pub-" + Guid.NewGuid().ToString("N"));
        if (infrastructureConfig)
        {
            Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Infrastructure", "aovias-config"));
        }

        if (legacyConfig)
        {
            Directory.CreateDirectory(Path.Combine(root, "aovias-config"));
        }

        Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Command", "ComplaincePlatform"));
        return root;
    }

    private static string CreateCsmRoot(bool infrastructureConfig, bool legacyConfig)
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-csm-" + Guid.NewGuid().ToString("N"));
        if (infrastructureConfig)
        {
            Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Infrastructure", "aovias-config"));
        }

        if (legacyConfig)
        {
            Directory.CreateDirectory(Path.Combine(root, "aovias-config"));
        }

        Directory.CreateDirectory(Path.Combine(root, "Aovias.Runtime.Agent"));
        return root;
    }
}
