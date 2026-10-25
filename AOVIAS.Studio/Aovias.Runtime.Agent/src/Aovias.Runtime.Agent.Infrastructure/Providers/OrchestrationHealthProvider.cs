using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts.Providers;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Providers;

/// <summary>
/// Health provider adapter for orchestration workflows.
/// </summary>
public sealed class OrchestrationHealthProvider : IHealthProvider
{
    private readonly Application.Interfaces.IHealthProvider healthProvider;
    private readonly IOptionsMonitor<ProviderSettings> providerSettings;
    private readonly IOptionsMonitor<DeploymentSettings> deploymentSettings;
    private readonly IHttpClientFactory httpClientFactory;

    public OrchestrationHealthProvider(
        Application.Interfaces.IHealthProvider healthProvider,
        IOptionsMonitor<ProviderSettings> providerSettings,
        IOptionsMonitor<DeploymentSettings> deploymentSettings,
        IHttpClientFactory httpClientFactory)
    {
        this.healthProvider = healthProvider;
        this.providerSettings = providerSettings;
        this.deploymentSettings = deploymentSettings;
        this.httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public string ProviderName => "Health";

    /// <inheritdoc />
    public bool IsAvailable => providerSettings.CurrentValue.HealthEnabled;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var url = ResolveEndpoint(endpoint);
        if (url is null)
        {
            var health = await healthProvider.GetHealthAsync(cancellationToken);
            return new HealthCheckResult
            {
                IsHealthy = string.Equals(health.Status, "Healthy", StringComparison.OrdinalIgnoreCase),
                Message = health.Message,
                StatusCode = 200
            };
        }

        return await ProbeAsync(url, 200, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> WaitForHealthAsync(
        string endpoint,
        int expectedStatusCode,
        TimeSpan timeout,
        int retryCount,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        var url = ResolveEndpoint(endpoint);
        if (url is null)
        {
            return await CheckEndpointAsync(endpoint, cancellationToken);
        }

        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        HealthCheckResult? lastResult = null;
        var attempts = Math.Max(1, retryCount + 1);

        for (var attempt = 0; attempt < attempts && DateTimeOffset.UtcNow < deadline; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastResult = await ProbeAsync(url, expectedStatusCode, cancellationToken);
            if (lastResult.IsHealthy)
            {
                return lastResult;
            }

            if (attempt < attempts - 1)
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        return lastResult ?? new HealthCheckResult
        {
            IsHealthy = false,
            Message = "Health verification did not complete.",
            StatusCode = 0
        };
    }

    private string? ResolveEndpoint(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            return endpoint;
        }

        var baseUrl = deploymentSettings.CurrentValue.HealthCheckBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
    }

    private async Task<HealthCheckResult> ProbeAsync(string url, int expectedStatusCode, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(nameof(OrchestrationHealthProvider));
        using var response = await client.GetAsync(url, cancellationToken);
        var isHealthy = (int)response.StatusCode == expectedStatusCode;
        return new HealthCheckResult
        {
            IsHealthy = isHealthy,
            Message = isHealthy
                ? $"Health endpoint '{url}' returned {response.StatusCode}."
                : $"Health endpoint '{url}' returned {response.StatusCode}; expected {expectedStatusCode}.",
            StatusCode = (int)response.StatusCode
        };
    }
}
