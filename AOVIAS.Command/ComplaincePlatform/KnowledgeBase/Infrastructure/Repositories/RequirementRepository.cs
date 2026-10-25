using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

public sealed class RequirementRepository : IRequirementRepository
{
    private readonly KnowledgeBaseContext _db;

    public RequirementRepository(KnowledgeBaseContext db) => _db = db;

    public async Task<RequirementDetail?> GetByNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _db.RequirementDetails.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NodeId == nodeId, cancellationToken);
    }
}
