using ComplianceService.Application.Authorization;
using ComplianceService.Application.Services;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Emits <see cref="ComplianceEventType.TaskOverdue"/> for overdue open tasks (once per overdue episode).
/// Envelope <c>TenantId</c> is OrganizationId; nested payload carries HospitalId and FrameworkId when known.
/// </summary>
public sealed class TaskOverdueEscalationJob
{
    private readonly ComplianceDbContext _db;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InProcessOutboxProcessor _outboxProcessor;
    private readonly IHospitalDirectory _hospitalDirectory;
    private readonly ILogger<TaskOverdueEscalationJob> _logger;

    public TaskOverdueEscalationJob(
        ComplianceDbContext db,
        IComplianceEventEmitter eventEmitter,
        IUnitOfWork unitOfWork,
        InProcessOutboxProcessor outboxProcessor,
        IHospitalDirectory hospitalDirectory,
        ILogger<TaskOverdueEscalationJob> logger)
    {
        _db = db;
        _eventEmitter = eventEmitter;
        _unitOfWork = unitOfWork;
        _outboxProcessor = outboxProcessor;
        _hospitalDirectory = hospitalDirectory;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessOverdueTasksAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        _logger.LogInformation("Starting TaskOverdueEscalationJob for {Today:yyyy-MM-dd}", today);

        var overdue = await _db.ComplianceTasks
            .Where(t => t.DueDate != null
                        && t.DueDate.Value.Date < today
                        && t.Status != ComplianceTaskStatus.Completed
                        && t.OverdueEscalatedAt == null)
            .OrderBy(t => t.DueDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var emitted = 0;
        foreach (var task in overdue)
        {
            if (await TryEmitOverdueAsync(task, cancellationToken))
            {
                task.OverdueEscalatedAt = DateTime.UtcNow;
                emitted++;
            }
        }

        if (emitted > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _outboxProcessor.ProcessPendingAsync(cancellationToken);

        _logger.LogInformation("TaskOverdueEscalationJob completed. Emitted={Emitted}", emitted);
    }

    /// <summary>Emits a single overdue event using the same tenant contract as the batch job.</summary>
    public async Task<bool> TryEmitOverdueAsync(ComplianceTask task, CancellationToken cancellationToken = default)
    {
        var organizationId = await ResolveOrganizationIdAsync(task.HospitalId, cancellationToken);
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning(
                "Skipping TaskOverdue for task {TaskId}: could not resolve OrganizationId for hospital {HospitalId}",
                task.Id,
                task.HospitalId);
            return false;
        }

        var frameworkId = await ResolveFrameworkIdAsync(task.ComplianceStandardId, cancellationToken);

        await _eventEmitter.EmitAsync(
            ComplianceEventType.TaskOverdue,
            task.Id,
            nameof(ComplianceTask),
            organizationId,
            userId: null,
            new
            {
                task.HospitalId,
                FrameworkId = frameworkId,
                task.ObjectiveElementId,
                task.AssignedToUserId,
                task.DueDate,
                task.Category,
                task.TaskKind,
                task.Priority,
                task.Criticality
            },
            cancellationToken);

        return true;
    }

    private async Task<Guid> ResolveOrganizationIdAsync(Guid hospitalId, CancellationToken cancellationToken)
    {
        var fromJourney = await _db.ComplianceJourneys
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(j => j.HospitalId == hospitalId)
            .Select(j => j.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (fromJourney != Guid.Empty)
        {
            return fromJourney;
        }

        var lookup = await _hospitalDirectory.GetHospitalAsync(hospitalId, cancellationToken);
        return lookup.Hospital?.OrganizationId ?? Guid.Empty;
    }

    private async Task<Guid?> ResolveFrameworkIdAsync(Guid complianceStandardId, CancellationToken cancellationToken)
    {
        var frameworkId = await _db.ComplianceStandards
            .AsNoTracking()
            .Where(s => s.Id == complianceStandardId)
            .Select(s => s.Chapter.FrameworkId)
            .FirstOrDefaultAsync(cancellationToken);

        return frameworkId == Guid.Empty ? null : frameworkId;
    }
}
