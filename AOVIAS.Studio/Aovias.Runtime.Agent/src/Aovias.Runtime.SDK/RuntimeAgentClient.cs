using System.Net.Http.Json;
using Aovias.Configuration.Settings;
using Aovias.Runtime.Contracts.Deployment;
using Aovias.Runtime.Contracts.Health;
using Aovias.Runtime.Contracts.Monitoring;
using Aovias.Runtime.Contracts.Observability;
using Aovias.Runtime.Contracts.Runtime;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.SDK;

/// <summary>
/// HTTP client for the AOVIAS Runtime Agent control plane API.
/// </summary>
public sealed class RuntimeAgentClient : IRuntimeAgentClient
{
    private readonly HttpClient httpClient;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeAgentClient"/> class.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client.</param>
    /// <param name="apiSettings">API settings monitor.</param>
    public RuntimeAgentClient(HttpClient httpClient, IOptionsMonitor<ApiSettings> apiSettings)
    {
        this.httpClient = httpClient;
        this.apiSettings = apiSettings;
    }

    /// <inheritdoc />
    public Task<RuntimeManifest> GetRuntimeAsync(CancellationToken cancellationToken = default)
        => GetAsync<RuntimeManifest>("/api/runtime", cancellationToken);

    /// <inheritdoc />
    public Task<RuntimeHealthDto> GetRuntimeHealthAsync(CancellationToken cancellationToken = default)
        => GetAsync<RuntimeHealthDto>("/api/runtime/health", cancellationToken);

    /// <inheritdoc />
    public Task<RuntimeInfoDto> GetRuntimeInfoAsync(CancellationToken cancellationToken = default)
        => GetAsync<RuntimeInfoDto>("/api/runtime/info", cancellationToken);

    /// <inheritdoc />
    public Task<RuntimeVersionDto> GetRuntimeVersionAsync(CancellationToken cancellationToken = default)
        => GetAsync<RuntimeVersionDto>("/api/runtime/version", cancellationToken);

    /// <inheritdoc />
    public Task<RuntimeEnvironmentDto> GetRuntimeEnvironmentAsync(CancellationToken cancellationToken = default)
        => GetAsync<RuntimeEnvironmentDto>("/api/runtime/environment", cancellationToken);

    /// <inheritdoc />
    public Task<RuntimeStatisticsDto> GetRuntimeStatisticsAsync(CancellationToken cancellationToken = default)
        => GetAsync<RuntimeStatisticsDto>("/api/runtime/statistics", cancellationToken);

    /// <inheritdoc />
    public Task<RuntimeCapabilitiesDto> GetRuntimeCapabilitiesAsync(CancellationToken cancellationToken = default)
        => GetAsync<RuntimeCapabilitiesDto>("/api/runtime/capabilities", cancellationToken);

    /// <inheritdoc />
    public Task<RuntimeStateDto> GetRuntimeStateAsync(CancellationToken cancellationToken = default)
        => GetAsync<RuntimeStateDto>("/api/runtime/state", cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeTimelineEntryDto>> GetRuntimeTimelineAsync(
        string? search = null,
        string? category = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = $"/api/runtime/timeline?limit={limit}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search)}";
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query += $"&category={Uri.EscapeDataString(category)}";
        }

        return await GetAsync<IReadOnlyList<RuntimeTimelineEntryDto>>(query, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RuntimeMetricsSnapshotDto>> GetRuntimeMetricsAsync(int limit = 60, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<RuntimeMetricsSnapshotDto>>($"/api/runtime/metrics?limit={limit}", cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ContainerObservationDto>> GetRuntimeContainersAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<ContainerObservationDto>>("/api/runtime/containers", cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<NetworkObservationDto>> GetRuntimeNetworksAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<NetworkObservationDto>>("/api/runtime/networks", cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ReleaseObservationDto>> GetRuntimeReleasesAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<ReleaseObservationDto>>("/api/runtime/releases", cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowObservationDto>> GetRuntimeWorkflowsAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<WorkflowObservationDto>>("/api/runtime/workflows", cancellationToken);

    /// <inheritdoc />
    public Task<RuntimeDeploymentAvailabilityDto> GetDeploymentAvailabilityAsync(CancellationToken cancellationToken = default)
        => GetAsync<RuntimeDeploymentAvailabilityDto>("/api/deployment/status", cancellationToken);

    private async Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        var settings = apiSettings.CurrentValue;
        var normalizedUri = NormalizeRequestUri(requestUri, settings.PathBase);
        var attempts = Math.Max(1, settings.RetryCount + 1);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var response = await httpClient.GetAsync(normalizedUri, cancellationToken);
                response.EnsureSuccessStatusCode();
                var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
                return payload ?? throw new InvalidOperationException($"Empty response received from {normalizedUri}.");
            }
            catch (Exception exception) when (attempt < attempts)
            {
                lastException = exception;
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException($"Failed to call {normalizedUri}.");
    }

    private static string NormalizeRequestUri(string requestUri, string pathBase)
    {
        if (string.IsNullOrWhiteSpace(pathBase) || requestUri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return requestUri;
        }

        var prefix = pathBase.TrimEnd('/');
        return requestUri.StartsWith('/')
            ? $"{prefix}{requestUri}"
            : $"{prefix}/{requestUri}";
    }
}
