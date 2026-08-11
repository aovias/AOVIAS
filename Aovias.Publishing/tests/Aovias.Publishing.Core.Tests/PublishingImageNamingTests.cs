using Aovias.Publishing.Core.Common;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class PublishingImageNamingTests
{
    [Theory]
    [InlineData("gateway", "aovias/gateway")]
    [InlineData("compliance-service", "aovias/compliance-service")]
    [InlineData("runtime-agent", "aovias/runtime-agent")]
    public void GetImageName_uses_aovias_prefix(string serviceId, string expected)
    {
        Assert.Equal(expected, PublishingImageNaming.GetImageName(serviceId));
    }

    [Theory]
    [InlineData("gateway", "gateway.aovdeploy")]
    [InlineData("portal", "portal.aovdeploy")]
    public void GetPackageFileName_uses_service_id(string serviceId, string expected)
    {
        Assert.Equal(expected, PublishingImageNaming.GetPackageFileName(serviceId));
    }
}
