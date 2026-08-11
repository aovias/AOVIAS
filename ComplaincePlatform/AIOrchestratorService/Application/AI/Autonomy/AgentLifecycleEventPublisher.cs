using System.Text.Json;
using AIOrchestratorService.Application.AI.Configuration;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Autonomy;

public interface IAgentLifecycleEventPublisher
{
    Task PublishAsync(
        string eventType,
        Guid executionId,
        Guid tenantId,
        object payload,
        string? correlationId,
        string? idempotencyKey,
        CancellationToken cancellationToken);
}

public sealed class AgentLifecycleEventPublisher : IAgentLifecycleEventPublisher
{
    private readonly IOptionsMonitor<AIEventProcessingOptions> _eventOptions;
    private readonly ILogger<AgentLifecycleEventPublisher> _logger;

    public AgentLifecycleEventPublisher(
        IOptionsMonitor<AIEventProcessingOptions> eventOptions,
        ILogger<AgentLifecycleEventPublisher> logger)
    {
        _eventOptions = eventOptions;
        _logger = logger;
    }

    public async Task PublishAsync(
        string eventType,
        Guid executionId,
        Guid tenantId,
        object payload,
        string? correlationId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var options = _eventOptions.CurrentValue;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ServiceBusConnectionString))
        {
            return;
        }

        try
        {
            await using var client = new ServiceBusClient(options.ServiceBusConnectionString);
            var sender = client.CreateSender(options.TopicName);
            var resolvedCorrelationId = string.IsNullOrWhiteSpace(correlationId) ? executionId.ToString("N") : correlationId;
            var resolvedMessageId = string.IsNullOrWhiteSpace(idempotencyKey)
                ? $"{eventType}:{executionId:N}:{DateTime.UtcNow:yyyyMMddHHmmssfff}"
                : idempotencyKey;
            var body = JsonSerializer.Serialize(new
            {
                EventType = eventType,
                EntityId = executionId,
                EntityType = "AgentExecution",
                CorrelationId = resolvedCorrelationId,
                IdempotencyKey = resolvedMessageId,
                TenantId = tenantId,
                Timestamp = DateTime.UtcNow,
                Payload = payload
            });
            var message = new ServiceBusMessage(body)
            {
                CorrelationId = resolvedCorrelationId,
                MessageId = resolvedMessageId
            };
            message.ApplicationProperties["eventType"] = eventType;
            message.ApplicationProperties["executionId"] = executionId.ToString("N");
            await sender.SendMessageAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish agent lifecycle event {EventType}", eventType);
        }
    }
}
