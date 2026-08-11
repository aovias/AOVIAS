namespace KnowledgeBase.Application.AssessmentSummary;

/// <summary>
/// Maps the Core wizard answer strings to numeric scores used by the scoring formula.
/// Scale: Compliant=100 / PartiallyCompliant=50 / NonCompliant=0 / NotApplicable=excluded.
/// This is intentionally separate from WizardAssessmentAnswerMapper (1–5 scale path).
/// </summary>
public static class CoreAnswerMapper
{
    public const string Compliant            = "compliant";
    public const string PartiallyCompliant   = "partially_compliant";
    public const string NonCompliant         = "non_compliant";
    public const string NotApplicable        = "not_applicable";

    /// <summary>
    /// Returns the numeric score for an answer string, or <c>null</c> when the answer is
    /// <c>not_applicable</c> (excluded from the denominator).
    /// </summary>
    public static decimal? GetScore(string answer) => answer?.ToLowerInvariant() switch
    {
        Compliant          => 100m,
        PartiallyCompliant => 50m,
        NonCompliant       => 0m,
        NotApplicable      => null,
        _                  => null   // treat unknown as not_applicable
    };

    /// <summary>True when the answer should be included in the weighted denominator.</summary>
    public static bool IsIncluded(string answer) =>
        GetScore(answer).HasValue;
}
