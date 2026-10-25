using KnowledgeBase.Application.AssessmentSummary;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.GapAnalysis;
using KnowledgeBase.Domain.Entities;
using Xunit;

namespace Knowledgebase.Tests;

public sealed class GapClassifierTests
{
    private readonly IGapClassifier _classifier = new GapClassifier();

    [Fact]
    public void Core_failure_is_critical()
    {
        var nodeId = Guid.NewGuid();
        var summary = new AssessmentSummaryResult
        {
            CoreFailureNodeIds = [nodeId]
        };
        var item = new AssessmentItemSnapshot { NodeId = nodeId, Score = 3, HasActionPlan = true };

        Assert.Equal(GapCategory.Critical, _classifier.Classify(item, new RequirementSnapshot(), summary, null));
    }

    [Fact]
    public void Score_one_is_critical()
    {
        var nodeId = Guid.NewGuid();
        var item = new AssessmentItemSnapshot { NodeId = nodeId, Score = 1, HasActionPlan = true };

        Assert.Equal(
            GapCategory.Critical,
            _classifier.Classify(item, new RequirementSnapshot(), new AssessmentSummaryResult(), null));
    }

    [Fact]
    public void Score_two_is_major()
    {
        var nodeId = Guid.NewGuid();
        var item = new AssessmentItemSnapshot { NodeId = nodeId, Score = 2, HasActionPlan = true };

        Assert.Equal(
            GapCategory.Major,
            _classifier.Classify(item, new RequirementSnapshot(), new AssessmentSummaryResult(), null));
    }

    [Fact]
    public void Score_three_is_minor_when_not_critical()
    {
        var nodeId = Guid.NewGuid();
        var item = new AssessmentItemSnapshot { NodeId = nodeId, Score = 3, HasActionPlan = true };

        Assert.Equal(
            GapCategory.Minor,
            _classifier.Classify(item, new RequirementSnapshot(), new AssessmentSummaryResult(), null));
    }

    [Fact]
    public void Score_four_or_higher_is_compliant()
    {
        var nodeId = Guid.NewGuid();
        var item = new AssessmentItemSnapshot { NodeId = nodeId, Score = 5, HasActionPlan = false };

        Assert.Equal(
            GapCategory.Compliant,
            _classifier.Classify(item, new RequirementSnapshot(), new AssessmentSummaryResult(), null));
    }

    [Fact]
    public void Standard_with_multiple_low_scores_is_major()
    {
        var nodeId = Guid.NewGuid();
        var standardId = Guid.NewGuid();
        var summary = new AssessmentSummaryResult
        {
            StandardsWithMultipleLowScores = [standardId]
        };
        var item = new AssessmentItemSnapshot { NodeId = nodeId, Score = 4, HasActionPlan = false };

        Assert.Equal(
            GapCategory.Major,
            _classifier.Classify(item, new RequirementSnapshot(), summary, standardId));
    }
}
