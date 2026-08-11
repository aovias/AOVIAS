using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace ComplianceService.Infrastructure.Persistence;

public class ComplianceDbContextFactory : IDesignTimeDbContextFactory<ComplianceDbContext>
{
    public ComplianceDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Design-time tools resolve config from the project directory.
        var basePath = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
        {
            basePath = Path.GetFullPath(Path.Combine(basePath, "ComplianceService"));
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ComplianceDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        optionsBuilder
            .UseSqlServer(connectionString)
            // Allow applying existing migration files when the snapshot still lags the model.
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new ComplianceDbContext(optionsBuilder.Options);
    }
}
