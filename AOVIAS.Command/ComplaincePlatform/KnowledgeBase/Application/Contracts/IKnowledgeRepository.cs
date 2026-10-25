using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface IKnowledgeRepository
{
    Task<IReadOnlyList<KnowledgeItem>> GetByNodeAsync(Guid nodeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeItem>> SearchAsync(string? query, Guid? frameworkId, CancellationToken cancellationToken = default);
}
