using Aovias.Bootstrap.Contracts;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Bootstrap.Core.Tests;

public sealed class FakeBootstrapRegistrationClient : IBootstrapRegistrationClient
{
    public bool AgentRegistrationFails { get; set; }

    /// <summary>How many of the first RegisterAgentAsync calls throw (simulating the container not listening yet) before succeeding — 0 (default) means <see cref="AgentRegistrationFails"/> alone decides pass/fail on every call, matching the pre-existing behavior.</summary>
    public int AgentRegistrationFailuresBeforeSuccess { get; set; }

    /// <summary>Simulates the real-world token-rejection response: the agent's own <c>EnvironmentRegistrationStore</c> throws <c>UnauthorizedAccessException("Registration token is invalid.")</c>, which its exception middleware maps to an HTTP 500 whose structured body the HTTP client now surfaces — the message shape below mirrors <c>HttpBootstrapRegistrationClient.BuildFailureMessageAsync</c>'s output for exactly that case.</summary>
    public bool RejectsRegistrationToken { get; set; }

    public int AgentRegistrationCallCount { get; private set; }

    public bool FleetRegistrationFails { get; set; }

    public string HeartbeatHealthStatus { get; set; } = "Healthy";

    /// <summary>How many of the first heartbeat calls report <see cref="HeartbeatHealthStatus"/> before flipping to "Healthy" — 0 means every call reports <see cref="HeartbeatHealthStatus"/> (the pre-existing default behavior). Lets tests exercise the health-verification polling loop without a real host that takes time to warm up.</summary>
    public int UnhealthyAttemptsBeforeRecovery { get; set; }

    public string HeartbeatRuntimeVersion { get; set; } = "1.0.0";

    public bool SignalRHealthy { get; set; } = true;

    public AgentRegistrationRequest? LastAgentRegistrationRequest { get; private set; }

    public FleetRegistrationRequest? LastFleetRegistrationRequest { get; private set; }

    public int HeartbeatCallCount { get; private set; }

    public Task<AgentRegistrationResponse> RegisterAgentAsync(string agentBaseUrl, AgentRegistrationRequest request, CancellationToken cancellationToken)
    {
        LastAgentRegistrationRequest = request;
        AgentRegistrationCallCount++;
        if (RejectsRegistrationToken)
        {
            throw new InvalidOperationException(
                $"Request to '{agentBaseUrl}/api/deployment/register' failed with status 500 (InternalServerError). Authentication: Registration token is invalid.");
        }

        if (AgentRegistrationFails || AgentRegistrationCallCount <= AgentRegistrationFailuresBeforeSuccess)
        {
            throw new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.TimedOut);
        }

        return Task.FromResult(new AgentRegistrationResponse
        {
            EnvironmentId = "env-123",
            ApiKey = "fake-api-key",
            RuntimeAgentEndpoint = agentBaseUrl,
            RegisteredUtc = DateTimeOffset.UtcNow
        });
    }

    public Task<RemoteEnvironment> RegisterWithFleetAsync(string commandCenterBaseUrl, FleetRegistrationRequest request, CancellationToken cancellationToken)
    {
        LastFleetRegistrationRequest = request;
        if (FleetRegistrationFails)
        {
            throw new InvalidOperationException("Simulated Fleet Registry registration failure.");
        }

        return Task.FromResult(new RemoteEnvironment
        {
            Id = "env-123",
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            RuntimeAgentEndpoint = request.BaseUrl,
            EnvironmentType = request.EnvironmentType,
            ApiKey = request.ApiKey,
            IsRegistered = true
        });
    }

    /// <summary>The API key the most recent heartbeat call presented — lets tests prove the key issued by self-registration actually flows to verification.</summary>
    public string? LastHeartbeatApiKey { get; private set; }

    /// <summary>Models the real agent's <c>ApiKeyAuthenticationMiddleware</c>: heartbeat rejects any call not presenting the key this fake's own RegisterAgentAsync issued. On by default so every test exercises the authenticated path the real VPS enforces (the 401 defect this reproduces was found in production, not a unit test).</summary>
    public bool EnforceHeartbeatAuthentication { get; set; } = true;

    public Task<RuntimeHeartbeatDto> GetHeartbeatAsync(string agentBaseUrl, string? apiKey, CancellationToken cancellationToken)
    {
        LastHeartbeatApiKey = apiKey;
        if (EnforceHeartbeatAuthentication && !string.Equals(apiKey, "fake-api-key", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Request to '{agentBaseUrl}/api/deployment/heartbeat' failed with status 401 (Unauthorized). API key is required.");
        }

        HeartbeatCallCount++;
        var isRecoveryScenario = UnhealthyAttemptsBeforeRecovery > 0;
        var healthStatus = isRecoveryScenario && HeartbeatCallCount > UnhealthyAttemptsBeforeRecovery
            ? "Healthy"
            : HeartbeatHealthStatus;

        return Task.FromResult(new RuntimeHeartbeatDto
        {
            EnvironmentId = "env-123",
            RuntimeVersion = HeartbeatRuntimeVersion,
            HealthStatus = healthStatus
        });
    }

    public Task<bool> VerifySignalRAsync(string agentBaseUrl, CancellationToken cancellationToken)
        => Task.FromResult(SignalRHealthy);
}
