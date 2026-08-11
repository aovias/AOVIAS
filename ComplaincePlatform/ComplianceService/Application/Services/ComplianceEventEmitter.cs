using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Application.Workflow;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

public sealed class ComplianceEventEmitter : IComplianceEventEmitter
{
    private readonly ComplianceDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ComplianceEventEmitter> _logger;

    public ComplianceEventEmitter(
        ComplianceDbContext dbContext,
        IServiceProvider serviceProvider,
        ILogger<ComplianceEventEmitter> logger)
    {
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task EmitAsync(
        ComplianceEventType eventType,
        Guid entityId,
        string entityType,
        Guid tenantId,
        Guid? userId,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var envelope = new ComplianceEventEnvelope
        {
            EntityId = entityId,
            EntityType = entityType,
            TenantId = tenantId,
            UserId = userId,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload)
        };

        var outboxMessage = new OutboxMessage
        {
            Id = envelope.EventId,
            AggregateId = entityId,
            OccurredOn = envelope.Timestamp,
            Type = $"ComplianceEvent.{eventType}",
            Payload = JsonSerializer.Serialize(envelope),
            ProjectionProcessedOn = null,
            RelayProcessedOn = null,
            RetryCount = 0,
            Error = null
        };

        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

        // Deferred resolve avoids ctor cycles (rules → promotion → emitter).
        try
        {
            var engine = _serviceProvider.GetService<IWorkflowRuleEngine>();
            if (engine != null)
            {
                await engine.ExecuteAsync(envelope, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "WorkflowRuleEngine failed for {EventType} entity {EntityId}",
                eventType,
                entityId);
        }
    }
}
