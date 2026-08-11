using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ComplianceService.Application.Services;

public class OutboxProcessor
{
    private readonly ComplianceDbContext _dbContext;
    private readonly IComplianceProjectionHandler _projectionHandler;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        ComplianceDbContext dbContext,
        IComplianceProjectionHandler projectionHandler,
        ILogger<OutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _projectionHandler = projectionHandler;
        _logger = logger;
    }

    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        // Projection pipeline only handles score-updated domain events; skip poison (RetryCount >= MaxRetries).
        var pendingMessages = await _dbContext.OutboxMessages
            .Where(m =>
                m.ProjectionProcessedOn == null
                && m.RetryCount < OutboxConstants.MaxRetries
                && m.Type.Contains(nameof(ComplianceScoreUpdatedDomainEvent)))
            .OrderBy(m => m.OccurredOn)
            .ToListAsync(ct);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} pending outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            try
            {
                // Deserialize payload to ComplianceScoreUpdatedDomainEvent
                var domainEvent = JsonSerializer.Deserialize<ComplianceScoreUpdatedDomainEvent>(message.Payload);
                
                if (domainEvent == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize OutboxMessage {message.Id} to ComplianceScoreUpdatedDomainEvent");
                }

                // Call projector
                await _projectionHandler.HandleAsync(domainEvent, ct);

                // Mark projection as processed
                message.ProjectionProcessedOn = DateTime.UtcNow;
                message.Error = null;

                _logger.LogDebug(
                    "Successfully processed OutboxMessage {MessageId} for HospitalId: {HospitalId}, FrameworkId: {FrameworkId}",
                    message.Id, domainEvent.HospitalId, domainEvent.FrameworkId);
            }
            catch (Exception ex)
            {
                // Keep pending for retry and capture error state.
                message.RetryCount += 1;
                message.Error = ex.Message;

                _logger.LogError(ex,
                    "Error processing OutboxMessage {MessageId}: {Error}",
                    message.Id, ex.Message);
            }
        }

        // Save all changes
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Completed processing {Count} outbox messages", pendingMessages.Count);
    }
}
