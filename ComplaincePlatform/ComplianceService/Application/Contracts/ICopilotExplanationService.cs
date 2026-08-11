using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Service that orchestrates copilot explanation generation.
/// Coordinates ExplanationComposer and LLMExplanationAdapter.
/// </summary>
public interface ICopilotExplanationService
{
    /// <summary>
    /// Generates a copilot explanation from intent, gap results, and context.
    /// </summary>
    /// <param name="intent">The copilot intent.</param>
    /// <param name="gapResults">The gap analysis results.</param>
    /// <param name="context">The compliance context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>CopilotExplanationResult with explanation and required disclaimer.</returns>
    Task<CopilotExplanationResult> GenerateExplanationAsync(
        CopilotIntent intent,
        GapAnalysisResult gapResults,
        ComplianceContext context,
        CancellationToken ct = default);
}
