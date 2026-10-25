using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface IDecisionRepository
{
    Task AddAsync(DecisionLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DecisionLog>> ListByNodeAsync(Guid nodeId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
