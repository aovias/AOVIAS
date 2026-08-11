namespace ComplianceService.Domain;

/// <summary>
/// Structured input prepared for LLMAdapter to generate explanations.
/// Contains facts and context without inference or outcome predictions.
/// </summary>
public class ExplanationInput
{
    /// <summary>
    /// The user's original question.
    /// </summary>
    public string UserQuestion { get; set; } = string.Empty;

    /// <summary>
    /// The detected question type.
    /// </summary>
    public CopilotQuestionType QuestionType { get; set; }

    /// <summary>
    /// Summary of the compliance context (facts only, no inference).
    /// </summary>
    public string ContextSummary { get; set; } = string.Empty;

    /// <summary>
    /// Summary of detected gaps (facts only, no inference).
    /// </summary>
    public string GapSummary { get; set; } = string.Empty;

    /// <summary>
    /// List of rules that were applied during analysis.
    /// </summary>
    public List<string> AppliedRules { get; set; } = new();

    /// <summary>
    /// Entity references relevant to the explanation.
    /// </summary>
    public Dictionary<string, string> EntityReferences { get; set; } = new();
}
