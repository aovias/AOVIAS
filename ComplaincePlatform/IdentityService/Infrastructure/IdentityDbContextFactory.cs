using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Infrastructure;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        optionsBuilder.UseSqlServer(connectionString);

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
