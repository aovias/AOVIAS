using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Services;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

public sealed class NodeRepository : INodeRepository
{
    private readonly KnowledgeBaseContext _db;

    public NodeRepository(KnowledgeBaseContext db) => _db = db;

    public async Task<IReadOnlyList<ComplianceNode>> ListByFrameworkAsync(Guid frameworkId, CancellationToken cancellationToken = default)
    {
        return await _db.ComplianceNodes.AsNoTracking()
            .Where(x => x.FrameworkId == frameworkId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ComplianceNode>> ListByFrameworkWithDetailsAsync(
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ComplianceNodes.AsNoTracking()
            .Where(x => x.FrameworkId == frameworkId)
            .Include(x => x.RequirementDetail)
            .Include(x => x.KnowledgeItems)
            .Include(x => x.RequirementScore)
            .ToListAsync(cancellationToken);
    }

    public async Task<ComplianceNode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.ComplianceNodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ComplianceNode?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.ComplianceNodes.AsNoTracking()
            .Include(x => x.RequirementDetail)
            .Include(x => x.KnowledgeItems)
            .Include(x => x.RequirementScore)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ComplianceNode>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var list = await _db.ComplianceNodes.AsNoTracking()
            .Where(x => x.ParentId == parentId)
            .ToListAsync(cancellationToken);
        list.Sort(new ComplianceNodeSequenceComparer());
        return list;
    }

    public async Task<IReadOnlyList<ComplianceNode>> FindNodesByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _db.ComplianceNodes.AsNoTracking()
            .Where(x => x.Code == code)
            .ToListAsync(cancellationToken);
    }
}
