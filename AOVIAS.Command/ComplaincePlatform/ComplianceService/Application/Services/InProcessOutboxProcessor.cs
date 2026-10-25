using ComplianceService.Application.Contracts;
using ComplianceService.Application.Dashboard.Projection;
using ComplianceService.Application.Dashboard.Summary;
using ComplianceService.Domain;
using ComplianceService.Domain.Events;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ComplianceService.Application.Services;

public class InProcessOutboxProcessor
{
    private readonly ComplianceDbContext _dbContext;
    private readonly IComplianceProjectionHandler _projectionHandler;
    private readonly IComplianceSnapshotRecalculationQueue _snapshotRecalculationQueue;
    private readonly IDeficiencyRemediationTaskService _deficiencyRemediationTaskService;
    private readonly IDashboardSummaryCache _dashboardSummaryCache;
    private readonly IDashboardProjectionEngine _dashboardProjectionEngine;

    public InProcessOutboxProcessor(
        ComplianceDbContext dbContext,
        IComplianceProjectionHandler projectionHandler,
        IComplianceSnapshotRecalculationQueue snapshotRecalculationQueue,
        IDeficiencyRemediationTaskService deficiencyRemediationTaskService,
        IDashboardSummaryCache dashboardSummaryCache,
        IDashboardProjectionEngine dashboardProjectionEngine)
    {
        _dbContext = dbContext;
        _projectionHandler = projectionHandler;
        _snapshotRecalculationQueue = snapshotRecalculationQueue;
        _deficiencyRemediationTaskService = deficiencyRemediationTaskService;
        _dashboardSummaryCache = dashboardSummaryCache;
        _dashboardProjectionEngine = dashboardProjectionEngine;
    }

    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        // DEF-M5-004 — IsDashboardProjectionCandidate is a C# method and cannot be translated to
        // SQL, so it must run after materialisation. The translatable predicates stay in the
        // database; only the type match moves to memory, over the bounded set of unprocessed rows.
        // Selection and ordering are unchanged.
        var unprocessed = await _dbContext.OutboxMessages
            .Where(m =>
                m.ProjectionProcessedOn == null
                && m.RetryCount < OutboxConstants.MaxRetries)
            .OrderBy(m => m.OccurredOn)
            .ToListAsync(ct);

        var pendingMessages = unprocessed
            .Where(m => IsDashboardProjectionCandidate(m.Type))
            .ToList();

        if (pendingMessages.Count == 0)
        {
            return;
        }

