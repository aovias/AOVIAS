using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Event-driven synchronization of deficiency lifecycle from linked task and evidence progress.
/// </summary>
public interface IDeficiencyTaskLifecycleService
{
    Task SyncFromTaskAsync(ComplianceTask task, ComplianceTaskStatus? previousStatus, CancellationToken cancellationToken = default);

    Task SyncFromEvidenceAsync(Guid taskId, EvidenceLifecycleTrigger trigger, CancellationToken cancellationToken = default);
}

public enum EvidenceLifecycleTrigger
{
    Uploaded,
    Approved,
    Rejected,
    NeedsMoreEvidence
}
