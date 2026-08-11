namespace ComplianceService.Application.Contracts;

/// <summary>
/// Publisher for integration events.
/// Note: RabbitMQ usage is ONLY in Gateway. This interface allows ComplianceService
/// to publish events without direct RabbitMQ dependency.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : class;
}
