using System.Net;
using System.Net.Http.Json;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Runtime.Agent.Infrastructure.Registration;

/// <summary>The real, HTTP-backed <see cref="ICommandCenterRegistrationClient"/>.</summary>
public sealed class CommandCenterRegistrationClient(HttpClient httpClient) : ICommandCenterRegistrationClient
{
    public async Task<RemoteEnvironment> RegisterAsync(string commandCenterBaseUrl, FleetRegistrationRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync($"{commandCenterBaseUrl.TrimEnd('/')}/api/environments/register", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RemoteEnvironment>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Command Center registration returned an empty response.");
    }

    public async Task<RemoteEnvironment?> TryGetAsync(string commandCenterBaseUrl, string environmentId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"{commandCenterBaseUrl.TrimEnd('/')}/api/environments/{environmentId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var details = await response.Content.ReadFromJsonAsync<EnvironmentDetails>(cancellationToken: cancellationToken);
        return details?.Environment;
    }
}
