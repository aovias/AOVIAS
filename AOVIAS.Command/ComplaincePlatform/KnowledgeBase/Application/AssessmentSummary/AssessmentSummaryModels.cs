namespace KnowledgeBase.Application.AssessmentSummary;

public sealed class AssessmentSummaryInput
{
    public IReadOnlyList<HierarchyNode> Nodes { get; init; } = Array.Empty<HierarchyNode>();
    public IReadOnlyList<AssessmentItemSnapshot> Items { get; init; } = Array.Empty<AssessmentItemSnapshot>();
    public IReadOnlyList<RequirementSnapshot> Requirements { get; init; } = Array.Empty<RequirementSnapshot>();
    public string CoreClassificationName { get; init; } = "CORE";
    public string CommitmentClassificationName { get; init; } = "Commitment";
    public decimal MinimumOverallPercent { get; init; } = 80m;
    public decimal MinimumAverageScore { get; init; } = 4m;
}

public sealed class HierarchyNode
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string NodeType { get; init; } = string.Empty;
}

public sealed class AssessmentItemSnapshot
{
    public Guid NodeId { get; init; }
    public int Score { get; init; }
    public bool HasActionPlan { get; init; }
}

public sealed class RequirementSnapshot
{
    public Guid NodeId { get; init; }
    public string Classification { get; init; } = string.Empty;
}

public sealed class AssessmentSummaryResult
{
    public decimal OverallPercent { get; init; }
    public bool MeetsOverallPercent { get; init; }
    public bool CoreRulePassed { get; init; }
    public IReadOnlyList<Guid> CoreFailureNodeIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyDictionary<Guid, decimal> ChapterAverages { get; init; } = new Dictionary<Guid, decimal>();
    public IReadOnlyDictionary<Guid, decimal> StandardAverages { get; init; } = new Dictionary<Guid, decimal>();
    public bool ChapterAveragesPassed { get; init; }
    public bool StandardAveragesPassed { get; init; }
    public bool CommitmentLevelPassed { get; init; }
    public bool TooManyLowScoresInStandard { get; init; }
    public IReadOnlyList<Guid> StandardsWithMultipleLowScores { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> ActionPlanViolationNodeIds { get; init; } = Array.Empty<Guid>();
}
