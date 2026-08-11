using ComplianceService.Domain;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Composes explainability metadata and prepares explanation input for LLMAdapter.
/// Uses cautious, assistive language without changing facts or inferring outcomes.
/// Does NOT detect new gaps, infer severity/risk, predict outcomes, or call AI directly.
/// </summary>
public interface IExplanationComposer
{
    /// <summary>
    /// Composes explainability metadata and explanation input from the provided context.
    /// </summary>
    /// <param name="intent">The copilot intent.</param>
    /// <param name="context">The compliance context.</param>
    /// <param name="gaps">The list of detected gaps (already detected, not detected here).</param>
    /// <returns>Result containing ExplainabilityDto and ExplanationInput.</returns>
    ExplanationCompositionResult Compose(
        CopilotIntent intent,
        ComplianceContext context,
        List<Gap> gaps);

    /// <summary>
    /// Creates a safe fallback Explainability when normal composition cannot be performed.
    /// This ensures CopilotResponse always has Explainability, even when errors occur.
    /// </summary>
    /// <param name="reason">The reason why fallback is needed (e.g., "Context is null", "Composition failed").</param>
    /// <param name="intent">Optional intent for context. If null, minimal explainability is returned.</param>
    /// <returns>A safe fallback ExplainabilityDto with HumanReviewRequired = true.</returns>
    ExplainabilityDto CreateFallbackExplainability(string reason, CopilotIntent? intent = null);
}

/// <summary>
/// Result of explanation composition containing metadata and LLM input.
/// </summary>
public class ExplanationCompositionResult
{
    /// <summary>
    /// The explainability metadata with reasoning and rules.
    /// </summary>
    public ExplainabilityDto Explainability { get; set; } = null!;

    /// <summary>
    /// The structured input prepared for LLMAdapter.
    /// </summary>
    public ExplanationInput ExplanationInput { get; set; } = null!;
}
