using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AccredaOne.Api.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccredaOnePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<PersistenceOptions>, PersistenceOptionsValidator>();
        services.AddSingleton<IPersistenceReadinessEvaluator, PersistenceReadinessEvaluator>();

        services.AddDbContext<AccredaOneDbContext>((serviceProvider, options) =>
        {
            var persistence = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            if (!persistence.HasConnectionString)
            {
                return;
            }

            var connectionBuilder = new NpgsqlConnectionStringBuilder(persistence.ConnectionString!)
            {
                Timeout = persistence.ConnectionTimeoutSeconds
            };

            options.UseNpgsql(connectionBuilder.ConnectionString, npgsql =>
            {
                npgsql.SetPostgresVersion(18, 0);
                npgsql.CommandTimeout(persistence.CommandTimeoutSeconds);
            });
        });

        services.AddHealthChecks()
            .AddCheck<PostgresConnectivityHealthCheck>(
                PostgresConnectivityHealthCheckRegistration.Name,
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "database"]);

        return services;
    }
}
