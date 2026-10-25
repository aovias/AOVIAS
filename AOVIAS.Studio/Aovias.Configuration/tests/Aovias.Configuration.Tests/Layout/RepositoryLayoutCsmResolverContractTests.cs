using Aovias.Configuration.Layout;
using Xunit;

namespace Aovias.Configuration.Tests.Layout;

public sealed class RepositoryLayoutCsmResolverContractTests
{
    [Fact]
    public void Csm_resolves_when_both_required_markers_present()
    {
        var root = CreateFixture(includeConfig: true, includeAgent: true, includeComplaincePlatform: false);
        WriteManifest(root);

        try
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            var start = Path.Combine(root, "ComplianceServiceManager", "bin");
            var legacy = LegacyCsmPlatformRootDiscovery.Resolve(start);
            var resolved = RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot(start);

            Assert.Equal(root, legacy);
            Assert.Equal(legacy, resolved);
            Assert.True(RepositoryLayoutProductionAdapter.TryResolveCsmPlatformRepositoryRoot(start, out var direct));
            Assert.Equal(root, direct);
        }
        finally
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Csm_does_not_resolve_when_runtime_agent_missing()
    {
        var root = CreateFixture(includeConfig: true, includeAgent: false, includeComplaincePlatform: true);
        WriteManifest(root);

        try
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            var start = Path.Combine(root, "ComplianceServiceManager", "bin");

            Assert.False(RepositoryLayoutProductionAdapter.TryResolveCsmPlatformRepositoryRoot(start, out _));

            var publishing = RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(start);
            Assert.Equal(root, publishing);
        }
        finally
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Csm_does_not_resolve_when_aovias_config_missing()
    {
        var root = CreateFixture(includeConfig: false, includeAgent: true, includeComplaincePlatform: false);
        WriteManifest(root);

        try
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            var start = Path.Combine(root, "ComplianceServiceManager", "bin");
            Assert.False(RepositoryLayoutProductionAdapter.TryResolveCsmPlatformRepositoryRoot(start, out _));
        }
        finally
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Csm_does_not_resolve_when_neither_marker_present()
    {
        var root = CreateFixture(includeConfig: false, includeAgent: false, includeComplaincePlatform: false);
        WriteManifest(root);

        try
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            var start = Path.Combine(root, "ComplianceServiceManager", "bin");
            Assert.False(RepositoryLayoutProductionAdapter.TryResolveCsmPlatformRepositoryRoot(start, out _));
            Assert.Throws<InvalidOperationException>(
                () => RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot(start));
            Assert.Throws<InvalidOperationException>(
                () => LegacyCsmPlatformRootDiscovery.Resolve(start));
        }
        finally
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Csm_falls_back_to_legacy_when_manifest_missing()
    {
        var root = CreateFixture(includeConfig: true, includeAgent: true, includeComplaincePlatform: false);
        var start = Path.Combine(root, "ComplianceServiceManager", "bin");

        try
        {
            var legacy = LegacyCsmPlatformRootDiscovery.Resolve(start);
            Assert.Equal(legacy, RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot(start));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Csm_falls_back_when_manifest_invalid()
    {
        var root = CreateFixture(includeConfig: true, includeAgent: true, includeComplaincePlatform: false);
        File.WriteAllText(Path.Combine(root, "repository-layout.json"), "{");
        var start = Path.Combine(root, "ComplianceServiceManager", "bin");

        try
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            var legacy = LegacyCsmPlatformRootDiscovery.Resolve(start);
            Assert.Equal(legacy, RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot(start));
        }
        finally
        {
            RepositoryLayoutManifestProvider.ResetCacheForTesting();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Target_profile_csm_fixture_does_not_activate_in_production_adapter()
    {
        var root = CreateTargetCsmFixture();
        var manifest = LoadManifestWithTargetCsm();
        var start = Path.Combine(root, "ComplianceServiceManager", "bin");

        try
        {
            Assert.False(RepositoryLayoutProductionAdapter.IsPermittedProductionProfile(RepositoryLayoutProfile.Target));
            Assert.False(RepositoryLayoutProductionAdapter.TryResolveCsmPlatformRepositoryRoot(start, out _));

            var targetRoot = RepositoryLayoutPathResolver.TryResolveCsmRepositoryRoot(
                start,
                manifest,
                RepositoryLayoutProfile.Target);
            Assert.Equal(root, targetRoot);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Csm_combine_rejects_path_traversal()
    {
        var repo = Path.Combine(Path.GetTempPath(), "aovias-csm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);

        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                RepositoryLayoutPathResolver.CombineUnderRepositoryRoot(repo, "..\\secrets"));
        }
        finally
        {
            Directory.Delete(repo, recursive: true);
        }
    }

    [Fact]
    public void Integration_repo_csm_adapter_matches_legacy_csm_walk()
    {
        var manifestPath = TryFindIntegrationManifest();
        if (manifestPath is null)
        {
            return;
        }

        var repoRoot = Path.GetDirectoryName(manifestPath)!;
        var start = Directory.Exists(Path.Combine(repoRoot, "ComplianceServiceManager", "bin"))
            ? Path.Combine(repoRoot, "ComplianceServiceManager", "bin")
            : repoRoot;

        RepositoryLayoutManifestProvider.ResetCacheForTesting();
        var legacy = LegacyCsmPlatformRootDiscovery.Resolve(start);
        var resolved = RepositoryLayoutProductionAdapter.ResolveCsmPlatformRoot(start);
        Assert.Equal(legacy, resolved);
    }

    private static string CreateFixture(bool includeConfig, bool includeAgent, bool includeComplaincePlatform)
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-csm-fix-" + Guid.NewGuid().ToString("N"));
        if (includeConfig)
        {
            Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Infrastructure", "aovias-config"));
        }

        if (includeAgent)
        {
            Directory.CreateDirectory(Path.Combine(root, "Aovias.Runtime.Agent"));
        }

        if (includeComplaincePlatform)
        {
            Directory.CreateDirectory(Path.Combine(root, "ComplaincePlatform"));
        }

        Directory.CreateDirectory(Path.Combine(root, "ComplianceServiceManager", "bin"));
        return root;
    }

    private static string CreateTargetCsmFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-csm-target-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Infrastructure", "aovias-config"));
        Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Studio", "Aovias.Runtime.Agent"));
        Directory.CreateDirectory(Path.Combine(root, "ComplianceServiceManager", "bin"));
        return root;
    }

    private static void WriteManifest(string repoRoot)
        => File.WriteAllText(Path.Combine(repoRoot, "repository-layout.json"), StandardManifestJson());

    private static RepositoryLayoutManifest LoadManifestWithTargetCsm()
        => RepositoryLayoutLoader.LoadFromJson(StandardManifestJson());

    private static string StandardManifestJson()
        => """
            {
              "schemaVersion": "1.0.0",
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

    private static string? TryFindIntegrationManifest()
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
