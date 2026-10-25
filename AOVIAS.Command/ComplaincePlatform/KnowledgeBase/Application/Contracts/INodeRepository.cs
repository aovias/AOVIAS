using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface INodeRepository
{
    Task<IReadOnlyList<ComplianceNode>> ListByFrameworkAsync(Guid frameworkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComplianceNode>> ListByFrameworkWithDetailsAsync(
        Guid frameworkId,
        CancellationToken cancellationToken = default);
    Task<ComplianceNode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ComplianceNode?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceNode>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>All nodes with the given <paramref name="code"/>.</summary>
    Task<IReadOnlyList<ComplianceNode>> FindNodesByCodeAsync(string code, CancellationToken cancellationToken = default);
}
