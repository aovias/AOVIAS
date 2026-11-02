using AccredaOne.Api.Persistence;
using AccredaOne.Api.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccredaOne.Api.Tests;

public class PersistenceDependencyInjectionTests
{
    [Fact]
    public void Application_starts_without_persistence_connection_string()
    {
        using var factory = new AccredaOneWebApplicationFactory();
        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void DbContext_can_be_resolved_without_connection_string()
    {
        using var factory = new AccredaOneWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetService<AccredaOneDbContext>();
        Assert.NotNull(dbContext);
    }

    [Fact]
    public void DbContext_is_registered_when_connection_string_is_valid()
    {
        using var factory = AccredaOneWebApplicationFactory.WithPersistenceConnectionString(
            "Host=localhost;Port=5432;Database=accredaone;Username=app;Password=placeholder");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccredaOneDbContext>();
        Assert.NotNull(dbContext);
    }

    [Fact]
    public void Resolving_DbContext_does_not_require_a_running_database_server()
    {
        using var factory = AccredaOneWebApplicationFactory.WithPersistenceConnectionString(
            "Host=localhost;Port=5432;Database=accredaone;Username=app;Password=placeholder");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccredaOneDbContext>();
        Assert.NotNull(dbContext);
    }
}
