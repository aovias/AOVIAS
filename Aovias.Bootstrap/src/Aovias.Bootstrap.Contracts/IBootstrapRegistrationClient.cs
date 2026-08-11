using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Bootstrap.Contracts;

/// <summary>
/// The provider seam (Guideline #9) for the two existing HTTP registration endpoints the
/// <see cref="BootstrapPhaseName.RuntimeAgentRegistration"/> and
/// <see cref="BootstrapPhaseName.BootstrapVerification"/> phases call. Deliberately thin and
/// reuses the existing wire contracts (<see cref="AgentRegistrationRequest"/>,
/// <see cref="FleetRegistrationRequest"/>, <see cref="RuntimeHeartbeatDto"/>) verbatim — Bootstrap
/// does not define its own registration payloads or a third registration endpoint, it only calls
/// the two that already exist: the target agent's own token-validated self-registration
/// (<c>POST /api/deployment/register</c>), and the Command Center's Fleet Registry
/// (<c>POST /api/environments/register</c>).
/// </summary>
public interface IBootstrapRegistrationClient
{
    /// <summary>Calls the newly-started agent's own <c>POST /api/deployment/register</c>.</summary>
    Task<AgentRegistrationResponse> RegisterAgentAsync(string agentBaseUrl, AgentRegistrationRequest request, CancellationToken cancellationToken);

    /// <summary>Calls the Command Center's <c>POST /api/environments/register</c> so the environment is added to fleet monitoring.</summary>
    Task<RemoteEnvironment> RegisterWithFleetAsync(string commandCenterBaseUrl, FleetRegistrationRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Calls the agent's own <c>GET /api/deployment/heartbeat</c> to confirm it is reachable and
    /// healthy. <paramref name="apiKey"/> is the key issued by the same run's self-registration,
    /// sent as <c>X-Api-Key</c> — the heartbeat endpoint sits behind the agent's own
    /// <c>ApiKeyAuthenticationMiddleware</c> (real-VPS defect: an anonymous heartbeat poll got 401
    /// and verification misread a working, healthy environment as unreachable after 10 attempts).
    /// </summary>
    Task<RuntimeHeartbeatDto> GetHeartbeatAsync(string agentBaseUrl, string? apiKey, CancellationToken cancellationToken);

    /// <summary>
    /// Confirms the agent's real-time (SignalR) channel is reachable by attempting the standard
    /// SignalR HTTP negotiate handshake against its already-mapped hub route — a real reachability
    /// probe (a 2xx response to <c>POST {agentBaseUrl}/hubs/deployment/negotiate</c>), not a
    /// simulated check. Does not open or hold a persistent connection.
    /// </summary>
    Task<bool> VerifySignalRAsync(string agentBaseUrl, CancellationToken cancellationToken);
}
