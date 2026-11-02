using AccredaOne.Api.Persistence;
using AccredaOne.Api.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AccredaOne.Api.Tests;

public class PersistenceConfigurationTests
{
    [Fact]
    public void Missing_persistence_connection_string_is_valid_for_startup()
    {
        using var factory = new AccredaOneWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;

        Assert.False(options.HasConnectionString);
        Assert.False(options.EnableConnectivity);
    }

    [Fact]
    public void Valid_configuration_binds_from_environment_style_keys()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:ConnectionString"] = "Host=localhost;Port=5432;Database=accredaone;Username=app;Password=placeholder",
                ["Persistence:EnableConnectivity"] = "false",
                ["Persistence:ConnectionTimeoutSeconds"] = "10",
                ["Persistence:CommandTimeoutSeconds"] = "20"
            })
            .Build();

        var options = new PersistenceOptions();
        configuration.GetSection(PersistenceOptions.SectionName).Bind(options);

        Assert.True(options.HasConnectionString);
        Assert.False(options.EnableConnectivity);
        Assert.Equal(10, options.ConnectionTimeoutSeconds);
        Assert.Equal(20, options.CommandTimeoutSeconds);
    }

    [Fact]
    public void Invalid_configuration_fails_validation_without_leaking_secrets()
    {
        const string secretPassword = "SuperSecret-NotForLogs-9f3a2b";
        var options = new PersistenceOptions
        {
            ConnectionString =
                $"Host=localhost;Port=5432;Database=accredaone;Username=app;Password={secretPassword}",
            EnableConnectivity = true,
            ConnectionTimeoutSeconds = 0
        };

        var validator = new PersistenceOptionsValidator();
        var result = validator.Validate(null, options);

        Assert.False(result.Succeeded);
        var message = string.Join(' ', result.Failures ?? []);
        Assert.DoesNotContain(secretPassword, message);
        Assert.DoesNotContain("Password=", message);
    }

    [Fact]
    public void Invalid_connection_string_fails_validation()
    {
        var options = new PersistenceOptions
        {
            ConnectionString = "not-a-valid-npgsql-connection-string",
            EnableConnectivity = false
        };

        var validator = new PersistenceOptionsValidator();
        var result = validator.Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Host_rejects_invalid_persistence_configuration_at_startup()
    {
        Assert.ThrowsAny<Exception>(() =>
        {
            using var factory = new AccredaOneWebApplicationFactory(useSettings: new Dictionary<string, string?>
            {
                ["Persistence:EnableConnectivity"] = "true"
            });

            factory.CreateClient();
        });
    }
}
