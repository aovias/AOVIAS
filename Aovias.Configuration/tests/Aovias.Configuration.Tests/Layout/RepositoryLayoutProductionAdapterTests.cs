using Aovias.Configuration.Layout;
using Xunit;

namespace Aovias.Configuration.Tests.Layout;

public sealed class RepositoryLayoutProductionAdapterTests
{
    [Fact]
    public void TryResolvePlatformRepositoryRoot_returns_false_when_manifest_missing()
    {
        var start = Path.Combine(Path.GetTempPath(), "aovias-no-manifest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(start);

        try
        {
            Assert.False(RepositoryLayoutProductionAdapter.TryResolvePlatformRepositoryRoot(start, out _));
            var legacy = LegacyPublishingPlatformRootDiscovery.Resolve(start);
            Assert.Equal(
                legacy,
                RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(start));
        }
        finally
        {
            Directory.Delete(start, recursive: true);
        }
    }

    [Fact]
    public void ResolvePublishingPlatformRoot_matches_legacy_on_current_layout_fixture()
    {
        var root = CreateCurrentLayoutFixture();
        WriteMinimalValidManifest(root);

        try
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            var legacy = LegacyPublishingPlatformRootDiscovery.Resolve(Path.Combine(root, "Aovias.Publishing", "bin"));
            var resolved = RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(
                Path.Combine(root, "Aovias.Publishing", "bin"));

            Assert.Equal(legacy, resolved);
            Assert.True(RepositoryLayoutProductionAdapter.TryResolvePlatformRepositoryRoot(
                Path.Combine(root, "Aovias.Publishing", "bin"), out var direct));
            Assert.Equal(legacy, direct);
        }
        finally
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadFromFile_rejects_unsupported_schema_version()
    {
        var path = Path.Combine(Path.GetTempPath(), "layout-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, MinimalManifestJson(schemaVersion: "9.9.9"));

        try
        {
            Assert.False(RepositoryLayoutLoader.TryLoadFromFile(path, out _, out var error));
            Assert.Contains("Unsupported schemaVersion", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TryLoadFromJson_rejects_malformed_json()
    {
        Assert.False(RepositoryLayoutLoader.TryLoadFromJson("{ not json", out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Production_adapter_never_permits_target_profile()
    {
        Assert.True(RepositoryLayoutProductionAdapter.IsPermittedProductionProfile(RepositoryLayoutProfile.Current));
        Assert.False(RepositoryLayoutProductionAdapter.IsPermittedProductionProfile(RepositoryLayoutProfile.Target));
    }

    [Fact]
    public void ResolvePublishingPlatformRoot_falls_back_when_manifest_invalid()
    {
        var root = CreateCurrentLayoutFixture();
        File.WriteAllText(Path.Combine(root, "repository-layout.json"), "{ broken");

        try
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            var start = Path.Combine(root, "Aovias.Publishing", "bin");
            var legacy = LegacyPublishingPlatformRootDiscovery.Resolve(start);
            Assert.Equal(legacy, RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(start));
            Assert.False(RepositoryLayoutProductionAdapter.TryResolvePlatformRepositoryRoot(start, out _));
        }
        finally
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Integration_repository_manifest_resolves_same_as_legacy_publishing_walk()
    {
        var manifestPath = TryFindIntegrationManifestPath();
        if (manifestPath is null)
        {
            return;
        }

        var repoRoot = Path.GetDirectoryName(manifestPath)!;
        var start = Path.Combine(repoRoot, "Aovias.Publishing", "src", "Aovias.Publishing.Core", "bin", "Release", "net10.0");
        if (!Directory.Exists(start))
        {
            start = repoRoot;
        }

        RepositoryLayoutManifestProvider.ResetCacheForTesting();
        var legacy = LegacyPublishingPlatformRootDiscovery.Resolve(start);
        var resolved = RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(start);
        Assert.Equal(legacy, resolved);
    }

    private static string? TryFindIntegrationManifestPath()
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

    private static string CreateCurrentLayoutFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-prod-adapter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Infrastructure", "aovias-config"));
        Directory.CreateDirectory(Path.Combine(root, "ComplaincePlatform"));
        Directory.CreateDirectory(Path.Combine(root, "Aovias.Publishing", "bin"));
        return root;
    }

    private static void WriteMinimalValidManifest(string repoRoot)
    {
        File.WriteAllText(Path.Combine(repoRoot, "repository-layout.json"), MinimalManifestJson());
    }

    private static string MinimalManifestJson(string schemaVersion = "1.0.0")
        => $$"""
            {
              "schemaVersion": "{{schemaVersion}}",
              "products": [],
              "solutions": [],
              "discovery": {
                "current": {
                  "platformRoot": { "requiredAll": ["AOVIAS.Infrastructure/aovias-config"], "requiredAny": ["ComplaincePlatform", "Aovias.Runtime.Agent"] },
                  "csmPlatformRoot": { "requiredAll": ["AOVIAS.Infrastructure/aovias-config", "Aovias.Runtime.Agent"], "requiredAny": [] },
                  "defaultApplicationRoots": ["ComplaincePlatform", "WebApp"],
                  "deployableProjectScanRoots": ["ComplaincePlatform", "Aovias.Runtime.Agent"],
                  "runtimeAgentProjectRelative": "Aovias.Runtime.Agent/src/Aovias.Runtime.Agent.Api/Aovias.Runtime.Agent.Api.csproj",
                  "webAppAngularManifestRelative": "WebApp/angular.json",
                  "runtimeAgentDockerCopyRoots": []
                },
                "target": {
                  "platformRoot": { "requiredAll": ["AOVIAS.Infrastructure/aovias-config"], "requiredAny": ["AOVIAS.Command/CompliancePlatform"] },
                  "csmPlatformRoot": { "requiredAll": ["AOVIAS.Infrastructure/aovias-config", "AOVIAS.Studio/Aovias.Runtime.Agent"], "requiredAny": [] },
                  "defaultApplicationRoots": [],
                  "deployableProjectScanRoots": [],
                  "runtimeAgentProjectRelative": "AOVIAS.Studio/Aovias.Runtime.Agent/src/Aovias.Runtime.Agent.Api/Aovias.Runtime.Agent.Api.csproj",
                  "webAppAngularManifestRelative": "AOVIAS.Command/WebApp/angular.json",
                  "runtimeAgentDockerCopyRoots": []
                }
              },
              "unresolvedDecisions": []
            }
            """;
}
