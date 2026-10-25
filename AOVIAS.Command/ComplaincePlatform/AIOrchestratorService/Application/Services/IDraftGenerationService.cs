using AIOrchestratorService.Domain;

namespace AIOrchestratorService.Application.Services;

/// <summary>
/// Service interface for AI-assisted draft generation.
/// </summary>
public interface IDraftGenerationService
{
    /// <summary>
    /// Generates a draft artifact from the provided request.
    /// </summary>
    /// <param name="request">The structured drafting request.</param>
    /// <param name="generatedBy">The AI model or system generating the draft (e.g., "GPT-4", "Claude-3").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created DraftArtifact with status = Draft.</returns>
    Task<DraftArtifact> GenerateDraftAsync(
        DraftingRequest request,
        string generatedBy,
        CancellationToken cancellationToken = default);
}
