using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;

namespace ComplianceService.Application.Services;

public sealed class FindingAuditTrailService : IFindingAuditTrailService
{
    private readonly ComplianceDbContext _dbContext;

    public FindingAuditTrailService(ComplianceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(
        Guid findingId,
        string action,
        Guid? actorUserId,
        string actorType,
        string? previousStateJson,
        string? newStateJson,
        CancellationToken cancellationToken = default)
    {
        var record = new FindingAuditTrail
        {
            Id = Guid.NewGuid(),
            FindingId = findingId,
            Action = action,
            ActorUserId = actorUserId,
            ActorType = actorType,
            PreviousStateJson = previousStateJson,
            NewStateJson = newStateJson,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.FindingAuditTrails.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
