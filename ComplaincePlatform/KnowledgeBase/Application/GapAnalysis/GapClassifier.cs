using KnowledgeBase.Application.AssessmentSummary;
using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.GapAnalysis;

public interface IGapClassifier
{
    GapCategory Classify(
        AssessmentItemSnapshot item,
        RequirementSnapshot requirement,
        AssessmentSummaryResult summary,
        Guid? standardAncestorId);
}

public sealed class GapClassifier : IGapClassifier
{
    public GapCategory Classify(
        AssessmentItemSnapshot item,
        RequirementSnapshot requirement,
        AssessmentSummaryResult summary,
        Guid? standardAncestorId)
    {
        _ = requirement;

        if (summary.CoreFailureNodeIds.Contains(item.NodeId) ||
            summary.ActionPlanViolationNodeIds.Contains(item.NodeId) ||
            item.Score <= 1)
        {
            return GapCategory.Critical;
        }

        if (item.Score == 2 ||
            (standardAncestorId.HasValue &&
             summary.StandardsWithMultipleLowScores.Contains(standardAncestorId.Value)))
        {
            return GapCategory.Major;
        }

        if (item.Score == 3)
            return GapCategory.Minor;

        return GapCategory.Compliant;
    }
}
