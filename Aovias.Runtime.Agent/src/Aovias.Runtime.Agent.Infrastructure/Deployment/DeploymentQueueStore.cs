using System.Collections.Concurrent;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Runtime.Agent.Infrastructure.Deployment;

/// <summary>
/// Thread-safe store for platform deployment records and queue ordering.
/// </summary>
public sealed class DeploymentQueueStore
{
    private readonly ConcurrentDictionary<string, PlatformDeploymentRecord> records = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> queue = new();
    private readonly object gate = new();
    private string? activeDeploymentId;

    public PlatformDeploymentRecord Enqueue(PlatformDeploymentRecord record)
    {
        records[record.DeploymentId] = record;
        queue.Enqueue(record.DeploymentId);
        return record;
    }

    public bool TryDequeue(out PlatformDeploymentRecord? record)
    {
        record = null;
        lock (gate)
        {
            if (activeDeploymentId is not null)
            {
                return false;
            }

            while (queue.TryDequeue(out var deploymentId))
            {
                if (!records.TryGetValue(deploymentId, out var candidate))
                {
                    continue;
                }

                if (candidate.State != PlatformDeploymentState.Queued)
                {
                    continue;
                }

                activeDeploymentId = deploymentId;
                record = candidate;
                return true;
            }
        }

        return false;
    }

    public void MarkActiveCompleted()
    {
        lock (gate)
        {
            activeDeploymentId = null;
        }
    }

    public PlatformDeploymentRecord? Get(string deploymentId)
        => records.TryGetValue(deploymentId, out var record) ? record : null;

    public IReadOnlyList<PlatformDeploymentRecord> List()
        => records.Values.OrderByDescending(record => record.StartedUtc).ToList();

    public void Upsert(PlatformDeploymentRecord record)
        => records[record.DeploymentId] = record;
}

/// <summary>
/// Mutable deployment record tracked by the queue store.
/// </summary>
public sealed class PlatformDeploymentRecord
{
    public required string DeploymentId { get; init; }

    public PlatformDeploymentState State { get; set; } = PlatformDeploymentState.Queued;

    public required string Operator { get; init; }

    public required string Runtime { get; init; }

    public required string Environment { get; init; }

    public PlatformDeploymentManifest? Manifest { get; init; }

    public IReadOnlyDictionary<string, string> PackagePaths { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public List<ServiceDeploymentStatus> Services { get; init; } = [];

    public PlatformRollbackPlan? RollbackPlan { get; set; }

    public InfrastructureReadinessSnapshot Infrastructure { get; set; } = new();

    public PlatformVersionSnapshot Version { get; set; } = new();

    public List<string> Logs { get; } = [];

    public List<string> AuditTrail { get; } = [];

    public List<PlatformDeploymentHistoryEntry> HistoryProjection { get; } = [];

    public PlatformCertificationReport? Certification { get; set; }

    public PlatformDeploymentRequest? ReplayRequest { get; set; }

    /// <summary>
    /// Effective service ids for this deployment. Null means Platform mode (entire manifest).
    /// Execution MUST honor this list — never re-expand to the full manifest.
    /// </summary>
    public IReadOnlyList<string>? SelectedServiceIds { get; set; }

    public PlatformDeploymentMode DeploymentMode { get; set; } = PlatformDeploymentMode.Platform;

    public DeploymentFailureCategory? FailureCategory { get; set; }

    public string? FailureMessage { get; set; }

    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedUtc { get; set; }

    public bool CancelRequested { get; set; }

    public string? SinglePackagePath { get; init; }

    public bool OperatorConfirmed { get; init; } = true;

    public int ProgressPercent =>
        Services.Count == 0
            ? State switch
            {
                PlatformDeploymentState.Completed => 100,
                PlatformDeploymentState.Failed or PlatformDeploymentState.Cancelled => 0,
                _ => 5
            }
            : (int)Math.Round(Services.Count(status =>
                status.Status is "Completed" or "PASS") * 100d / Services.Count);

    public DeploymentStatusResponse ToStatusResponse()
    {
        return new DeploymentStatusResponse
        {
            DeploymentId = DeploymentId,
            State = State,
            ProgressPercent = ProgressPercent,
            CurrentPhase = State.ToString(),
            FailureMessage = FailureMessage,
            FailureCategory = FailureCategory,
            Services = Services.ToList(),
            Infrastructure = Infrastructure,
            Version = Version,
            StartedUtc = StartedUtc,
            CompletedUtc = CompletedUtc
        };
    }
}
