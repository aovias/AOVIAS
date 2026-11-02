using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AccredaOne.Api.Tests;

public class FoundationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FoundationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Current_version_returns_foundation_status()
    {
        var response = await _client.GetAsync("/api/v1/foundation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("AccredaOne.Api", body);
        Assert.Contains("not_connected", body);
        Assert.Contains("not_implemented", body);
    }

    [Fact]
    public async Task Unknown_version_returns_problem_details()
    {
        var response = await _client.GetAsync("/api/v9/foundation");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unknown API version", body);
    }

    [Fact]
    public async Task Unknown_path_returns_problem_details()
    {
        var response = await _client.GetAsync("/api/v1/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApi_document_is_available_in_development()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("/health", body);
        Assert.Contains("/ready", body);
    }
}
