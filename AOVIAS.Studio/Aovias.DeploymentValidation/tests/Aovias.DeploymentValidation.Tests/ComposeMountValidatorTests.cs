using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Validation;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

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

        Assert.Contains(
            bundle.Filesystem.Directories,
            directory => directory.Path == PlatformDeploymentDefaults.IncomingRoot);
    }

    [Fact]
    public void Validate_RuntimeAgentCompose_PassesRequiredMounts()
    {
        var bundle = BuildRuntimeAgentBundle();
        var compose = BuildSampleCompose(bundle.Volume);

        var report = ComposeMountValidator.Validate(compose, bundle.Volume, bundle.Filesystem);

        Assert.True(report.Passed, string.Join("; ", report.Errors));
        Assert.Contains(report.Checks, check => check.Label == "Deployment Root" && check.Status == "PASS");
        Assert.Contains(report.Checks, check => check.Label == "Incoming" && check.Status == "PASS");
        Assert.Contains(report.Checks, check => check.Label == "Runtime Config" && check.Status == "PASS");
        Assert.Contains(report.Checks, check => check.Label == "Runtime Packages" && check.Status == "PASS");
        Assert.Contains(report.Checks, check => check.Label == "Runtime Logs" && check.Status == "PASS");
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
        Assert.Contains(report.Errors, error => error.Contains(PlatformDeploymentDefaults.DeploymentRoot, StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MissingRuntimePackageMount_FailsPublishing()
    {
        var bundle = BuildRuntimeAgentBundle();
        var volumeWithoutPackages = new VolumeContract
        {
            Mounts = bundle.Volume.Mounts
                .Where(mount => mount.ContainerPath != PlatformRuntimeDefaults.PackagesRoot)
                .ToList()
        };
        var compose = BuildSampleCompose(volumeWithoutPackages);

        var report = ComposeMountValidator.Validate(compose, volumeWithoutPackages, bundle.Filesystem);

        Assert.False(report.Passed);
        Assert.Contains(report.Errors, error => error.Contains(PlatformRuntimeDefaults.PackagesRoot, StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReadOnlyConfigMount_RequiresRoSuffix()
    {
        var bundle = BuildRuntimeAgentBundle();
        var volume = new VolumeContract
        {
            Mounts =
            [
                new VolumeMountContract
                {
                    HostToken = "${AOVIAS_HOST_CONFIG_ROOT}",
                    ContainerPath = PlatformRuntimeDefaults.ConfigRoot,
                    ReadOnly = true
                }
            ]
        };
        var compose = """
                      services:
                        runtime-agent:
                          volumes:
                            - ${AOVIAS_HOST_CONFIG_ROOT}:/opt/aovias/runtime/config
                      """;

        var report = ComposeMountValidator.Validate(compose, volume, bundle.Filesystem);

        Assert.False(report.Passed);
        Assert.Contains(report.Errors, error => error.Contains("read-only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_DuplicateContainerPath_FailsPublishing()
    {
        var compose = """
                      services:
                        runtime-agent:
                          volumes:
                            - ${AOVIAS_HOST_LOGS_ROOT}:/opt/aovias/runtime/logs
                            - ${AOVIAS_HOST_LOGS_ROOT}:/opt/aovias/runtime/logs
                      """;
        var volume = new VolumeContract
        {
            Mounts =
            [
                new VolumeMountContract { HostToken = "${AOVIAS_HOST_LOGS_ROOT}", ContainerPath = PlatformRuntimeDefaults.LogsRoot }
            ]
        };

        var report = ComposeMountValidator.Validate(compose, volume);

        Assert.False(report.Passed);
        Assert.Contains(report.Errors, error => error.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
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
