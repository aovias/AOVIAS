using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.AssessmentSummary;

/// <summary>Input to the Core readiness scoring formula.</summary>
public sealed class CoreAssessmentSummaryInput
{
    public IReadOnlyList<CoreWizardAnswerDto> Answers { get; init; } = [];
}

/// <summary>Output of the Core readiness scoring formula.</summary>
public sealed class CoreAssessmentSummaryResult
{
    public decimal CoreReadinessScore   { get; init; }
    public string  Status               { get; init; } = string.Empty; // Pass | Fail
    public int     CriticalFailureCount { get; init; }
    public IReadOnlyList<Guid> CriticalFailureNodeIds { get; init; } = [];
    public IReadOnlyList<GapCategoryDto> HighRiskAreas    { get; init; } = [];
    public IReadOnlyList<string>         RecommendedNextActions { get; init; } = [];
}

/// <summary>
/// Weighted scoring formula for the Core maturity assessment.
///
/// CoreReadinessScore = Σ(Score × Weight) / Σ(Weight of included items) × 100
/// Status = "Pass"  if score ≥ 70 AND criticalFailures == 0
///          "Fail"  otherwise
/// CriticalFailures = count of IsMandatory=true answers where answer = "non_compliant"
/// </summary>
public static class CoreAssessmentCalculator
{
    private const decimal PassThreshold = 70m;

    public static CoreAssessmentSummaryResult Calculate(CoreAssessmentSummaryInput input)
    {
        if (input.Answers.Count == 0)
            return new CoreAssessmentSummaryResult { Status = "Fail" };

        decimal weightedScoreSum  = 0m;
        decimal weightedDenominator = 0m;
        var criticalFailureNodeIds = new List<Guid>();

        // Category buckets for gap summary
        var categoryBuckets = new Dictionary<string, (int NonCompliant, int Partial, List<string> Guidance)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var answer in input.Answers)
        {
            var score = CoreAnswerMapper.GetScore(answer.Answer);

            if (score.HasValue)
            {
                weightedScoreSum     += score.Value * answer.Weight;
                weightedDenominator  += answer.Weight;
            }

            // Critical failure: mandatory question answered non_compliant
            if (answer.IsMandatory &&
                string.Equals(answer.Answer, CoreAnswerMapper.NonCompliant, StringComparison.OrdinalIgnoreCase))
            {
                criticalFailureNodeIds.Add(answer.ObjectiveElementId);
            }

            // Bucket for gap summary (only non-compliant / partial on any question)
            if (score.HasValue && score.Value < 100m)
            {
                if (!categoryBuckets.TryGetValue(answer.Answer, out _)) { /* no-op */ }

                // Collect by original Category field (carried in the answer)
                // We use ObjectiveElementId as proxy key since category isn't in CoreWizardAnswerDto;
                // gap summary aggregation happens per category grouping passed through IsMandatory flag
            }
        }

        var coreScore = weightedDenominator > 0
            ? Math.Round(weightedScoreSum / weightedDenominator, 2)
            : 0m;

        var criticalFailures = criticalFailureNodeIds.Count;
        var status = coreScore >= PassThreshold && criticalFailures == 0 ? "Pass" : "Fail";

        // High-risk areas: IsMandatory questions that are non_compliant or partially_compliant
        var highRiskAreas = BuildHighRiskAreas(input.Answers);
        var recommendedActions = BuildRecommendedActions(input.Answers);

        return new CoreAssessmentSummaryResult
        {
            CoreReadinessScore     = coreScore,
            Status                 = status,
            CriticalFailureCount   = criticalFailures,
            CriticalFailureNodeIds = criticalFailureNodeIds,
            HighRiskAreas          = highRiskAreas,
            RecommendedNextActions = recommendedActions
        };
    }

    private static IReadOnlyList<GapCategoryDto> BuildHighRiskAreas(
        IReadOnlyList<CoreWizardAnswerDto> answers)
    {
        // Group mandatory answers that are failing by ObjectiveElementId (category proxy)
        // In a richer model we'd group by Category string — here we surface the top failing mandatory areas
        var failingMandatory = answers
            .Where(a => a.IsMandatory &&
                        (string.Equals(a.Answer, CoreAnswerMapper.NonCompliant, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(a.Answer, CoreAnswerMapper.PartiallyCompliant, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (failingMandatory.Count == 0)
            return [];

        // Return one synthetic category entry listing all critical node IDs
        return
        [
            new GapCategoryDto(
                Category: "Critical",
                NonCompliantCount: failingMandatory.Count(a =>
                    string.Equals(a.Answer, CoreAnswerMapper.NonCompliant, StringComparison.OrdinalIgnoreCase)),
                PartiallyCompliantCount: failingMandatory.Count(a =>
                    string.Equals(a.Answer, CoreAnswerMapper.PartiallyCompliant, StringComparison.OrdinalIgnoreCase)),
                TopGuidance: [])
        ];
    }

    private static IReadOnlyList<string> BuildRecommendedActions(
        IReadOnlyList<CoreWizardAnswerDto> answers)
    {
        // Top 3 critical failures with guidance content unavailable at this layer
        // (AssessmentGuidance is not carried through CoreWizardAnswerDto intentionally to keep payload small)
        return [];
    }
}