        foreach (var message in pendingMessages)
        {
            try
            {
                if (message.Type.Contains(nameof(ComplianceScoreUpdatedDomainEvent)))
                {
                    var domainEvent = JsonSerializer.Deserialize<ComplianceScoreUpdatedDomainEvent>(message.Payload)
                        ?? throw new InvalidOperationException($"Failed to deserialize OutboxMessage {message.Id} to ComplianceScoreUpdatedDomainEvent");

                    await _projectionHandler.HandleAsync(domainEvent, ct);
                    await InvalidateSummaryCacheAsync(domainEvent.HospitalId, domainEvent.FrameworkId, ct);
                }
                else if (message.Type.Contains(nameof(ObligationStatusUpdatedDomainEvent)))
                {
                    var obligationEvent = JsonSerializer.Deserialize<ObligationStatusUpdatedDomainEvent>(message.Payload)
                        ?? throw new InvalidOperationException($"Failed to deserialize OutboxMessage {message.Id} to ObligationStatusUpdatedDomainEvent");

                    _snapshotRecalculationQueue.EnqueueHospitalFramework(
                        obligationEvent.HospitalId,
                        obligationEvent.FrameworkId);

                    _dashboardSummaryCache.Invalidate(
                        obligationEvent.OrganizationId,
                        obligationEvent.HospitalId,
                        obligationEvent.FrameworkId);
                }
                else if (message.Type.Contains(nameof(DeficiencyIdentifiedDomainEvent)))
                {
                    var deficiencyEvent = JsonSerializer.Deserialize<DeficiencyIdentifiedDomainEvent>(message.Payload)
                        ?? throw new InvalidOperationException($"Failed to deserialize OutboxMessage {message.Id} to DeficiencyIdentifiedDomainEvent");

                    await _deficiencyRemediationTaskService.EnsureRemediationTaskAsync(
                        deficiencyEvent.DeficiencyId,
                        ct);

                    _snapshotRecalculationQueue.EnqueueHospitalFramework(
                        deficiencyEvent.HospitalId,
                        deficiencyEvent.FrameworkId);

                    await InvalidateSummaryCacheAsync(deficiencyEvent.HospitalId, deficiencyEvent.FrameworkId, ct);
                }
                else if (message.Type.Contains(nameof(DeficiencyClosedDomainEvent)))
                {
                    var deficiencyClosedEvent = JsonSerializer.Deserialize<DeficiencyClosedDomainEvent>(message.Payload)
                        ?? throw new InvalidOperationException($"Failed to deserialize OutboxMessage {message.Id} to DeficiencyClosedDomainEvent");

                    _snapshotRecalculationQueue.EnqueueHospitalFramework(
                        deficiencyClosedEvent.HospitalId,
                        deficiencyClosedEvent.FrameworkId);

                    await InvalidateSummaryCacheAsync(deficiencyClosedEvent.HospitalId, deficiencyClosedEvent.FrameworkId, ct);
                }
                else if (message.Type.Contains(nameof(DeficiencyStatusChangedDomainEvent)))
                {
                    var statusEvent = JsonSerializer.Deserialize<DeficiencyStatusChangedDomainEvent>(message.Payload)
                        ?? throw new InvalidOperationException($"Failed to deserialize OutboxMessage {message.Id} to DeficiencyStatusChangedDomainEvent");

                    _snapshotRecalculationQueue.EnqueueHospitalFramework(
                        statusEvent.HospitalId,
                        statusEvent.FrameworkId);

                    await InvalidateSummaryCacheAsync(statusEvent.HospitalId, statusEvent.FrameworkId, ct);
                }

                if (message.Type.Contains("FrameworkEnrolled", StringComparison.OrdinalIgnoreCase))
                {
                    var enrolled = JsonSerializer.Deserialize<FrameworkEnrolledDomainEvent>(message.Payload);
                    if (enrolled is null || enrolled.HospitalId == Guid.Empty || enrolled.FrameworkId == Guid.Empty)
                    {
                        throw new InvalidOperationException(
                            $"FrameworkEnrolled outbox {message.Id} payload is missing HospitalId/FrameworkId; not marking projection processed.");
                    }
                }

                await _dashboardProjectionEngine.ProcessOutboxMessageAsync(message, ct);

                message.ProjectionProcessedOn = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount += 1;
                message.Error = ex.Message;
            }
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private static bool IsDashboardProjectionCandidate(string type) =>
        type.Contains(nameof(ComplianceScoreUpdatedDomainEvent), StringComparison.OrdinalIgnoreCase)
        || type.Contains(nameof(ObligationStatusUpdatedDomainEvent), StringComparison.OrdinalIgnoreCase)
        || type.Contains(nameof(DeficiencyIdentifiedDomainEvent), StringComparison.OrdinalIgnoreCase)
        || type.Contains(nameof(DeficiencyClosedDomainEvent), StringComparison.OrdinalIgnoreCase)
        || type.Contains(nameof(DeficiencyStatusChangedDomainEvent), StringComparison.OrdinalIgnoreCase)
        || type.Contains(nameof(ComplianceStateChangedDomainEvent), StringComparison.OrdinalIgnoreCase)
        || type.StartsWith("ComplianceEvent.", StringComparison.OrdinalIgnoreCase)
        || type.Contains("FrameworkEnrolled", StringComparison.OrdinalIgnoreCase)
        || type.Contains("ComplianceEnrolled", StringComparison.OrdinalIgnoreCase)
        || type.Contains("ComplianceSubmitted", StringComparison.OrdinalIgnoreCase)
        || type.Contains("ComplianceApproved", StringComparison.OrdinalIgnoreCase)
        || type.Contains("ComplianceRenewed", StringComparison.OrdinalIgnoreCase)
        || type.Contains("ComplianceRejected", StringComparison.OrdinalIgnoreCase)
        || type.Contains("ComplianceExpired", StringComparison.OrdinalIgnoreCase);

    private async Task InvalidateSummaryCacheAsync(Guid hospitalId, Guid frameworkId, CancellationToken ct)
    {
        var tenantId = await _dbContext.ComplianceJourneys
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(j => j.HospitalId == hospitalId && j.FrameworkId == frameworkId)
            .Select(j => j.TenantId)
            .FirstOrDefaultAsync(ct);

        if (tenantId != Guid.Empty)
        {
            _dashboardSummaryCache.Invalidate(tenantId, hospitalId, frameworkId);
        }
    }
}
