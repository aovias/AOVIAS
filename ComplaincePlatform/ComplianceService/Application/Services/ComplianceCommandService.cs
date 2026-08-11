using ComplianceService.Application.Contracts;
using ComplianceService.Application.Dashboard.Projection;
using ComplianceService.Application.Dashboard.Summary;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DomainScoringService = ComplianceService.Domain.IComplianceScoringService;

namespace ComplianceService.Application.Services;

public class ComplianceCommandService
{
    private readonly IHospitalComplianceRepository _hospitalComplianceRepository;
    private readonly DomainScoringService _scoringService;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ComplianceDbContext _dbContext;
    private readonly OutboxProcessor _outboxProcessor;
    private readonly InProcessOutboxProcessor _inProcessOutboxProcessor;
    private readonly ITaskQueryService _taskQuery;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IObligationProjectionService _obligationProjection;
    private readonly IDashboardSummaryCache _dashboardSummaryCache;

    public ComplianceCommandService(
        IHospitalComplianceRepository hospitalComplianceRepository,
        DomainScoringService scoringService,
        IDomainEventDispatcher domainEventDispatcher,
        ComplianceDbContext dbContext,
        OutboxProcessor outboxProcessor,
        InProcessOutboxProcessor inProcessOutboxProcessor,
        ITaskQueryService taskQuery,
        IComplianceEventEmitter eventEmitter,
        IObligationProjectionService obligationProjection,
        IDashboardSummaryCache dashboardSummaryCache)
    {
        _hospitalComplianceRepository = hospitalComplianceRepository;
        _scoringService = scoringService;
        _domainEventDispatcher = domainEventDispatcher;
        _dbContext = dbContext;
        _outboxProcessor = outboxProcessor;
        _inProcessOutboxProcessor = inProcessOutboxProcessor;
        _taskQuery = taskQuery;
        _eventEmitter = eventEmitter;
        _obligationProjection = obligationProjection;
        _dashboardSummaryCache = dashboardSummaryCache;
    }

    public async Task CompleteTaskAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid taskId,
        CancellationToken ct)
    {
        var pendingReviews = await _taskQuery.GetEvidencePendingReviewCountAsync(taskId, frameworkId, ct);
        if (pendingReviews > 0)
        {
            throw new InvalidOperationException("EVIDENCE_PENDING_REVIEW");
        }

        var rejectedOrNeedsMore = await _taskQuery.GetEvidenceRejectedOrNeedsMoreCountAsync(taskId, frameworkId, ct);
        if (rejectedOrNeedsMore > 0)
        {
            throw new InvalidOperationException("EVIDENCE_REJECTED");
        }

        var missingEvidence = await _taskQuery.HasMissingRequiredEvidenceAsync(taskId, frameworkId, ct);
        if (missingEvidence)
        {
            throw new InvalidOperationException("EVIDENCE_REQUIRED_MISSING");
        }

        // Use execution strategy to wrap transaction (required when retry strategy is enabled)
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            // Load aggregate
            var aggregate = await _hospitalComplianceRepository.LoadAsync(hospitalId, frameworkId, ct);
            if (aggregate == null)
            {
                throw new InvalidOperationException(
                    $"HospitalComplianceAggregate not found for HospitalId={hospitalId}, FrameworkId={frameworkId}.");
            }

            // Execute domain method
            aggregate.CompleteTask(taskId, _scoringService);

            // Save aggregate
            await _hospitalComplianceRepository.SaveAsync(aggregate, ct);

            // Collect DomainEvents
            var domainEvents = aggregate.DomainEvents.ToList();

            // Clear DomainEvents
            aggregate.ClearDomainEvents();

            // Call OutboxDomainEventDispatcher.DispatchAsync(events) - inserts outbox records
            if (domainEvents.Count > 0)
            {
                await _domainEventDispatcher.DispatchAsync(domainEvents, ct);
                // Save outbox messages to the same transaction
                await _dbContext.SaveChangesAsync(ct);
            }

            await _eventEmitter.EmitAsync(
                ComplianceEventType.TaskCompleted,
                taskId,
                "ComplianceTask",
                Guid.Empty,
                null,
                new
                {
                    HospitalId = hospitalId,
                    FrameworkId = frameworkId,
                    TaskId = taskId
                },
                ct);
            await _dbContext.SaveChangesAsync(ct);

            // Commit transaction (state change + outbox insert are atomic)
            await transaction.CommitAsync(ct);
        });

        // After commit: process outbox (score projection + dashboard projection pipeline)
        await _outboxProcessor.ProcessPendingAsync(ct);
        await _inProcessOutboxProcessor.ProcessPendingAsync(ct);

        var tenantId = await _dbContext.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId && j.FrameworkId == frameworkId)
            .Select(j => j.TenantId)
            .FirstOrDefaultAsync(ct);

        if (tenantId != Guid.Empty)
        {
            _dashboardSummaryCache.Invalidate(tenantId, hospitalId, frameworkId);
        }

        var taskRow = await _dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(t => t.Id == taskId && t.HospitalId == hospitalId)
            .Select(t => new { t.ObjectiveElementId })
            .FirstOrDefaultAsync(ct);

        if (taskRow != null)
        {
            await _obligationProjection.ProjectByObjectiveElementAsync(
                hospitalId,
                taskRow.ObjectiveElementId,
                ObligationProjectionTriggers.TaskCompleted,
                ct);
        }
    }
}

