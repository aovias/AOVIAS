using System.Net.Http;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public sealed class HealthCheckService : IDisposable
{
    private readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    private readonly ILoggingService logger;

    public HealthCheckService(ILoggingService logger)
    {
        this.logger = logger;
    }

    public async Task<HealthStatus> CheckAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        var baseUrl = ResolveBaseUrl(service);
        if (baseUrl is null)
        {
            return service.Status == ServiceStatus.Running ? HealthStatus.Starting : HealthStatus.Unknown;
        }

        var paths = service.Kind == ServiceKind.AngularWebApp
            ? new[] { "/" }
            : new[] { "/readiness", "/health", "/liveness" };

        foreach (var path in paths)
        {
            try
            {
                using var response = await httpClient.GetAsync(new Uri(baseUrl, path), cancellationToken);
                if ((int)response.StatusCode < 500)
                {
                    return HealthStatus.Healthy;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
            }
        }

        logger.Warning(service.Name, $"Health check failed for {baseUrl}.");
        return HealthStatus.Unreachable;
    }

    public void Dispose() => httpClient.Dispose();

    private static Uri? ResolveBaseUrl(ServiceInfoModel service)
    {
        if (!string.IsNullOrWhiteSpace(service.HttpsUrl) && Uri.TryCreate(service.HttpsUrl, UriKind.Absolute, out var httpsUri))
        {
            return httpsUri;
        }

        if (!string.IsNullOrWhiteSpace(service.LaunchUrl) && Uri.TryCreate(service.LaunchUrl, UriKind.Absolute, out var launchUri))
        {
            return launchUri;
        }

        if (!string.IsNullOrWhiteSpace(service.HttpUrl) && Uri.TryCreate(service.HttpUrl, UriKind.Absolute, out var httpUri))
        {
            return httpUri;
        }

        return service.HttpsPort is not null
            ? new Uri($"https://localhost:{service.HttpsPort}")
            : service.Port is null ? null : new Uri($"http://localhost:{service.Port}");
    }
}
