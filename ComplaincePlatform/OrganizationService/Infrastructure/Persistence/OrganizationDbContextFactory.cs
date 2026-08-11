using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OrganizationService.Infrastructure.Persistence;

public class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<OrganizationDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        optionsBuilder.UseSqlServer(connectionString);

        return new OrganizationDbContext(optionsBuilder.Options);
    }
}
