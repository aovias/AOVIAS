using Aovias.Configuration.SignalR;
using Xunit;

namespace Aovias.Configuration.Tests;

public sealed class SignalRHubEndpointResolverTests
{
    [Fact]
    public void ResolveHubUrl_preserves_path_base_for_demo_profile()
    {
        var url = SignalRHubEndpointResolver.ResolveHubUrl(
            "https://demo.praman.cloud/runtime",
            "/runtime",
            "/hubs/deployment");

        Assert.Equal("https://demo.praman.cloud/runtime/hubs/deployment", url);
    }

    [Fact]
    public void ResolveHubUrl_does_not_drop_base_url_path_segment()
    {
        var broken = new Uri(new Uri("https://demo.praman.cloud/runtime"), "/hubs/deployment");
        Assert.Equal("https://demo.praman.cloud/hubs/deployment", broken.ToString());

        var fixedUrl = SignalRHubEndpointResolver.ResolveHubUrl(
            "https://demo.praman.cloud/runtime",
            "/runtime",
            "/hubs/deployment");

        Assert.Equal("https://demo.praman.cloud/runtime/hubs/deployment", fixedUrl);
    }

    [Fact]
    public void ResolveNegotiatePath_includes_version_query()
    {
        var negotiate = SignalRHubEndpointResolver.ResolveNegotiatePath("/runtime", "/hubs/deployment");
        Assert.Equal("/runtime/hubs/deployment/negotiate?negotiateVersion=1", negotiate);
    }

    [Theory]
    [InlineData("/usr/share/Aovias")]
    [InlineData("/api/runtime")]
    public void ResolveHubPath_rejects_conflicting_paths_via_configuration_not_hardcoded_urls(string hubPath)
    {
        var resolved = SignalRHubEndpointResolver.ResolveHubPath(string.Empty, hubPath);
        Assert.Equal(hubPath, resolved);
    }

    [Fact]
    public void GetConnectionCandidateHubPaths_includes_configured_and_legacy_paths()
    {
        var candidates = SignalRHubEndpointResolver.GetConnectionCandidateHubPaths(
            discoveredHubPath: null,
            configuredHubPath: "/hubs/deployment");

        Assert.Equal(["/hubs/deployment", "/hubs/runtime", "/api/runtime"], candidates);
    }
}
