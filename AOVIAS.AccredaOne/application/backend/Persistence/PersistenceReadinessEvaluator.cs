using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AccredaOne.Api.Persistence;

public interface IPersistenceReadinessEvaluator
{
    Task<string> GetDatabaseReadinessStatusAsync(CancellationToken cancellationToken = default);
}

public sealed class PersistenceReadinessEvaluator : IPersistenceReadinessEvaluator
{
    private readonly IOptions<PersistenceOptions> _options;
    private readonly HealthCheckService _healthCheckService;

    public PersistenceReadinessEvaluator(
        IOptions<PersistenceOptions> options,
        HealthCheckService healthCheckService)
    {
        _options = options;
        _healthCheckService = healthCheckService;
    }

    public async Task<string> GetDatabaseReadinessStatusAsync(CancellationToken cancellationToken = default)
    {
        var settings = _options.Value;
        if (!settings.HasConnectionString)
        {
            return DatabaseReadinessStatus.NotConfigured;
        }

        if (!settings.EnableConnectivity)
        {
            return DatabaseReadinessStatus.Disabled;
        }

        var report = await _healthCheckService.CheckHealthAsync(
            registration => registration.Name == PostgresConnectivityHealthCheckRegistration.Name,
            cancellationToken);

        return report.Status == HealthStatus.Healthy
            ? DatabaseReadinessStatus.Healthy
            : DatabaseReadinessStatus.Unhealthy;
    }
}

internal static class PostgresConnectivityHealthCheckRegistration
{
    public const string Name = "postgresql";
}
