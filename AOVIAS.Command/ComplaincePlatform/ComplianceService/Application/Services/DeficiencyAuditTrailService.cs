using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;

namespace ComplianceService.Application.Services;

public interface IDeficiencyAuditTrailService
{
    Task LogAsync(
        Guid deficiencyId,
        string action,
        Guid? actorUserId,
        string actorType,
        string? detailJson = null,
        CancellationToken cancellationToken = default);
}

public sealed class DeficiencyAuditTrailService : IDeficiencyAuditTrailService
{
    private readonly ComplianceDbContext _db;

    public DeficiencyAuditTrailService(ComplianceDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        Guid deficiencyId,
        string action,
        Guid? actorUserId,
        string actorType,
        string? detailJson = null,
        CancellationToken cancellationToken = default)
    {
        await _db.DeficiencyAuditTrails.AddAsync(new DeficiencyAuditTrail
        {
            Id = Guid.NewGuid(),
            DeficiencyId = deficiencyId,
            Action = action,
            ActorUserId = actorUserId,
            ActorType = actorType,
            DetailJson = detailJson,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);
    }
}
