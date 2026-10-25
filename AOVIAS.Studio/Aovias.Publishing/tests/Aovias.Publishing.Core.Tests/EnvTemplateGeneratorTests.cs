using Aovias.Deployment.Contracts;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Generation;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class EnvTemplateGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_emits_environment_neutral_template()
    {
        var generator = new EnvironmentFileGenerator();
        var context = new PublishingContext
        {
            Request = new PublishingRequest
            {
                ProjectPath = ".",
                ServiceId = "runtime-agent",
                Version = "1.0.0"
            }
        };

        var content = await generator.GenerateAsync(context, CancellationToken.None);

        Assert.Contains("AOVIAS_HOST_CONFIG_ROOT=${AOVIAS_HOST_CONFIG_ROOT}", content, StringComparison.Ordinal);
        Assert.Contains("AOVIAS_ACTIVE_PROFILE=${AOVIAS_ACTIVE_PROFILE}", content, StringComparison.Ordinal);
        Assert.Contains("AOVIAS_DOCKER_NETWORK=${AOVIAS_DOCKER_NETWORK}", content, StringComparison.Ordinal);
        Assert.Contains($"AOVIAS_CONFIG_ROOT={PlatformRuntimeDefaults.ConfigRoot}", content, StringComparison.Ordinal);
        Assert.DoesNotContain("AOVIAS_ACTIVE_PROFILE=demo", content, StringComparison.Ordinal);
        Assert.DoesNotContain("AOVIAS_HOST_CONFIG_ROOT=/opt/aovias", content, StringComparison.Ordinal);
        Assert.DoesNotContain("C:/", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_is_identical_regardless_of_publishing_profile_setting()
    {
        var generator = new EnvironmentFileGenerator();
        var first = await generator.GenerateAsync(CreateContext(), CancellationToken.None);
        var second = await generator.GenerateAsync(CreateContext(), CancellationToken.None);

        Assert.Equal(first, second);
    }

    private static PublishingContext CreateContext()
        => new()
        {
            Request = new PublishingRequest
            {
                ProjectPath = ".",
                ServiceId = "gateway",
                Version = "1.0.0",
                Profile = "demo"
            }
        };
}
