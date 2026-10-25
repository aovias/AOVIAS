using Aovias.Configuration.Layout;
using Xunit;

namespace Aovias.Configuration.Tests.Layout;

public sealed class RepositoryLayoutContractTests
{
    [Fact]
    public void LoadFromFile_validates_repository_layout_json_at_repo_root()
    {
        var manifestPath = TryFindManifestPath();
        if (manifestPath is null)
        {
            return;
        }

        var manifest = RepositoryLayoutLoader.LoadFromFile(manifestPath);
        Assert.Equal("1.0.0", manifest.SchemaVersion);
        Assert.NotEmpty(manifest.Products);
        Assert.NotEmpty(manifest.Solutions);
        Assert.NotEmpty(manifest.UnresolvedDecisions);
    }

    [Fact]
    public void LoadFromJson_rejects_missing_schema_version()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            RepositoryLayoutLoader.LoadFromJson("""{"products":[]}"""));
        Assert.Contains("schemaVersion", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_rejects_path_traversal_segments()
    {
        var json = """
            {
              "schemaVersion": "1.0.0",
              "products": [],
              "solutions": [],
              "discovery": {
                "current": {
                  "platformRoot": { "requiredAll": ["aovias-config"], "requiredAny": ["ComplaincePlatform"] },
                  "csmPlatformRoot": { "requiredAll": ["aovias-config", "Aovias.Runtime.Agent"], "requiredAny": [] },
                  "defaultApplicationRoots": ["../escape"],
                  "deployableProjectScanRoots": [],
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

        var ex = Assert.Throws<InvalidOperationException>(() => RepositoryLayoutLoader.LoadFromJson(json));
        Assert.Contains("..", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TryResolveRepositoryRoot_finds_current_layout_from_repo_fixture()
    {
        var repoRoot = CreateCurrentLayoutFixture();
        var manifest = LoadMinimalManifest();

        var resolved = RepositoryLayoutPathResolver.TryResolveRepositoryRoot(
            Path.Combine(repoRoot, "ComplianceServiceManager", "bin", "Debug"),
            manifest,
            RepositoryLayoutProfile.Current);

        Assert.Equal(repoRoot, resolved);
    }

    [Fact]
    public void TryResolveRepositoryRoot_finds_target_layout_from_synthetic_fixture()
    {
        var repoRoot = CreateTargetLayoutFixture();
        var manifest = LoadMinimalManifest();

        var resolved = RepositoryLayoutPathResolver.TryResolveRepositoryRoot(
            Path.Combine(repoRoot, "AOVIAS.Studio", "Aovias.Runtime.Agent", "src"),
            manifest,
            RepositoryLayoutProfile.Target);

        Assert.Equal(repoRoot, resolved);
    }

    [Fact]
    public void TryResolveRepositoryRoot_prefers_nearest_ancestor_when_multiple_match()
    {
        var outer = CreateCurrentLayoutFixture();
        var inner = Path.Combine(outer, "nested");
        Directory.CreateDirectory(Path.Combine(inner, "aovias-config"));
        Directory.CreateDirectory(Path.Combine(inner, "ComplaincePlatform"));
        var manifest = LoadMinimalManifest();

        var resolved = RepositoryLayoutPathResolver.TryResolveRepositoryRoot(
            Path.Combine(inner, "ComplianceServiceManager"),
            manifest,
            RepositoryLayoutProfile.Current);

        Assert.Equal(inner, resolved);
    }

    [Fact]
    public void CombineUnderRepositoryRoot_rejects_escape_attempts()
    {
        var repo = Path.Combine(Path.GetTempPath(), "aovias-layout-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repo);

        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                RepositoryLayoutPathResolver.CombineUnderRepositoryRoot(repo, "..\\outside"));
        }
        finally
        {
            Directory.Delete(repo, recursive: true);
        }
    }

    [Fact]
    public void GetDeployableProjectScanRoots_current_profile_matches_legacy_discoverer_segments()
    {
        var manifestPath = TryFindManifestPath();
        if (manifestPath is null)
        {
            return;
        }

        var manifest = RepositoryLayoutLoader.LoadFromFile(manifestPath);
        var repoRoot = Path.GetDirectoryName(manifestPath)!;

        var scanRoots = RepositoryLayoutPathResolver.GetDeployableProjectScanRoots(
            repoRoot,
            manifest,
            RepositoryLayoutProfile.Current);

        Assert.Contains(Path.Combine(repoRoot, "ComplaincePlatform"), scanRoots, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.Combine(repoRoot, "Aovias.Runtime.Agent"), scanRoots, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetDefaultApplicationRoots_current_profile_matches_source_discovery_boundary_names()
    {
        var manifestPath = TryFindManifestPath();
        if (manifestPath is null)
        {
            return;
        }

        var manifest = RepositoryLayoutLoader.LoadFromFile(manifestPath);
        var repoRoot = Path.GetDirectoryName(manifestPath)!;

        var roots = RepositoryLayoutPathResolver.GetDefaultApplicationRoots(
            repoRoot,
            manifest,
            RepositoryLayoutProfile.Current);

        Assert.Contains(Path.Combine(repoRoot, "ComplaincePlatform"), roots, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.Combine(repoRoot, "WebApp"), roots, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Target_profile_resolution_uses_injected_fixture_without_on_disk_target_tree()
    {
        var repoRoot = CreateTargetLayoutFixture();
        var manifest = LoadMinimalManifest();

        var scanRoots = RepositoryLayoutPathResolver.GetDeployableProjectScanRoots(
            repoRoot,
            manifest,
            RepositoryLayoutProfile.Target);

        Assert.Equal(
            Path.Combine(repoRoot, "AOVIAS.Command", "CompliancePlatform"),
            scanRoots[0],
            StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            Path.Combine(repoRoot, "AOVIAS.Studio", "Aovias.Runtime.Agent"),
            scanRoots[1],
            StringComparer.OrdinalIgnoreCase);

        var agentProject = RepositoryLayoutPathResolver.GetRuntimeAgentProjectPath(
            repoRoot,
            manifest,
            RepositoryLayoutProfile.Target);
        Assert.True(agentProject.Contains("AOVIAS.Studio", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Current_layout_key_paths_exist_when_manifest_loaded_from_integration_repo()
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
            "Current platform root markers should match the integration repository layout.");

        Assert.True(File.Exists(RepositoryLayoutPathResolver.GetWebAppAngularManifestPath(
            repoRoot, manifest, RepositoryLayoutProfile.Current)));
        Assert.True(File.Exists(RepositoryLayoutPathResolver.GetRuntimeAgentProjectPath(
            repoRoot, manifest, RepositoryLayoutProfile.Current)));
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

    private static RepositoryLayoutManifest LoadMinimalManifest()
    {
        var manifestPath = TryFindManifestPath();
        if (manifestPath is not null)
        {
            return RepositoryLayoutLoader.LoadFromFile(manifestPath);
        }

        return RepositoryLayoutLoader.LoadFromJson(
            """
            {
              "schemaVersion": "1.0.0",
              "products": [],
              "solutions": [],
              "discovery": {
                "current": {
                  "platformRoot": { "requiredAll": ["aovias-config"], "requiredAny": ["ComplaincePlatform", "Aovias.Runtime.Agent"] },
                  "csmPlatformRoot": { "requiredAll": ["aovias-config", "Aovias.Runtime.Agent"], "requiredAny": [] },
                  "defaultApplicationRoots": ["ComplaincePlatform", "WebApp"],
                  "deployableProjectScanRoots": ["ComplaincePlatform", "Aovias.Runtime.Agent"],
                  "runtimeAgentProjectRelative": "Aovias.Runtime.Agent/src/Aovias.Runtime.Agent.Api/Aovias.Runtime.Agent.Api.csproj",
                  "webAppAngularManifestRelative": "WebApp/angular.json",
                  "runtimeAgentDockerCopyRoots": ["Aovias.Configuration", "Aovias.Runtime.Agent"]
                },
                "target": {
                  "platformRoot": { "requiredAll": ["AOVIAS.Infrastructure/aovias-config"], "requiredAny": ["AOVIAS.Command/CompliancePlatform", "AOVIAS.Studio/Aovias.Runtime.Agent"] },
                  "csmPlatformRoot": { "requiredAll": ["AOVIAS.Infrastructure/aovias-config", "AOVIAS.Studio/Aovias.Runtime.Agent"], "requiredAny": [] },
                  "defaultApplicationRoots": ["AOVIAS.Command/CompliancePlatform", "AOVIAS.Command/WebApp"],
                  "deployableProjectScanRoots": ["AOVIAS.Command/CompliancePlatform", "AOVIAS.Studio/Aovias.Runtime.Agent"],
                  "runtimeAgentProjectRelative": "AOVIAS.Studio/Aovias.Runtime.Agent/src/Aovias.Runtime.Agent.Api/Aovias.Runtime.Agent.Api.csproj",
                  "webAppAngularManifestRelative": "AOVIAS.Command/WebApp/angular.json",
                  "runtimeAgentDockerCopyRoots": ["AOVIAS.Studio/Aovias.Configuration"]
                }
              },
              "unresolvedDecisions": [{ "id": "O-3", "topic": "test" }]
            }
            """);
    }

    private static string CreateCurrentLayoutFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-current-layout-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "aovias-config"));
        Directory.CreateDirectory(Path.Combine(root, "ComplaincePlatform"));
        Directory.CreateDirectory(Path.Combine(root, "WebApp"));
        Directory.CreateDirectory(Path.Combine(root, "ComplianceServiceManager", "bin", "Debug"));
        return root;
    }

    private static string CreateTargetLayoutFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-target-layout-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Infrastructure", "aovias-config"));
        Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Command", "CompliancePlatform"));
        Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Command", "WebApp"));
        Directory.CreateDirectory(Path.Combine(root, "AOVIAS.Studio", "Aovias.Runtime.Agent", "src"));
        return root;
    }
}
