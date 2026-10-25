using System.Net.Http.Json;
using Aovias.Bootstrap.Contracts;
using Aovias.Release.Contracts.Deployment;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Bootstrap.Execution;

/// <summary>
/// The real, HTTP-backed <see cref="IBootstrapRegistrationClient"/>. Calls the two existing
/// registration endpoints directly with a plain <see cref="HttpClient"/> rather than taking a
/// dependency on <c>Aovias.Release.SDK</c>'s <c>DeploymentClient</c> — that client is shaped around
/// a CLI operator's already-configured <c>DeploymentSettings</c> (a single, ambient "current
/// environment"), whereas Bootstrap addresses a specific, just-created target per call. Both reuse
/// the identical wire contracts, so this is not parallel registration logic — only a different,
/// equally thin caller of the same endpoints.
///
/// Moved from <c>Aovias.Bootstrap.Host</c> into this shared execution project so both the CLI and
/// AOVIAS Studio construct and use the exact same class — there is no second registration client.
/// </summary>
public sealed class HttpBootstrapRegistrationClient(HttpClient httpClient) : IBootstrapRegistrationClient
{
    public async Task<AgentRegistrationResponse> RegisterAgentAsync(string agentBaseUrl, AgentRegistrationRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync($"{agentBaseUrl.TrimEnd('/')}/api/deployment/register", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildFailureMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<AgentRegistrationResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Agent registration returned an empty response.");
    }

    public async Task<RemoteEnvironment> RegisterWithFleetAsync(string commandCenterBaseUrl, FleetRegistrationRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync($"{commandCenterBaseUrl.TrimEnd('/')}/api/environments/register", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildFailureMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<RemoteEnvironment>(cancellationToken)
            ?? throw new InvalidOperationException("Fleet registration returned an empty response.");
    }

    public async Task<RuntimeHeartbeatDto> GetHeartbeatAsync(string agentBaseUrl, string? apiKey, CancellationToken cancellationToken)
    {
        // Per-request header (not a default header on the shared HttpClient) — the key belongs to
        // one agent and one bootstrap run, never to every request this client instance ever makes.
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{agentBaseUrl.TrimEnd('/')}/api/deployment/heartbeat");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add("X-Api-Key", apiKey);
        }

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildFailureMessageAsync(response, cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<RuntimeHeartbeatDto>(cancellationToken)
            ?? throw new InvalidOperationException("Heartbeat returned an empty response.");
    }

    /// <summary>
    /// Real-world observed gap: <c>EnsureSuccessStatusCode()</c> throws on a failed response without
    /// ever reading its body, discarding the structured <see cref="DeploymentFailure"/> JSON the
    /// Runtime Agent's own <c>ExceptionHandlingMiddleware</c> writes on every failure (message,
    /// category, recovery action) — leaving only "status code does not indicate success", exactly the
    /// bare failure this defect fix's diagnostics work elsewhere was meant to eliminate. Reading the
    /// body first, and surfacing its <see cref="DeploymentFailure.Message"/> (falling back to the raw
    /// body, then the status code alone) when it does not parse, means a registration failure card
    /// always shows the Runtime Agent's own stated reason instead of a generic HTTP status.
    /// </summary>
    private static async Task<string> BuildFailureMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return $"Request to '{response.RequestMessage?.RequestUri}' failed with status {(int)response.StatusCode} ({response.StatusCode}).";
        }

        DeploymentFailure? failure = null;
        try
        {
            failure = System.Text.Json.JsonSerializer.Deserialize<DeploymentFailure>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (System.Text.Json.JsonException)
        {
            // body is not a DeploymentFailure — fall through and report it verbatim below.
        }

        var reason = failure is not null ? $"{failure.Category}: {failure.Message}" : body;
        return $"Request to '{response.RequestMessage?.RequestUri}' failed with status {(int)response.StatusCode} ({response.StatusCode}). {reason}";
    }

    /// <summary>Mirrors the negotiate-URL convention <c>Aovias.Configuration.SignalR.SignalRHubEndpointResolver</c>
    /// already establishes for the same canonical hub route (<c>/hubs/deployment</c>) — this project
    /// does not take a dependency on that assembly purely to reuse a string-building helper, but the
    /// literal path and query string below are that same convention, not an independently invented one.</summary>
    public async Task<bool> VerifySignalRAsync(string agentBaseUrl, CancellationToken cancellationToken)
    {
        try
        {
            var negotiateUrl = $"{agentBaseUrl.TrimEnd('/')}/hubs/deployment/negotiate?negotiateVersion=1";
            var response = await httpClient.PostAsync(negotiateUrl, content: null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
