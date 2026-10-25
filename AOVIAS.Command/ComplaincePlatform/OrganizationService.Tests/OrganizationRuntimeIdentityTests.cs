using Xunit;

namespace OrganizationService.Tests;

public class OrganizationRuntimeIdentityTests
{
    [Fact]
    public void Payload_identifies_this_OrganizationService_build()
    {
        var payload = OrganizationRuntimeIdentity.CreatePayload("Development", "1.0.0");

        Assert.Equal("OrganizationService", payload.Service);
        Assert.Equal("v28-mode-b-backend-isolation", payload.RuntimeIdentity);
        Assert.Equal("Development", payload.Environment);
        Assert.Equal("1.0.0", payload.InformationalVersion);
        Assert.Equal("X-Aovias-Runtime-Identity", OrganizationRuntimeIdentity.HeaderName);
    }
}
