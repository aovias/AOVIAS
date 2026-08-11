using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

public sealed class KnowledgeRepository : IKnowledgeRepository
{
    private readonly KnowledgeBaseContext _db;

    public KnowledgeRepository(KnowledgeBaseContext db) => _db = db;

    public async Task<IReadOnlyList<KnowledgeItem>> GetByNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _db.KnowledgeItems.AsNoTracking()
            .Where(x => x.NodeId == nodeId && x.IsActive)
            .OrderBy(x => x.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeItem>> SearchAsync(string? query, Guid? frameworkId, CancellationToken cancellationToken = default)
    {
        var q = _db.KnowledgeItems
            .AsNoTracking()
            .Include(x => x.Node)
            .Where(x => x.IsActive);

        if (frameworkId is not null)
        {
            var fid = frameworkId.Value;
            q = q.Where(x => x.Node != null && x.Node.FrameworkId == fid);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            var pattern = $"%{term.ToUpper()}%";
            q = q.Where(x =>
                EF.Functions.Like((x.Explanation ?? string.Empty).ToUpper(), pattern) ||
                EF.Functions.Like((x.Tags ?? string.Empty).ToUpper(), pattern) ||
                EF.Functions.Like((x.HowToComply ?? string.Empty).ToUpper(), pattern) ||
                (x.Node != null && EF.Functions.Like((x.Node.Code ?? string.Empty).ToUpper(), pattern)) ||
                (x.Node != null && EF.Functions.Like((x.Node.Title ?? string.Empty).ToUpper(), pattern)));
        }

        return await q
            .OrderByDescending(x => x.Version)
            .Take(10)
            .ToListAsync(cancellationToken);
    }
}
