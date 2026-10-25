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
/// </summary>
public sealed class TaskOverdueEscalationJob
{
    private readonly ComplianceDbContext _db;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly InProcessOutboxProcessor _outboxProcessor;
    private readonly ILogger<TaskOverdueEscalationJob> _logger;

    public TaskOverdueEscalationJob(
        ComplianceDbContext db,
        IComplianceEventEmitter eventEmitter,
        IUnitOfWork unitOfWork,
        InProcessOutboxProcessor outboxProcessor,
        ILogger<TaskOverdueEscalationJob> logger)
    {
        _db = db;
        _eventEmitter = eventEmitter;
        _unitOfWork = unitOfWork;
        _outboxProcessor = outboxProcessor;
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
            await _eventEmitter.EmitAsync(
                ComplianceEventType.TaskOverdue,
                task.Id,
                nameof(ComplianceTask),
                task.HospitalId,
                userId: null,
                new
                {
                    task.HospitalId,
                    task.ObjectiveElementId,
                    task.AssignedToUserId,
                    task.DueDate,
                    task.Category,
                    task.TaskKind,
                    task.Priority,
                    task.Criticality
                },
                cancellationToken);

            task.OverdueEscalatedAt = DateTime.UtcNow;
            emitted++;
        }

        if (emitted > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _outboxProcessor.ProcessPendingAsync(cancellationToken);

        _logger.LogInformation("TaskOverdueEscalationJob completed. Emitted={Emitted}", emitted);
    }
}
