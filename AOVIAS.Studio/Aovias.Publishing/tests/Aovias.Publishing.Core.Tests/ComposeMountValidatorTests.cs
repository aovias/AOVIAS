using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Validation;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class ComposeMountValidatorTests
{
    [Fact]
    public void RuntimeAgentContract_IncludesDeploymentStorageMount()
    {
        var bundle = BuildRuntimeAgentBundle();

        Assert.Contains(
            bundle.Volume.Mounts,
            mount => mount.ContainerPath == PlatformDeploymentDefaults.DeploymentRoot
                     && mount.HostToken == PlatformDeploymentDefaults.HostDeploymentsRootToken);
    }

    [Fact]
    public void Validate_MissingDeploymentMount_FailsPublishing()
    {
        var bundle = BuildRuntimeAgentBundle();
        var volumeWithoutDeployment = new VolumeContract
        {
            Mounts = bundle.Volume.Mounts
                .Where(mount => mount.ContainerPath != PlatformDeploymentDefaults.DeploymentRoot)
                .ToList()
        };
        var compose = BuildSampleCompose(volumeWithoutDeployment);

        var report = ComposeMountValidator.Validate(compose, volumeWithoutDeployment, bundle.Filesystem);

        Assert.False(report.Passed);
    }

    [Fact]
    public void Validate_QuotedComposeVolumes_AreParsed()
    {
        var bundle = BuildRuntimeAgentBundle();
        var compose = """
                      services:
                        runtime-agent:
                          volumes:
                            - "${AOVIAS_HOST_CONFIG_ROOT}:/opt/aovias/runtime/config:ro"
                            - "${AOVIAS_HOST_LOGS_ROOT}:/opt/aovias/runtime/logs"
                            - "${AOVIAS_HOST_PACKAGES_ROOT}:/opt/aovias/runtime/packages"
                            - "${AOVIAS_HOST_DEPLOYMENTS_ROOT}:/opt/aovias/deployments"
                      """;

        var report = ComposeMountValidator.Validate(compose, bundle.Volume, bundle.Filesystem);

        Assert.True(report.Passed, string.Join("; ", report.Errors));
    }

    private static ServiceDeploymentBundle BuildRuntimeAgentBundle()
        => ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "runtime-agent",
            DisplayName = "Runtime Agent",
            Version = "1.0.0",
            ImageReference = "aovias/runtime-agent:1.0.0",
            ContainerName = "runtime-agent",
            EntryPoint = "Aovias.Runtime.Agent.Api.dll",
            HealthEndpoint = "/health",
            Ports = [7080]
        });

    private static string BuildSampleCompose(VolumeContract volume)
    {
        var lines = new List<string>
        {
            "services:",
            "  runtime-agent:",
            "    volumes:"
        };

        foreach (var mount in volume.Mounts.OrderBy(item => item.ContainerPath, StringComparer.Ordinal))
        {
            var entry = $"{mount.HostToken}:{mount.ContainerPath}";
            if (mount.ReadOnly)
            {
                entry += ":ro";
            }

            lines.Add($"      - {entry}");
        }

        return string.Join('\n', lines);
    }
}
