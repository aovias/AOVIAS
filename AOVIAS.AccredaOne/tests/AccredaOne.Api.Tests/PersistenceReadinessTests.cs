using System.Net;
using AccredaOne.Api.Tests.Support;
using Microsoft.Extensions.Configuration;

namespace AccredaOne.Api.Tests;

public class PersistenceReadinessTests
{
    [Fact]
    public async Task Health_remains_healthy_without_database_configuration()
    {
        using var factory = new AccredaOneWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body);
    }

    [Fact]
    public async Task Ready_reports_not_configured_without_database_configuration()
    {
        using var factory = new AccredaOneWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("not_configured", body);
    }

    [Fact]
    public async Task Ready_reports_disabled_when_connection_string_exists_but_connectivity_is_off()
    {
        using var factory = AccredaOneWebApplicationFactory.WithPersistenceConnectionString(
            "Host=localhost;Port=5432;Database=accredaone;Username=app;Password=placeholder");

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("disabled", body);
    }

    [Fact]
    public async Task Ready_response_does_not_expose_connection_secrets()
    {
        const string secretPassword = "ReadyEndpointSecret-7c4e1d";
        using var factory = AccredaOneWebApplicationFactory.WithPersistenceConnectionString(
            $"Host=localhost;Port=5432;Database=accredaone;Username=app;Password={secretPassword}");

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(secretPassword, body);
        Assert.DoesNotContain("Password=", body);
    }

    [Fact]
    public async Task Default_host_does_not_perform_live_database_health_verification()
    {
        using var factory = new AccredaOneWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("healthy", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not_configured", body);
    }
}
