using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Application service contract for AI-generated drafts.
/// Sprint 13C: adds draft generation, versioning, and source-intelligence retrieval.
/// </summary>
public interface ICopilotDraftService
{
    // ── Read ──────────────────────────────────────────────────────────────────

    /// <summary>Returns all drafts for an organization (latest version of each group).</summary>
    Task<IReadOnlyList<AiDraftDto>> GetDraftsAsync(
        Guid organizationId,
        CopilotContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a single draft by ID.</summary>
    Task<AiDraftDto?> GetDraftAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all versions of the draft group for a given draft ID.</summary>
    Task<IReadOnlyList<AiDraftDto>> GetVersionsAsync(
        Guid draftId,
        CancellationToken cancellationToken = default);

    // ── Generate ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a new draft from the given request.
    /// Does NOT create tasks, CAPAs, findings, or modify compliance state.
    /// </summary>
    Task<AiDraftDto> GenerateDraftAsync(
        Guid organizationId,
        GenerateDraftRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates a new version of an existing draft.
    /// Original draft is preserved; new entity with incremented version is created.
    /// </summary>
    Task<AiDraftDto> RegenerateDraftAsync(
        Guid draftId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>Updates draft content — increments version for human edits.</summary>
    Task UpdateDraftAsync(Guid id, string newContent, CancellationToken cancellationToken = default);

    /// <summary>Archives a draft permanently.</summary>
    Task ArchiveDraftAsync(Guid id, CancellationToken cancellationToken = default);
}
