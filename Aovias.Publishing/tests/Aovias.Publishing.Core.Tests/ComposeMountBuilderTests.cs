using Aovias.Deployment.Contracts;
using Aovias.Publishing.Core.Generation.Compose;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class ComposeMountBuilderTests
{
    [Fact]
    public void BuildVolumeEntries_PreventsDuplicateMounts()
    {
        var volume = new VolumeContract
        {
            Mounts =
            [
                new VolumeMountContract { HostToken = "${AOVIAS_HOST_LOGS_ROOT}", ContainerPath = PlatformRuntimeDefaults.LogsRoot },
                new VolumeMountContract { HostToken = "${AOVIAS_HOST_LOGS_ROOT}", ContainerPath = PlatformRuntimeDefaults.LogsRoot }
            ]
        };

        var exception = Assert.Throws<InvalidOperationException>(() => ComposeMountBuilder.BuildVolumeEntries(volume));
        Assert.Contains("Duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatMount_AppendsReadOnlySuffix()
    {
        var mount = new VolumeMountContract
        {
            HostToken = "${AOVIAS_HOST_CONFIG_ROOT}",
            ContainerPath = PlatformRuntimeDefaults.ConfigRoot,
            ReadOnly = true
        };

        Assert.Equal(
            "${AOVIAS_HOST_CONFIG_ROOT}:/opt/aovias/runtime/config:ro",
            ComposeMountBuilder.FormatMount(mount));
    }
}
