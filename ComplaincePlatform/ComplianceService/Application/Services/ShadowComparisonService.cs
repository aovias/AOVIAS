using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class ShadowComparisonService : IShadowComparisonService
{
    private readonly ComplianceDbContext _dbContext;

    public ShadowComparisonService(ComplianceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CompareAndLogAsync(
        string entityType,
        Guid entityId,
        string action,
        object oldPayload,
        object newPayload,
        Guid performedBy,
        CancellationToken cancellationToken = default)
    {
        var oldJson = JsonSerializer.Serialize(oldPayload);
        var newJson = JsonSerializer.Serialize(newPayload);
        if (oldJson == newJson)
        {
            return;
        }

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = $"shadow_mismatch:{action}",
            PerformedBy = performedBy == Guid.Empty ? Guid.Empty : performedBy,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new
            {
                oldPayload,
                newPayload
            })
        };

        await _dbContext.AuditLogs.AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShadowMismatchDto>> GetRecentMismatchesAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var size = Math.Clamp(take, 1, 500);
        var rows = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.Action.StartsWith("shadow_mismatch:"))
            .OrderByDescending(x => x.Timestamp)
            .Take(size)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new ShadowMismatchDto
        {
            Id = x.Id,
            EntityType = x.EntityType,
            EntityId = x.EntityId,
            Action = x.Action,
            Timestamp = x.Timestamp,
            MetadataJson = x.MetadataJson
        }).ToList();
    }
}
