using Aovias.Configuration.Deployment;
using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Infrastructure.Deployment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aovias.DeploymentValidation.Tests.Reliability;

internal static class RuntimeAgentDeploymentTestSupport
{
    public static RuntimeAgentDeploymentPathResolver CreateResolver(string releaseRoot, string packageRoot)
    {
        var runtimeRoot = Path.GetDirectoryName(packageRoot)
            ?? throw new InvalidOperationException("Package root must include a parent directory.");
        var configRoot = Path.Combine(runtimeRoot, "config");
        var logsRoot = Path.Combine(runtimeRoot, "logs");

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [$"{DeploymentStorageSettings.SectionName}:Root"] = string.Empty,
            [$"{ReleaseManagementSettings.SectionName}:ReleaseRoot"] = releaseRoot,
            [$"{DeploymentHostSettings.SectionName}:HostConfigRoot"] = configRoot,
            [$"{DeploymentHostSettings.SectionName}:HostPackagesRoot"] = packageRoot,
            [$"{DeploymentHostSettings.SectionName}:HostLogsRoot"] = logsRoot,
            [$"{DeploymentHostSettings.SectionName}:ConfigRoot"] = configRoot,
            [$"{DeploymentHostSettings.SectionName}:PackagesRoot"] = packageRoot,
            [$"{DeploymentHostSettings.SectionName}:LogsRoot"] = logsRoot,
            [$"{DeploymentHostSettings.SectionName}:DotNetEnvironment"] = "Production",
            [$"{DeploymentHostSettings.SectionName}:DockerNetwork"] = "aovias-network"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var deploymentRoot = ConfiguredDeploymentPathResolver.NormalizeLegacyDeploymentRoot(
            configuredRoot: null,
            legacyReleaseRoot: releaseRoot);

        var configured = new ConfiguredDeploymentPathResolver(
            Options.Create(new DeploymentStorageSettings { Root = deploymentRoot }),
            Options.Create(new DeploymentHostSettings
            {
                DotNetEnvironment = "Production",
                ConfigRoot = configRoot,
                PackagesRoot = packageRoot,
                LogsRoot = logsRoot,
                HostConfigRoot = configRoot,
                HostPackagesRoot = packageRoot,
                HostLogsRoot = logsRoot,
                DockerNetwork = "aovias-network"
            }),
            configuration);

        return new RuntimeAgentDeploymentPathResolver(configured);
    }
}
