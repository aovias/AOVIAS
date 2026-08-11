using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Runtime.Agent.Infrastructure.Deployment;

/// <summary>
/// Stores trusted environments registered by Runtime Agents.
/// </summary>
public sealed class EnvironmentRegistrationStore
{
    private readonly ConcurrentDictionary<string, RemoteEnvironment> environments = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> tokens = new(StringComparer.OrdinalIgnoreCase);

    public EnvironmentRegistrationStore()
    {
        SeedDefaultTokens();
    }

    public AgentRegistrationResponse Register(AgentRegistrationRequest request)
    {
        if (!tokens.TryGetValue(request.RegistrationToken, out var environmentName))
        {
            throw new UnauthorizedAccessException("Registration token is invalid.");
        }

        var environmentId = Guid.NewGuid().ToString("N");
        var apiKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{environmentId}:{request.RegistrationToken}:{DateTimeOffset.UtcNow:O}")));

        var environment = new RemoteEnvironment
        {
            Id = environmentId,
            Name = string.IsNullOrWhiteSpace(request.EnvironmentName) ? environmentName : request.EnvironmentName,
            Description = $"Registered from {request.Hostname}",
            BaseUrl = request.PublicUrl,
            RuntimeAgentEndpoint = request.PublicUrl.TrimEnd('/'),
            AuthenticationMode = EnvironmentAuthenticationMode.ApiKey,
            ApiKey = apiKey,
            EnvironmentType = request.EnvironmentType,
            RuntimeVersion = request.RuntimeVersion,
            HealthStatus = "Healthy",
            IsRegistered = true,
            RegisteredUtc = DateTimeOffset.UtcNow
        };

        environments[environmentId] = environment;
        return new AgentRegistrationResponse
        {
            EnvironmentId = environmentId,
            ApiKey = apiKey,
            RuntimeAgentEndpoint = environment.RuntimeAgentEndpoint,
            RegisteredUtc = environment.RegisteredUtc!.Value
        };
    }

    public RemoteEnvironment? Get(string environmentId)
        => environments.TryGetValue(environmentId, out var environment) ? environment : null;

    public bool ValidateApiKey(string apiKey)
        => environments.Values.Any(environment =>
            string.Equals(environment.ApiKey, apiKey, StringComparison.Ordinal));

    private void SeedDefaultTokens()
    {
        // Single source of truth shared with AOVIAS Studio's Bootstrap token pre-fill — see
        // WellKnownRegistrationTokens' remarks for the real-world mismatch that motivated it.
        foreach (var (token, environmentName) in WellKnownRegistrationTokens.All)
        {
            tokens[token] = environmentName;
        }
    }
}
