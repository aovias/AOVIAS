namespace Shared.Infrastructure.DTO;

public record ExplainabilityDto
{
    /// <summary>
    /// Original reasoning text from ExplanationComposer (authoritative, internal).
    /// This is the source of truth and remains unchanged.
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// Rewritten reasoning text for user-facing display (optional).
    /// This is the LLM-rewritten version of Reasoning, applied only for clarity.
    /// If not rewritten, this will be empty and Reasoning should be used.
    /// </summary>
    public string RewrittenReasoning { get; init; } = string.Empty;

    public List<string> RulesReferenced { get; init; } = new();
    public double ConfidenceScore { get; init; }
    public bool HumanReviewRequired { get; init; }
    public string GeneratedBy { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
}
