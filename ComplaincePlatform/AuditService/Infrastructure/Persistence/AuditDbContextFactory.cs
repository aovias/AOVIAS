using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AuditService.Infrastructure.Persistence;

public class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AuditDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        optionsBuilder.UseSqlServer(connectionString);

        return new AuditDbContext(optionsBuilder.Options);
    }
}
