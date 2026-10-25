using KnowledgeBase.Application.AssessmentSummary;
using KnowledgeBase.Application.Contracts;
using Xunit;

namespace Knowledgebase.Tests;

public sealed class AssessmentSummaryCalculatorTests
{
    private readonly IAssessmentSummaryCalculator _calc = new AssessmentSummaryCalculator();

    [Fact]
    public void Core_failure_when_core_below_4()
    {
        var chapter = Guid.NewGuid();
        var standard = Guid.NewGuid();
        var oe = Guid.NewGuid();
        var input = new AssessmentSummaryInput
        {
            Nodes =
            [
                new HierarchyNode { Id = chapter, ParentId = null, NodeType = "Chapter" },
                new HierarchyNode { Id = standard, ParentId = chapter, NodeType = "Standard" },
                new HierarchyNode { Id = oe, ParentId = standard, NodeType = "ObjectiveElement" }
            ],
            Items = [new AssessmentItemSnapshot { NodeId = oe, Score = 3, HasActionPlan = true }],
            Requirements = [new RequirementSnapshot { NodeId = oe, Classification = "CORE" }]
        };
        var r = _calc.Calculate(input);
        Assert.False(r.CoreRulePassed);
        Assert.Contains(oe, r.CoreFailureNodeIds);
    }

    [Fact]
    public void Action_plan_violation_when_score_le_3_without_plan()
    {
        var n = Guid.NewGuid();
        var input = new AssessmentSummaryInput
        {
            Nodes = [new HierarchyNode { Id = n, ParentId = null, NodeType = "Chapter" }],
            Items = [new AssessmentItemSnapshot { NodeId = n, Score = 3, HasActionPlan = false }],
            Requirements = [new RequirementSnapshot { NodeId = n, Classification = "Achievement" }]
        };
        var r = _calc.Calculate(input);
        Assert.Contains(n, r.ActionPlanViolationNodeIds);
    }

    [Fact]
    public void Overall_percent_is_average_of_score_over_5()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var input = new AssessmentSummaryInput
        {
            Nodes =
            [
                new HierarchyNode { Id = a, ParentId = null, NodeType = "Chapter" },
                new HierarchyNode { Id = b, ParentId = a, NodeType = "ObjectiveElement" }
            ],
            Items =
            [
                new AssessmentItemSnapshot { NodeId = b, Score = 5, HasActionPlan = false },
                new AssessmentItemSnapshot { NodeId = a, Score = 3, HasActionPlan = true }
            ],
            Requirements =
            [
                new RequirementSnapshot { NodeId = b, Classification = "X" },
                new RequirementSnapshot { NodeId = a, Classification = "X" }
            ]
        };
        var r = _calc.Calculate(input);
        Assert.Equal(80m, r.OverallPercent);
    }
}
