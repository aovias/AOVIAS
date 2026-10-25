using ComplianceService.Domain;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Factory interface for creating CopilotResponse instances with enforced Explainability.
/// Ensures that every CopilotResponse always includes Explainability metadata.
/// </summary>
public interface ICopilotResponseFactory
{
    /// <summary>
    /// Creates a CopilotResponse with enforced Explainability.
    /// Always calls ExplanationComposer to generate explainability metadata.
    /// If composition fails, uses safe fallback explainability.
    /// Applies LLM rewrite to Reasoning text for improved clarity (user-facing).
    /// </summary>
    /// <param name="intent">The copilot intent.</param>
    /// <param name="context">The compliance context.</param>
    /// <param name="gaps">The list of detected gaps.</param>
    /// <param name="summary">The AI-generated summary text.</param>
    /// <param name="findings">The gap findings.</param>
    /// <param name="suggestedNextSteps">Optional suggested next steps.</param>
    /// <param name="sourceReferences">Optional source references.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A CopilotResponse with guaranteed Explainability.</returns>
    Task<CopilotResponse> CreateAsync(
        CopilotIntent intent,
        ComplianceContext context,
        List<Gap> gaps,
        string summary,
        List<GapResult> findings,
        List<SuggestedNextStep>? suggestedNextSteps = null,
        SourceReferences? sourceReferences = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a CopilotResponse with enforced Explainability when context or gaps are unavailable.
    /// Uses fallback explainability to ensure Explainability is always present.
    /// Applies LLM rewrite to Reasoning text for improved clarity (user-facing).
    /// </summary>
    /// <param name="intent">The copilot intent.</param>
    /// <param name="summary">The AI-generated summary text.</param>
    /// <param name="findings">The gap findings.</param>
    /// <param name="reason">The reason why normal composition cannot be performed.</param>
    /// <param name="suggestedNextSteps">Optional suggested next steps.</param>
    /// <param name="sourceReferences">Optional source references.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A CopilotResponse with fallback Explainability.</returns>
    Task<CopilotResponse> CreateWithFallbackAsync(
        CopilotIntent intent,
        string summary,
        List<GapResult> findings,
        string reason,
        List<SuggestedNextStep>? suggestedNextSteps = null,
        SourceReferences? sourceReferences = null,
        CancellationToken ct = default);
}
