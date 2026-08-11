using Aovias.Configuration.Deployment;
using Aovias.DeploymentValidation.Rules;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

/// <summary>
/// Permanent regression tests (D12). Every previously fixed publishing bug is
/// pinned here so future changes can never silently reintroduce it.
/// </summary>
public sealed class RegressionTests
{
    [Theory]
    [InlineData(@"V:\DevelopmentRepo\V1.0\config")]
    [InlineData("V:/DevelopmentRepo/V1.0/config")]
    [InlineData(@"C:\Users\developer\source")]
    [InlineData("C:/Users/developer/source")]
    public void Neutrality_rejects_developer_repo_and_user_paths(string leak)
    {
        var violations = EnvironmentNeutralRules.ScanPackageArtifacts(leak, "artifact.json");
        Assert.NotEmpty(violations);
    }

    [Theory]
    [InlineData(@"C:\Users\hp\Desktop\pkg")]
    [InlineData("/home/user/Downloads/pkg")]
    [InlineData("/home/user/Documents/pkg")]
    public void Neutrality_rejects_personal_folders(string leak)
    {
        var violations = EnvironmentNeutralRules.ScanPackageArtifacts(leak, "artifact.json", allowWindowsHostPaths: true);
        Assert.Contains(violations, violation => violation.Contains("personal folder", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Neutrality_rejects_unc_machine_paths_even_for_windows_profiles()
    {
        var violations = EnvironmentNeutralRules.ScanPackageArtifacts(@"\\BUILDSERVER\share\pkg", "artifact.json", allowWindowsHostPaths: true);
        Assert.Contains(violations, violation => violation.Contains("machine/UNC", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Neutrality_rejects_localhost_and_loopback_in_package_artifacts()
    {
        var violations = EnvironmentNeutralRules.ScanPackageArtifacts("Server=localhost;Backup=127.0.0.1", "metadata.json");
        Assert.Contains(violations, violation => violation.Contains("localhost", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(violations, violation => violation.Contains("loopback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Neutrality_allows_localhost_in_compose_healthcheck()
    {
        var violations = EnvironmentNeutralRules.ScanComposeFile("test: curl http://localhost:7080/health", "docker-compose.yml");
        Assert.DoesNotContain(violations, violation => violation.Contains("localhost", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Neutrality_allows_windows_host_paths_only_when_profile_permits()
    {
        const string windowsPath = "AOVIAS_HOST_CONFIG_ROOT=C:/Aovias/runtime/config";
        Assert.NotEmpty(EnvironmentNeutralRules.ScanPackageArtifacts(windowsPath, ".env.template", allowWindowsHostPaths: false));
        Assert.Empty(EnvironmentNeutralRules.ScanPackageArtifacts(windowsPath, ".env.template", allowWindowsHostPaths: true));
    }

    [Fact]
    public void ProfileNeutralityPolicy_reads_active_profile_and_network()
    {
        const string env = """
                           DOTNET_ENVIRONMENT=Production
                           AOVIAS_ACTIVE_PROFILE=demo
                           AOVIAS_DOCKER_NETWORK=aovias-network
                           """;

        Assert.Equal("demo", ProfileNeutralityPolicy.ReadActiveProfile(env));
        Assert.Equal("aovias-network", ProfileNeutralityPolicy.ReadDockerNetwork(env));
    }

    [Fact]
    public void Resolver_rejects_profile_with_developer_path_leakage()
    {
        var root = CreateProfile("leaky", hostConfigRoot: "V:/DevelopmentRepo/V1.0/config");
        try
        {
            Assert.Throws<InvalidOperationException>(() => DeploymentPathResolver.Resolve(root, "leaky"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Resolver_rejects_container_root_that_is_not_linux_path()
    {
        var root = CreateProfile("badcontainer", configRoot: "C:/aovias/config");
        try
        {
            Assert.Throws<InvalidOperationException>(() => DeploymentPathResolver.Resolve(root, "badcontainer"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Resolver_produces_complete_environment_variable_map()
    {
        var root = CreateProfile("good");
        try
        {
            var profile = DeploymentPathResolver.Resolve(root, "good");
            var env = profile.ToEnvironmentVariables();

            Assert.Contains(env, pair => pair.Key == "AOVIAS_ACTIVE_PROFILE" && pair.Value == "good");
            Assert.Contains(env, pair => pair.Key == "AOVIAS_DOCKER_NETWORK" && pair.Value == "aovias-network");
            Assert.DoesNotContain(env, pair => pair.Value.Contains("/path/to", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(env, pair => pair.Value.Contains("{{", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateProfile(
        string name,
        string configRoot = "/opt/aovias/runtime/config",
        string hostConfigRoot = "/opt/aovias/runtime/config")
    {
        var root = Path.Combine(Path.GetTempPath(), $"aovias-profile-{Guid.NewGuid():N}");
        var profileDir = Path.Combine(root, "profiles", name);
        Directory.CreateDirectory(profileDir);
        var json = $$"""
                     {
                       "dotNetEnvironment": "Production",
                       "configRoot": "{{configRoot}}",
                       "packagesRoot": "/opt/aovias/runtime/packages",
                       "logsRoot": "/opt/aovias/runtime/logs",
                       "hostConfigRoot": "{{hostConfigRoot}}",
                       "hostPackagesRoot": "/opt/aovias/runtime/packages",
                       "hostLogsRoot": "/opt/aovias/runtime/logs",
                       "dockerNetwork": "aovias-network",
                       "profilesDirectory": "profiles",
                       "secretsDirectory": "secrets"
                     }
                     """;
        File.WriteAllText(Path.Combine(profileDir, "settings.deployment-host.json"), json);
        return root;
    }
}
