using ComplianceService.Application.DTOs;
using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Adapter interface for generating explanations using LLM.
/// LLM receives structured inputs only and explains findings (does not detect them).
/// </summary>
public interface ILLMExplanationAdapter
{
    /// <summary>
    /// Generates an explanation from structured explanation input.
    /// </summary>
    /// <param name="input">Structured input containing facts and context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Generated explanation text or error message.</returns>
    Task<string> GenerateExplanationAsync(ExplanationInput input, CancellationToken ct = default);

    /// <summary>
    /// Rewrites an existing explanation text to improve clarity and readability.
    /// This is a REWRITE operation, not a generation operation.
    /// </summary>
    /// <param name="request">The rewrite request containing original text, audience type, and tone.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Rewritten explanation text with same meaning, improved clarity, and better readability. Returns original text if rewrite fails.</returns>
    Task<string> RewriteExplanationAsync(ExplanationRewriteRequest request, CancellationToken ct = default);
}
