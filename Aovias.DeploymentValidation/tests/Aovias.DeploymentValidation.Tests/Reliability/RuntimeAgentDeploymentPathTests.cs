using System.Text.RegularExpressions;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Infrastructure.Deployment;
using Xunit;
namespace Aovias.DeploymentValidation.Tests.Reliability;

/// <summary>
/// Guards Runtime Agent deployment persistence against hardcoded system paths.
/// </summary>
public sealed class RuntimeAgentDeploymentPathTests
{
    private static readonly string[] ForbiddenSourcePatterns =
    [
        "Environment.SpecialFolder.CommonApplicationData",
        @"""\/usr\/share",
        @"""\/etc\/",
        @"""\/var\/",
        @"AppContext\.BaseDirectory.*deployment-packages"
    ];

    [Theory]
    [InlineData("/usr/share/Aovias")]
    [InlineData("/etc/aovias/deployment")]
    [InlineData("/var/lib/aovias")]
    public void ValidateWritablePath_rejects_hardcoded_system_directories(string path)
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var resolver = RuntimeAgentDeploymentTestSupport.CreateResolver(
            releaseRoot: "/opt/aovias/releases",
            packageRoot: "/opt/aovias/runtime/packages");

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.ValidateWritablePath(path));
        Assert.Contains("not allowed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateWritablePath_rejects_common_application_data_paths()
    {
        var resolver = RuntimeAgentDeploymentTestSupport.CreateResolver(
            releaseRoot: "/opt/aovias/releases",
            packageRoot: "/opt/aovias/runtime/packages");
        var commonApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var forbiddenPath = Path.Combine(commonApplicationData, "Aovias", "deployment-sessions");

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.ValidateWritablePath(forbiddenPath));
        Assert.Contains("CommonApplicationData", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Session_store_resolves_under_configured_runtime_root()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"aovias-runtime-{Guid.NewGuid():N}");
        var releaseRoot = Path.Combine(tempRoot, "releases");
        var packageRoot = Path.Combine(tempRoot, "runtime", "packages");

        var resolver = RuntimeAgentDeploymentTestSupport.CreateResolver(releaseRoot, packageRoot);
        var sessionRoot = resolver.ResolveSessionStoreRoot();

        // Sprint G1: session store paths are always forward-slash normalized (platform-wide
        // convention, ConfiguredDeploymentPathResolver.NormalizePath), while tempRoot here uses
        // the OS-native separator — normalize both sides for the comparison, not just report it.
        Assert.StartsWith(tempRoot.Replace('\\', '/'), sessionRoot, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deployment-sessions", sessionRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deployment_session_store_writes_only_under_configured_paths()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"aovias-runtime-{Guid.NewGuid():N}");
        var releaseRoot = Path.Combine(tempRoot, "releases");
        var packageRoot = Path.Combine(tempRoot, "runtime", "packages");
        var resolver = RuntimeAgentDeploymentTestSupport.CreateResolver(releaseRoot, packageRoot);
        var store = new DeploymentSessionStore(resolver);

        try
        {
            var record = new PlatformDeploymentRecord
            {
                DeploymentId = Guid.NewGuid().ToString("N"),
                Operator = "system",
                Runtime = "docker",
                Environment = "demo",
                State = PlatformDeploymentState.Deploying
            };

            store.Save(record);

            Assert.StartsWith(tempRoot.Replace('\\', '/'), resolver.ResolveSessionStoreRoot(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains(store.ListRecoverable(), item => item.DeploymentId == record.DeploymentId);
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
    public void Runtime_agent_deployment_sources_must_not_use_forbidden_write_paths()
    {
        var infrastructureRoot = LocateInfrastructureSourceRoot();
        var deploymentSources = Directory.EnumerateFiles(
                Path.Combine(infrastructureRoot, "Deployment"),
                "*.cs",
                SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(
                Path.Combine(infrastructureRoot, "Persistence"),
                "*.cs",
                SearchOption.AllDirectories))
            .ToList();

        Assert.NotEmpty(deploymentSources);

        var violations = new List<string>();
        foreach (var file in deploymentSources)
        {
            if (string.Equals(
                    Path.GetFileName(file),
                    "RuntimeAgentDeploymentPathResolver.cs",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            foreach (var pattern in ForbiddenSourcePatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    violations.Add($"{Path.GetRelativePath(infrastructureRoot, file)} matches `{pattern}`");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Runtime Agent deployment persistence must resolve writable paths from AOVIAS configuration only:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    private static string LocateInfrastructureSourceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "Aovias.Runtime.Agent",
                "src",
                "Aovias.Runtime.Agent.Infrastructure");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate Aovias.Runtime.Agent.Infrastructure sources.");
    }
}
