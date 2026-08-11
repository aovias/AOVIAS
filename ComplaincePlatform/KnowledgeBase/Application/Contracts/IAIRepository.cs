using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface IAIRepository
{
    Task AddAsync(AIInteraction interaction, CancellationToken cancellationToken = default);
    Task<AIInteraction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
