using ComplianceService.Application.Aggregation.Models;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;

namespace ComplianceService.Application.Journey;

/// <summary>
/// Accreditation eligibility per NABH workspace contract: overall ≥80%, chapter/standard avg ≥4,
/// each Core OE ≥4, at most one non-NA OE with score ≤2.
/// </summary>
public static class NabhAccreditationEvaluator
{
    public static AccreditationEligibilityDto Evaluate(
        FrameworkAggregateResult aggregate,
        IReadOnlyDictionary<Guid, ObjectiveElementScoreState> latestByObjectiveElementId,
        IReadOnlyDictionary<Guid, ObjectiveElement> objectiveElementsById)
    {
        var violations = new List<string>();

        if (aggregate.CompliancePercent < 80m)
        {
            violations.Add("Overall compliance is below 80%.");
        }

        foreach (var ch in aggregate.Chapters)
        {
            if (ch.AverageScore < 4m)
            {
                violations.Add($"Chapter '{ch.ChapterId}' average score is below 4.");
            }

            foreach (var st in ch.Standards)
            {
                if (st.AverageScore < 4m)
                {
                    violations.Add($"Standard '{st.ComplianceStandardId}' average score is below 4.");
                }
            }
        }

        var lowScoreCount = 0;
        foreach (var kv in latestByObjectiveElementId)
        {
            var oeId = kv.Key;
            var st = kv.Value;
            if (!objectiveElementsById.TryGetValue(oeId, out var oe))
            {
                continue;
            }

            if (!st.IsApplicable)
            {
                continue;
            }

            if (oe.Classification == ObjectiveElementClassification.Core && st.Score < 4)
            {
                violations.Add($"Core objective element '{oe.Code}' score is below 4.");
            }

            if (st.Score <= 2)
            {
                lowScoreCount++;
            }
        }

        if (lowScoreCount > 1)
        {
            violations.Add("More than one objective element has a score of 2 or below (non-NA).");
        }

        var eligible = violations.Count == 0;

        return new AccreditationEligibilityDto
        {
            Eligible = eligible,
            OverallCompliancePercent = aggregate.CompliancePercent,
            Violations = violations
        };
    }
}

/// <summary>Latest score row for an OE within a journey.</summary>
public record ObjectiveElementScoreState(int Score, bool IsApplicable);
