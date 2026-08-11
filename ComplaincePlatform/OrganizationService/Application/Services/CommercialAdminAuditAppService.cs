using Microsoft.EntityFrameworkCore;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;

namespace OrganizationService.Application.Services;

public sealed class CommercialAdminAuditAppService
{
    private readonly OrganizationDbContext _db;
    private readonly ILogger<CommercialAdminAuditAppService> _logger;

    public CommercialAdminAuditAppService(OrganizationDbContext db, ILogger<CommercialAdminAuditAppService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CommercialAdminAuditEvent> RecordAsync(
        Guid? organizationId,
        Guid? operatorUserId,
        string? operatorEmail,
        string targetType,
        string? targetId,
        string action,
        string? previousJson,
        string? newJson,
        string reason,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new InvalidOperationException("Action is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Reason is required for commercial admin actions.");
        }

        var entity = new CommercialAdminAuditEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            OperatorUserId = operatorUserId,
            OperatorEmail = operatorEmail?.Trim() ?? string.Empty,
            TargetType = targetType?.Trim() ?? "Unknown",
            TargetId = targetId,
            Action = action.Trim(),
            PreviousJson = previousJson,
            NewJson = newJson,
            Reason = reason.Trim(),
            CorrelationId = correlationId,
            OccurredAtUtc = DateTime.UtcNow
        };

        _db.CommercialAdminAuditEvents.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "CommercialAdminAudit {Action} Org={OrganizationId} Target={TargetType}/{TargetId} Operator={Operator}",
            entity.Action,
            entity.OrganizationId,
            entity.TargetType,
            entity.TargetId,
            entity.OperatorEmail);
        return entity;
    }

    public async Task<(IReadOnlyList<CommercialAdminAuditEvent> Items, int Total)> SearchAsync(
        Guid? organizationId,
        string? targetType,
        string? action,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.CommercialAdminAuditEvents.AsNoTracking().AsQueryable();
        if (organizationId is { } oid && oid != Guid.Empty)
        {
            q = q.Where(x => x.OrganizationId == oid);
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            q = q.Where(x => x.TargetType == targetType);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            q = q.Where(x => x.Action == action);
        }

        if (fromUtc is { } from)
        {
            q = q.Where(x => x.OccurredAtUtc >= from);
        }

        if (toUtc is { } to)
        {
            q = q.Where(x => x.OccurredAtUtc <= to);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(x => x.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
