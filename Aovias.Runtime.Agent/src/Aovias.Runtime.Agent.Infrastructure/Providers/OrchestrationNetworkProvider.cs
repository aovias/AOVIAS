using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts.Providers;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Providers;

/// <summary>
/// Network reachability provider.
/// </summary>
public sealed class OrchestrationNetworkProvider : INetworkProvider
{
    private readonly IOptionsMonitor<ProviderSettings> providerSettings;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;

    public OrchestrationNetworkProvider(
        IOptionsMonitor<ProviderSettings> providerSettings,
        IOptionsMonitor<ApiSettings> apiSettings)
    {
        this.providerSettings = providerSettings;
        this.apiSettings = apiSettings;
    }

    /// <inheritdoc />
    public string ProviderName => "Network";

    /// <inheritdoc />
    public bool IsAvailable => providerSettings.CurrentValue.NetworkEnabled;

    /// <inheritdoc />
    public async Task<bool> IsReachableAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var target = string.IsNullOrWhiteSpace(endpoint) ? apiSettings.CurrentValue.BaseUrl : endpoint;
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            using var response = await client.GetAsync(target, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
