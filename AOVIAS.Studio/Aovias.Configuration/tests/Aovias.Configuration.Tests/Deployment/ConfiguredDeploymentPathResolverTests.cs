using Aovias.Configuration.Deployment;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aovias.Configuration.Tests.Deployment;

public sealed class ConfiguredDeploymentPathResolverTests
{
    [Fact]
    public void LinuxDeploymentPaths_DeriveFromConfiguredRoot()
    {
        var resolver = CreateResolver(
            root: "/opt/aovias/deployments",
            hostConfig: "/opt/aovias/runtime/config",
            hostPackages: "/opt/aovias/runtime/packages",
            hostLogs: "/opt/aovias/runtime/logs");

        Assert.Equal("/opt/aovias/deployments", resolver.DeploymentRoot);
        Assert.Equal("/opt/aovias/deployments/incoming", resolver.IncomingRoot);
        Assert.Equal("/opt/aovias/deployments/releases", resolver.ReleaseRoot);
        Assert.Equal("/opt/aovias/deployments/temp", resolver.TempRoot);
        Assert.Equal("/opt/aovias/runtime", resolver.RuntimeRoot);
    }

    [Fact]
    public void WindowsDevelopmentPaths_DeriveFromConfiguredRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "aovias-dev-deployments");
        var resolver = CreateResolver(
            root: root,
            hostConfig: Path.Combine(root, "..", "runtime", "config"),
            hostPackages: Path.Combine(root, "..", "runtime", "packages"),
            hostLogs: Path.Combine(root, "..", "runtime", "logs"));

        Assert.StartsWith(root.Replace('\\', '/'), resolver.DeploymentRoot.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("/incoming", resolver.IncomingRoot.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegacyReleaseRoot_MigratesToDeploymentsRoot()
    {
        var root = ConfiguredDeploymentPathResolver.NormalizeLegacyDeploymentRoot(
            configuredRoot: null,
            legacyReleaseRoot: "/opt/aovias/releases");

        Assert.Equal("/opt/aovias/deployments", root);
    }

    [Fact]
    public void LegacyReleaseRoot_IsMigratedWhenConfiguredAsRoot()
    {
        var root = ConfiguredDeploymentPathResolver.NormalizeLegacyDeploymentRoot(
            configuredRoot: "/opt/aovias/releases",
            legacyReleaseRoot: null);

        Assert.Equal("/opt/aovias/deployments", root);
    }

    [Fact]
    public void CustomDeploymentRoot_ResolvesRelativeSegments()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var resolver = CreateResolver(
            root: tempRoot,
            hostConfig: Path.Combine(tempRoot, "runtime", "config"),
            hostPackages: Path.Combine(tempRoot, "runtime", "packages"),
            hostLogs: Path.Combine(tempRoot, "runtime", "logs"));

        Assert.Equal(Path.Combine(tempRoot, "incoming").Replace('\\', '/'), resolver.IncomingRoot.Replace('\\', '/'));
        Assert.Equal(Path.Combine(tempRoot, "releases").Replace('\\', '/'), resolver.ReleaseRoot.Replace('\\', '/'));
    }

    [Fact]
    public void ValidateStorageLayout_CreatesMissingDirectories()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var resolver = CreateResolver(
            root: tempRoot,
            hostConfig: Path.Combine(tempRoot, "runtime", "config"),
            hostPackages: Path.Combine(tempRoot, "runtime", "packages"),
            hostLogs: Path.Combine(tempRoot, "runtime", "logs"));

        var result = resolver.ValidateStorageLayout(createMissing: true);

        try
        {
            Assert.True(result.IsValid);
            Assert.True(Directory.Exists(resolver.IncomingRoot));
            Assert.True(Directory.Exists(resolver.ReleaseRoot));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ValidateStorageLayout_ReadOnlyConfigMount_DoesNotRequireWriteAccess()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var configRoot = Path.Combine(tempRoot, "runtime", "config");
        var logsRoot = Path.Combine(tempRoot, "runtime", "logs");
        var packagesRoot = Path.Combine(tempRoot, "runtime", "packages");
        Directory.CreateDirectory(configRoot);
        Directory.CreateDirectory(logsRoot);
        Directory.CreateDirectory(packagesRoot);
        File.WriteAllText(Path.Combine(configRoot, "settings.json"), "{}");

        var resolver = CreateResolver(
            root: Path.Combine(tempRoot, "deployments"),
            hostConfig: configRoot,
            hostPackages: packagesRoot,
            hostLogs: logsRoot);

        var result = resolver.ValidateStorageLayout(createMissing: true);

        try
        {
            Assert.True(result.IsValid, string.Join("; ", result.Checks.Where(check => check.Status != "PASS").Select(check => check.Detail)));
            Assert.Equal("PASS", result.Checks.Single(check => check.Label == "Config").Status);
            Assert.Equal("PASS", result.Checks.Single(check => check.Label == "Runtime").Status);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void MissingRoot_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => CreateResolver(
            root: string.Empty,
            hostConfig: "/opt/aovias/runtime/config",
            hostPackages: "/opt/aovias/runtime/packages",
            hostLogs: "/opt/aovias/runtime/logs",
            legacyReleaseRoot: null));
    }

    private static ConfiguredDeploymentPathResolver CreateResolver(
        string root,
        string hostConfig,
        string hostPackages,
        string hostLogs,
        string? legacyReleaseRoot = null)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{DeploymentStorageSettings.SectionName}:Root"] = root,
            [$"{DeploymentHostSettings.SectionName}:HostConfigRoot"] = hostConfig,
            [$"{DeploymentHostSettings.SectionName}:HostPackagesRoot"] = hostPackages,
            [$"{DeploymentHostSettings.SectionName}:HostLogsRoot"] = hostLogs,
            [$"{DeploymentHostSettings.SectionName}:ConfigRoot"] = hostConfig,
            [$"{DeploymentHostSettings.SectionName}:PackagesRoot"] = hostPackages,
            [$"{DeploymentHostSettings.SectionName}:LogsRoot"] = hostLogs,
            [$"{DeploymentHostSettings.SectionName}:DotNetEnvironment"] = "Production",
            [$"{DeploymentHostSettings.SectionName}:DockerNetwork"] = "aovias"
        };

        if (!string.IsNullOrWhiteSpace(legacyReleaseRoot))
        {
            values[$"{ReleaseManagementSettings.SectionName}:ReleaseRoot"] = legacyReleaseRoot;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new ConfiguredDeploymentPathResolver(
            Options.Create(new DeploymentStorageSettings { Root = root }),
            Options.Create(new DeploymentHostSettings
            {
                DotNetEnvironment = "Production",
                ConfigRoot = hostConfig,
                PackagesRoot = hostPackages,
                LogsRoot = hostLogs,
                HostConfigRoot = hostConfig,
                HostPackagesRoot = hostPackages,
                HostLogsRoot = hostLogs,
                DockerNetwork = "aovias"
            }),
            configuration);
    }
}
