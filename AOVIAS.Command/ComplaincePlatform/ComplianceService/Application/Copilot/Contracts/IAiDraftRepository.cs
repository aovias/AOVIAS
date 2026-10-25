using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Persistence contract for AiDraft entities.
/// Supports versioned draft lifecycle. No generation logic (generation is in IDraftGenerationService).
/// </summary>
public interface IAiDraftRepository
{
    Task CreateAsync(AiDraft draft, CancellationToken cancellationToken = default);

    Task<AiDraft?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiDraft>> SearchAsync(
        Guid organizationId,
        DraftStatus? status = null,
        string? persona = null,
        DraftType? draftType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Persists updated content and incremented version.</summary>
    Task UpdateAsync(AiDraft draft, CancellationToken cancellationToken = default);

    /// <summary>Archives the draft — no further edits possible.</summary>
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all versions of the same logical draft group ordered by version descending.
    /// GroupId is shared across all regenerations of the same draft.
    /// </summary>
    Task<IReadOnlyList<AiDraft>> GetVersionsByGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy: returns drafts by organization + title match.
    /// Use GetVersionsByGroupAsync(groupId) for Sprint 13C versioning.
    /// </summary>
    Task<IReadOnlyList<AiDraft>> GetVersionHistoryAsync(
        Guid organizationId,
        string title,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
