using System.Collections.Concurrent;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.CommandCenter.Infrastructure;

/// <summary>
/// A fleet-wide search/filter request (Sprint M2.7 — "Fleet-wide search/filter" +
/// "Environment labels/tags"). Every field is optional and additive (AND-combined) — an empty
/// query returns every registered environment.
/// </summary>
public sealed class FleetSearchQuery
{
    /// <summary>Case-insensitive substring match against <see cref="RemoteEnvironment.Name"/>/<see cref="RemoteEnvironment.Description"/>.</summary>
    public string? Text { get; init; }

    public EnvironmentType? EnvironmentType { get; init; }

    /// <summary>When set, restricts to environments whose <see cref="RemoteEnvironment.Labels"/> contain every key/value pair given here.</summary>
    public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool? OnlineOnly { get; init; }

    public bool? MaintenanceOnly { get; init; }
}

/// <summary>
/// The Operations Command Center's Fleet Registry (Sprint M2.7 D-item "Fleet Registry") — the
/// authoritative list of every environment the Command Center monitors. Built directly on
/// <see cref="RemoteEnvironment"/> rather than a new parallel type (see that class's own remarks):
/// this is the same Fleet Registry entry type it was already shaped for since Sprint M2.0.
///
/// This registry is the Command Center's own cache of fleet membership and last-known state — the
/// Runtime Agent each entry describes remains the sole source of truth for its own health,
/// reconciliation, and maintenance-mode state (Final Engineering Rule: "Runtime Agent remains
/// autonomous"). <see cref="RuntimeAgentPollingService"/> is the only writer that updates an
/// entry's cached fields after registration.
/// </summary>
public interface IFleetRegistry
{
    RemoteEnvironment Register(FleetRegistrationRequest request);

    RemoteEnvironment? Get(string environmentId);

    IReadOnlyList<RemoteEnvironment> List();

    IReadOnlyList<RemoteEnvironment> Search(FleetSearchQuery query);

    /// <summary>Applies <paramref name="mutate"/> to the current entry (if it exists) via a <c>with</c> expression and stores the result — the update path <see cref="RuntimeAgentPollingService"/> and the maintenance-mode API endpoints use.</summary>
    RemoteEnvironment? Update(string environmentId, Func<RemoteEnvironment, RemoteEnvironment> mutate);

    bool Remove(string environmentId);
}

/// <summary>In-memory <see cref="IFleetRegistry"/> — deliberately non-durable, matching every other Runtime Agent/Command Center <c>*Store</c>'s established convention (Sprint M2.6 Technical Debt precedent) for a first cut; re-registering environments after a Command Center restart is the accepted gap (Sprint M2.7 Technical Debt).</summary>
public sealed class FleetRegistry : IFleetRegistry
{
    private readonly ConcurrentDictionary<string, RemoteEnvironment> environments = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers an environment, or — Sprint M4.3 — re-registers it in place if an entry with the
    /// same <see cref="RemoteEnvironment.BaseUrl"/> already exists. This is the server-side half of
    /// "registration must be idempotent / duplicate registrations must be prevented": a Runtime
    /// Agent that registers automatically on every restart (<c>EnvironmentRegistrationHostedService</c>)
    /// must not accumulate a new Fleet Registry entry — and therefore a new identity — every time it
    /// restarts while Command Center itself stays up. Matching by <see cref="RemoteEnvironment.BaseUrl"/>
    /// rather than any caller-supplied id keeps this correct even for the very first registration a
    /// process makes, before it has ever been told an id. This does not attempt to solve Command
    /// Center's own non-durability (Sprint M2.7 Technical Debt, still accepted): if Command Center
    /// itself restarts and loses its in-memory registry, the next registration genuinely is
    /// first-time from its perspective and receives a new id — callers must not assume an id is
    /// permanent across a Command Center restart.
    /// </summary>
    public RemoteEnvironment Register(FleetRegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedBaseUrl = request.BaseUrl.TrimEnd('/');
        var existing = environments.Values.FirstOrDefault(environment =>
            string.Equals(environment.BaseUrl, normalizedBaseUrl, StringComparison.OrdinalIgnoreCase));

        var environmentId = existing?.Id ?? Guid.NewGuid().ToString("N");
        var environment = new RemoteEnvironment
        {
            Id = environmentId,
            Name = request.Name,
            Description = request.Description,
            BaseUrl = normalizedBaseUrl,
            RuntimeAgentEndpoint = normalizedBaseUrl,
            AuthenticationMode = EnvironmentAuthenticationMode.ApiKey,
            ApiKey = request.ApiKey,
            EnvironmentType = request.EnvironmentType,
            IsRegistered = true,
            RegisteredUtc = DateTimeOffset.UtcNow,
            Labels = request.Labels,
            Capabilities = request.Capabilities,
            // Re-registration must not erase state only the heartbeat loop populates.
            RuntimeVersion = existing?.RuntimeVersion ?? string.Empty,
            PlatformVersion = existing?.PlatformVersion ?? string.Empty,
            HealthStatus = existing?.HealthStatus ?? "Unknown",
            LastHeartbeatUtc = existing?.LastHeartbeatUtc,
            MaintenanceMode = existing?.MaintenanceMode ?? false,
            MaintenanceReason = existing?.MaintenanceReason,
            // Sprint M4.4: same rationale as RuntimeVersion/HealthStatus above — a re-registration
            // (e.g. after a Runtime Agent restart) must not reset a previously observed Not Ready
            // state back to the optimistic "Ready, nothing deployed yet" default before the next
            // heartbeat has a chance to report the real, current state.
            DeploymentReady = existing?.DeploymentReady ?? true,
            AdmissionBlockerCount = existing?.AdmissionBlockerCount ?? 0
        };

        environments[environmentId] = environment;
        return environment;
    }

    public RemoteEnvironment? Get(string environmentId)
        => environments.TryGetValue(environmentId, out var environment) ? environment : null;

    public IReadOnlyList<RemoteEnvironment> List() => environments.Values.ToList();

    public IReadOnlyList<RemoteEnvironment> Search(FleetSearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        IEnumerable<RemoteEnvironment> results = environments.Values;

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var text = query.Text;
            results = results.Where(environment =>
                environment.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                environment.Description.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        if (query.EnvironmentType is { } environmentType)
        {
            results = results.Where(environment => environment.EnvironmentType == environmentType);
        }

        if (query.Labels.Count > 0)
        {
            results = results.Where(environment => query.Labels.All(requested =>
                environment.Labels.TryGetValue(requested.Key, out var value) &&
                string.Equals(value, requested.Value, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.OnlineOnly is true)
        {
            results = results.Where(environment => environment.LastHeartbeatUtc is not null);
        }

        if (query.MaintenanceOnly is true)
        {
            results = results.Where(environment => environment.MaintenanceMode);
        }

        return results.ToList();
    }

    public RemoteEnvironment? Update(string environmentId, Func<RemoteEnvironment, RemoteEnvironment> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        // Optimistic retry rather than AddOrUpdate: this registry never creates an entry on
        // update (an unknown environmentId returns null, it is not silently registered), and a
        // throwing add-factory would defeat ConcurrentDictionary's lock-free update path.
        while (environments.TryGetValue(environmentId, out var existing))
        {
            var updated = mutate(existing);
            if (environments.TryUpdate(environmentId, updated, existing))
            {
                return updated;
            }
        }

        return null;
    }

    public bool Remove(string environmentId) => environments.TryRemove(environmentId, out _);
}
