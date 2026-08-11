namespace ComplianceService.Domain;

/// <summary>
/// Result of copilot explanation generation with required disclaimer.
/// </summary>
public class CopilotExplanationResult
{
    /// <summary>
    /// The AI-generated explanation text.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// The complete explanation including the required disclaimer.
    /// </summary>
    public string ExplanationWithDisclaimer { get; set; } = string.Empty;

    /// <summary>
    /// The explainability metadata.
    /// </summary>
    public Shared.Infrastructure.DTO.ExplainabilityDto Explainability { get; set; } = null!;

    /// <summary>
    /// Indicates if the explanation was successfully generated.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
