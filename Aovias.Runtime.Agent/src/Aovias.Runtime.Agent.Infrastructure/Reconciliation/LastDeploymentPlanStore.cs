using System.Collections.Concurrent;
using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Runtime.Agent.Infrastructure.Reconciliation;

/// <summary>The last successfully executed <see cref="DeploymentPlan"/> for one service, plus everything a reconciliation cycle needs to rebuild Desired State from it (Sprint M2.6 D1/D8) — the concrete source <c>RuntimeReconciliationTargetProvider</c> reads from.</summary>
public sealed record LastDeploymentRecord(DeploymentPlan Plan, InfrastructureContract Contract, ServiceManifest Manifest, string? ResolvedNetworkName);

/// <summary>
/// An in-memory registry of the last successfully executed deployment per service (Sprint M2.6
/// D1/D8), following the same <c>ConcurrentDictionary</c>-backed, non-durable convention every
/// other Runtime Agent <c>*Store</c> (<c>DeploymentQueueStore</c>, <c>EnvironmentRegistrationStore</c>,
/// etc.) already uses. Populated by <c>ReleaseDeploymentProvider</c> immediately after a
/// successful <c>DeploymentExecutionRuntime.ExecuteAsync</c> call (Sprint M2.5) — Desired State
/// (D1) is built exclusively from what is stored here, never from anything observed.
/// </summary>
public sealed class LastDeploymentPlanStore
{
    private readonly ConcurrentDictionary<string, LastDeploymentRecord> records = new(StringComparer.OrdinalIgnoreCase);

    public void Record(string serviceId, LastDeploymentRecord record) => records[serviceId] = record;

    public LastDeploymentRecord? TryGet(string serviceId) => records.TryGetValue(serviceId, out var record) ? record : null;

    public IReadOnlyList<string> ServiceIds => records.Keys.ToList();
}
