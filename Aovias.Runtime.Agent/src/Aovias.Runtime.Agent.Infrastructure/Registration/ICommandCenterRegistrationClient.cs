using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Runtime.Agent.Infrastructure.Registration;

/// <summary>
/// Sprint M4.3 — Guideline #9 provider seam for the one new external touchpoint this sprint adds:
/// the Runtime Agent calling out to the Operations Command Center. Deliberately a new, narrow
/// interface rather than a dependency on <c>Aovias.Release.SDK</c>'s <c>IDeploymentClient</c> — that
/// client is shaped around a CLI operator's single already-configured "current environment" and
/// exposes the full deployment surface; this agent needs exactly two calls against one specific,
/// externally-addressed Command Center. Reuses the identical wire contracts
/// (<see cref="FleetRegistrationRequest"/>/<see cref="RemoteEnvironment"/>) the existing
/// <c>POST /api/environments/register</c> and <c>GET /api/environments/{id}</c> endpoints already
/// define — no new contract, no new endpoint.
/// </summary>
public interface ICommandCenterRegistrationClient
{
    Task<RemoteEnvironment> RegisterAsync(string commandCenterBaseUrl, FleetRegistrationRequest request, CancellationToken cancellationToken);

    /// <summary>Returns <see langword="null"/> if Command Center does not know this environment id (a 404) — the signal that tells the caller to re-register rather than assume it is still known.</summary>
    Task<RemoteEnvironment?> TryGetAsync(string commandCenterBaseUrl, string environmentId, CancellationToken cancellationToken);
}
