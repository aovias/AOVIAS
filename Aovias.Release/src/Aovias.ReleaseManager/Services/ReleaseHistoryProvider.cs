using Aovias.Release.Contracts.Audit;
using Aovias.Release.Contracts.Releases;

namespace Aovias.ReleaseManager.Services;

/// <summary>
/// Provides release history from persisted audit records and release repository data.
/// </summary>
public sealed class ReleaseHistoryProvider : Abstractions.IReleaseHistoryProvider
{
    private readonly Abstractions.IReleaseRepository releaseRepository;
    private readonly FileSystemReleaseAuditStore auditStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseHistoryProvider"/> class.
    /// </summary>
    public ReleaseHistoryProvider(
        Abstractions.IReleaseRepository releaseRepository,
        FileSystemReleaseAuditStore auditStore)
    {
        this.releaseRepository = releaseRepository;
        this.auditStore = auditStore;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseHistoryEntry>> GetHistoryAsync(
        ReleaseHistorySearchCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        var auditHistory = await auditStore.SearchHistoryAsync(criteria, cancellationToken);
        if (auditHistory.Count > 0)
        {
            return auditHistory;
        }

        var records = await releaseRepository.ListAsync(cancellationToken);
        return records
            .Select(record => new ReleaseHistoryEntry
            {
                ReleaseId = record.Identity.ReleaseId,
                PackageId = record.Identity.PackageId,
                PackageVersion = record.Identity.PackageVersion,
                Runtime = record.Identity.Runtime,
                Environment = record.Identity.Environment,
                State = record.State,
                Timestamp = record.Identity.Timestamp,
                Summary = record.LastPlan?.PlanId,
                DeploymentId = record.Identity.ReleaseId,
                Operator = "system",
                StartedAt = record.Identity.Timestamp,
                CompletedAt = record.UpdatedAt,
                Duration = record.UpdatedAt - record.Identity.Timestamp,
                WasRollback = record.State == Aovias.Release.Contracts.Enums.ReleaseState.RolledBack,
                CorrelationId = null
            })
            .OrderByDescending(entry => entry.Timestamp)
            .ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AuditEvent>> GetDeploymentDetailAsync(
        string deploymentId,
        CancellationToken cancellationToken = default)
        => auditStore.GetDeploymentEventsAsync(deploymentId, cancellationToken);
}
