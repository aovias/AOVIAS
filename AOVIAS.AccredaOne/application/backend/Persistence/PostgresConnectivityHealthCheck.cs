using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AccredaOne.Api.Persistence;

/// <summary>
/// Performs a bounded connectivity probe when live database checks are explicitly enabled.
/// </summary>
public sealed class PostgresConnectivityHealthCheck : IHealthCheck
{
    private readonly AccredaOneDbContext _dbContext;
    private readonly IOptions<PersistenceOptions> _options;

    public PostgresConnectivityHealthCheck(
        AccredaOneDbContext dbContext,
        IOptions<PersistenceOptions> options)
    {
        _dbContext = dbContext;
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = _options.Value;
        if (!settings.HasConnectionString)
        {
            return HealthCheckResult.Healthy("Database is not configured.");
        }

        if (!settings.EnableConnectivity)
        {
            return HealthCheckResult.Healthy("Database connectivity checks are disabled.");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(settings.ConnectionTimeoutSeconds));

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(timeoutSource.Token);
            return canConnect
                ? HealthCheckResult.Healthy("Database connectivity verified.")
                : HealthCheckResult.Unhealthy("Database connectivity check failed.");
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Database connectivity check timed out.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Database connectivity check failed.");
        }
    }
}
