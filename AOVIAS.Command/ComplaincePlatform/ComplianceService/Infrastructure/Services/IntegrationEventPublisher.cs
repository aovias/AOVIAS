using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using System.Text.Json;

namespace ComplianceService.Infrastructure.Services;

/// <summary>
/// Integration event publisher that stores events in Outbox.
/// Gateway service will read from Outbox and publish to RabbitMQ.
/// This keeps ComplianceService message-broker agnostic.
/// </summary>
public class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ComplianceService.Infrastructure.Persistence.ComplianceDbContext _dbContext;
    private readonly ILogger<IntegrationEventPublisher> _logger;

    public IntegrationEventPublisher(
        ComplianceService.Infrastructure.Persistence.ComplianceDbContext dbContext,
        ILogger<IntegrationEventPublisher> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Map domain events to integration events and store in Outbox
            // The OutboxDomainEventDispatcher will handle domain events
            // This publisher is for explicit integration events

            var eventType = typeof(T).FullName ?? typeof(T).Name;
            var payload = JsonSerializer.Serialize(integrationEvent, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // Generate aggregate ID from integration event
            var aggregateId = ExtractAggregateId(integrationEvent);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = aggregateId,
                OccurredOn = DateTime.UtcNow,
                Type = eventType,
                Payload = payload,
                ProjectionProcessedOn = null,
                RelayProcessedOn = null,
                RetryCount = 0,
                Error = null
            };

            await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

            _logger.LogInformation(
                "Published integration event {EventType} for aggregate {AggregateId}",
                eventType, aggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing integration event {EventType}", typeof(T).Name);
            throw;
        }
    }

    private Guid ExtractAggregateId<T>(T integrationEvent) where T : class
    {
        // Try to extract ComplianceJourneyId from integration event
        var property = typeof(T).GetProperty("ComplianceJourneyId");
        if (property != null && property.GetValue(integrationEvent) is Guid journeyId)
        {
            return journeyId;
        }

        // Fallback: generate deterministic GUID from event properties
        var bytes = new byte[16];
        var hash = integrationEvent.GetHashCode();
        BitConverter.GetBytes(hash).CopyTo(bytes, 0);
        return new Guid(bytes);
    }
}
