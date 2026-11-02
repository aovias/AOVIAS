using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AccredaOne.Api.Tests;

public class ReadinessEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ReadinessEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ready_reports_configuration_without_a_database()
    {
        var response = await _client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Ready", body);
        Assert.Contains("not_configured", body);
        Assert.Contains("AccredaOne.Api", body);
    }
}
