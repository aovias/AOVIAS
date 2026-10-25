using GovernanceService.Domain.Entities;
using GovernanceService.Infrastructure.Persistence;

namespace GovernanceService.Application.Services;

public interface IGovernanceAuditWriter
{
    Task WriteAsync(string entityType, Guid entityId, string action, Guid actorUserId, CancellationToken ct = default);
}

public class GovernanceAuditWriter(GovernanceDbContext db) : IGovernanceAuditWriter
{
    public async Task WriteAsync(string entityType, Guid entityId, string action, Guid actorUserId, CancellationToken ct = default)
    {
        db.GovernanceAuditLogs.Add(new GovernanceAuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ActorUserId = actorUserId,
            OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
