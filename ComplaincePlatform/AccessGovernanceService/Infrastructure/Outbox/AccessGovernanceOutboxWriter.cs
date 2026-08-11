using AccessGovernanceService.Domain.Entities;
using AccessGovernanceService.Infrastructure.Persistence;
using System.Text.Json;

namespace AccessGovernanceService.Infrastructure.Outbox;

public static class AccessGovernanceOutboxWriter
{
    public static void Enqueue(AccessGovernanceDbContext db, Guid aggregateId, string eventType, object payload)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            OccurredOn = DateTime.UtcNow,
            Type = eventType,
            Payload = JsonSerializer.Serialize(payload)
        });
    }
}
