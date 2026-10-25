using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface IFrameworkRepository
{
    Task<IReadOnlyList<Framework>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Framework?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
