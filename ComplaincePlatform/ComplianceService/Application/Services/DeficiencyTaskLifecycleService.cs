using ComplianceService.Application.Contracts;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Propagates linked task and evidence progress into deficiency status transitions.
/// </summary>
public sealed class DeficiencyTaskLifecycleService : IDeficiencyTaskLifecycleService
{
    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyRepository _deficiencyRepository;
    private readonly IDeficiencyTaskOwnershipSyncService _ownershipSync;
    private readonly IDeficiencyAuditTrailService _auditTrail;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeficiencyTaskLifecycleService> _logger;

    public DeficiencyTaskLifecycleService(
        ComplianceDbContext db,
        IDeficiencyRepository deficiencyRepository,
        IDeficiencyTaskOwnershipSyncService ownershipSync,
        IDeficiencyAuditTrailService auditTrail,
        IUnitOfWork unitOfWork,
        ILogger<DeficiencyTaskLifecycleService> logger)
    {
        _db = db;
        _deficiencyRepository = deficiencyRepository;
        _ownershipSync = ownershipSync;
        _auditTrail = auditTrail;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SyncFromTaskAsync(
        ComplianceTask task,
        ComplianceTaskStatus? previousStatus,
        CancellationToken cancellationToken = default)
    {
        if (task.SourceDeficiencyId is not { } deficiencyId || deficiencyId == Guid.Empty)
        {
            return;
        }

        var deficiency = await _deficiencyRepository.GetByIdAsync(deficiencyId, cancellationToken);
        if (deficiency == null || deficiency.Status is DeficiencyStatus.Closed or DeficiencyStatus.Cancelled)
        {
            return;
        }

        var changed = false;

        await _ownershipSync.SyncDeficiencyOwnerFromTaskAsync(task, "task-lifecycle", cancellationToken);
        deficiency = await _deficiencyRepository.GetByIdAsync(deficiencyId, cancellationToken);
        if (deficiency == null)
        {
            return;
        }

        var targetStatus = MapTaskStatusToDeficiency(task.Status, deficiency.Status);
        if (targetStatus.HasValue
            && targetStatus.Value != deficiency.Status
            && DeficiencyLifecycleTransitions.CanTransition(deficiency.Status, targetStatus.Value))
        {
            deficiency.TransitionStatus(targetStatus.Value, "task-lifecycle");
            if (targetStatus == DeficiencyStatus.InProgress)
            {
                await _auditTrail.LogAsync(deficiencyId, "InProgress", null, "System", null, cancellationToken);
            }

            changed = true;
        }

        if (!changed)
        {
            return;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "DeficiencyTaskLifecycle: Synced deficiency {DeficiencyId} from task {TaskId} → {Status}.",
            deficiencyId,
            task.Id,
            deficiency.Status);
    }

    public async Task SyncFromEvidenceAsync(
        Guid taskId,
        EvidenceLifecycleTrigger trigger,
        CancellationToken cancellationToken = default)
    {
        var task = await _db.ComplianceTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task?.SourceDeficiencyId is not { } deficiencyId)
        {
            return;
        }

        var deficiency = await _deficiencyRepository.GetByIdAsync(deficiencyId, cancellationToken);
        if (deficiency == null || deficiency.Status is DeficiencyStatus.Closed or DeficiencyStatus.Cancelled)
        {
            return;
        }

        var target = trigger switch
        {
            EvidenceLifecycleTrigger.Uploaded => DeficiencyStatus.EvidenceSubmitted,
            EvidenceLifecycleTrigger.Approved => DeficiencyStatus.Verified,
            EvidenceLifecycleTrigger.Rejected => DeficiencyStatus.InProgress,
            EvidenceLifecycleTrigger.NeedsMoreEvidence => DeficiencyStatus.EvidenceSubmitted,
            _ => (DeficiencyStatus?)null
        };

        if (target == null || !DeficiencyLifecycleTransitions.CanTransition(deficiency.Status, target.Value))
        {
            return;
        }

        deficiency.TransitionStatus(target.Value, "evidence-lifecycle");

        var action = trigger switch
        {
            EvidenceLifecycleTrigger.Uploaded => "EvidenceUploaded",
            EvidenceLifecycleTrigger.Approved => "Verified",
            EvidenceLifecycleTrigger.Rejected => "EvidenceRejected",
            EvidenceLifecycleTrigger.NeedsMoreEvidence => "EvidenceNeedsMore",
            _ => "EvidenceUpdated",
        };
        await _auditTrail.LogAsync(deficiencyId, action, null, "System", null, cancellationToken);

        if (target == DeficiencyStatus.Verified)
        {
            await TryCloseWhenObligationCompliantAsync(deficiency, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "DeficiencyTaskLifecycle: Evidence {Trigger} on task {TaskId} → deficiency {DeficiencyId} status {Status}.",
            trigger,
            taskId,
            deficiencyId,
            deficiency.Status);
    }

    private async Task TryCloseWhenObligationCompliantAsync(
        ComplianceDeficiency deficiency,
        CancellationToken cancellationToken)
    {
        var obligation = await _db.OrganizationComplianceObligations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == deficiency.ComplianceObligationId, cancellationToken);

        if (obligation == null)
        {
            return;
        }

        var verified = string.Equals(obligation.VerificationStatus.ToString(), "Verified", StringComparison.OrdinalIgnoreCase)
            || string.Equals(obligation.EvidenceStatus.ToString(), "Verified", StringComparison.OrdinalIgnoreCase);

        if (verified && DeficiencyLifecycleTransitions.CanTransition(deficiency.Status, DeficiencyStatus.Closed))
        {
            deficiency.Close("evidence-lifecycle");
            await _auditTrail.LogAsync(deficiency.Id, "Closed", null, "System", null, cancellationToken);
        }
    }

    private static DeficiencyStatus? MapTaskStatusToDeficiency(
        ComplianceTaskStatus taskStatus,
        DeficiencyStatus current) =>
        taskStatus switch
        {
            ComplianceTaskStatus.Pending when current == DeficiencyStatus.Identified
                => DeficiencyStatus.Assigned,
            ComplianceTaskStatus.InProgress => DeficiencyStatus.InProgress,
            _ => null
        };
}
