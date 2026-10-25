using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Runtime.Agent.Infrastructure.Reconciliation;

/// <summary>
/// Tracks whether this Runtime Agent's environment is currently in maintenance mode (Sprint
/// M2.7). In-memory only, mirroring the same lightweight, non-durable convention every other
/// small cross-cutting Runtime Agent store already uses (e.g. Sprint M2.6's
/// <c>InMemoryOperatorApprovalStore</c>). Consulted by <c>RuntimeReconciliationTargetProvider</c>
/// when building each repair task's <c>SafetyContext</c> (Sprint M2.6 D11) — this is the one and
/// only place maintenance mode changes what the Runtime Agent's own, still fully autonomous,
/// reconciliation loop does: it never stops Desired/Observed State comparison or drift detection,
/// it only suspends automatic repair (Final Engineering Rule, Sprint M2.6: "Unsafe repairs always
/// require explicit operator approval" — maintenance mode is exactly such a condition).
///
/// The Command Center (Sprint M2.7) is the intended caller of the endpoints backed by this store,
/// but it is not the only possible one — this store has no dependency on the Command Center at
/// all, matching Final Engineering Rule "Runtime Agent remains autonomous" and "Command Center
/// communicates only through Runtime APIs" (never reaching into Runtime Agent's internals).
/// </summary>
public sealed class MaintenanceModeStore
{
    private readonly object gate = new();
    private bool active;
    private string? reason;
    private DateTimeOffset? changedUtc;
    private string? changedBy;

    public MaintenanceModeStatus GetStatus()
    {
        lock (gate)
        {
            return new MaintenanceModeStatus { Active = active, Reason = reason, ChangedUtc = changedUtc, ChangedBy = changedBy };
        }
    }

    public MaintenanceModeStatus Enable(MaintenanceModeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (gate)
        {
            active = true;
            reason = request.Reason;
            changedUtc = DateTimeOffset.UtcNow;
            changedBy = request.RequestedBy;
            return GetStatusUnsafe();
        }
    }

    public MaintenanceModeStatus Resume(string resumedBy)
    {
        lock (gate)
        {
            active = false;
            reason = null;
            changedUtc = DateTimeOffset.UtcNow;
            changedBy = resumedBy;
            return GetStatusUnsafe();
        }
    }

    private MaintenanceModeStatus GetStatusUnsafe()
        => new() { Active = active, Reason = reason, ChangedUtc = changedUtc, ChangedBy = changedBy };
}
