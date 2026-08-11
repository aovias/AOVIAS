using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Models;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Orchestrates the full draft generation pipeline:
/// template resolution → context assembly → content generation → persistence → versioning.
/// </summary>
public interface IDraftGenerationService
{
    /// <summary>
    /// Generates a brand-new draft (version 1) from a generation request.
    /// Does NOT create tasks, findings, CAPAs, or modify any workflow state.
    /// </summary>
    Task<AiDraftDto> GenerateAsync(
        DraftGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates a new version of an existing draft group.
    /// The original draft is preserved; a new entity with incremented Version is created.
    /// </summary>
    Task<AiDraftDto> RegenerateAsync(
        Guid existingDraftId,
        DraftGenerationRequest? overrideRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all versions of the draft group for the given draft ID.</summary>
    Task<IReadOnlyList<AiDraftDto>> GetVersionsAsync(
        Guid draftId,
        CancellationToken cancellationToken = default);
}
