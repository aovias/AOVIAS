using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

public sealed class ScoringRepository : IScoringRepository
{
    private readonly KnowledgeBaseContext _db;

    public ScoringRepository(KnowledgeBaseContext db) => _db = db;

    public async Task<ScoringConfig?> GetByFrameworkAsync(Guid frameworkId, CancellationToken cancellationToken = default)
    {
        return await _db.ScoringConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.FrameworkId == frameworkId, cancellationToken);
    }

    public async Task<IReadOnlyList<FrameworkClassification>> GetClassificationsAsync(Guid frameworkId, CancellationToken cancellationToken = default)
    {
        return await _db.FrameworkClassifications.AsNoTracking()
            .Where(x => x.FrameworkId == frameworkId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<RequirementScore?> GetRequirementScoreAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _db.RequirementScores.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NodeId == nodeId, cancellationToken);
    }
}
