using AIOrchestratorService.Domain;

namespace AIOrchestratorService.Application.Services;

/// <summary>
/// Repository interface for DraftArtifact persistence.
/// </summary>
public interface IDraftArtifactRepository
{
    /// <summary>
    /// Adds a new draft artifact.
    /// </summary>
    Task<DraftArtifact> AddAsync(DraftArtifact draftArtifact, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a draft artifact by ID.
    /// </summary>
    Task<DraftArtifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets draft artifacts by source entity ID and draft type.
    /// </summary>
    Task<List<DraftArtifact>> GetBySourceEntityAsync(Guid sourceEntityId, DraftType? draftType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
