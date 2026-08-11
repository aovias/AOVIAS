using Microsoft.EntityFrameworkCore;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;

namespace OrganizationService.Application.Services;

public sealed class EmailDeliveryLogAppService
{
    private readonly OrganizationDbContext _db;

    public EmailDeliveryLogAppService(OrganizationDbContext db) => _db = db;

    public async Task<EmailDeliveryLog> RecordAsync(
        Guid? organizationId,
        Guid? userId,
        string templateKey,
        string toAddress,
        string? subject,
        string status,
        string? error,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        var entity = new EmailDeliveryLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            TemplateKey = templateKey,
            ToAddress = toAddress,
            Subject = subject,
            Status = status,
            Error = error,
            CorrelationId = correlationId,
            SentAtUtc = DateTime.UtcNow
        };
        _db.EmailDeliveryLogs.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<(IReadOnlyList<EmailDeliveryLog> Items, int Total)> SearchAsync(
        Guid? organizationId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = _db.EmailDeliveryLogs.AsNoTracking().AsQueryable();
        if (organizationId is { } oid)
        {
            q = q.Where(x => x.OrganizationId == oid);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(x => x.Status == status);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderByDescending(x => x.SentAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
