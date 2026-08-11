using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.AssessmentSummary;

public sealed class AssessmentEvaluationContext
{
    private const string ChapterType = "Chapter";
    private const string StandardType = "Standard";

    public required Assessment Assessment { get; init; }
    public IReadOnlyList<HierarchyNode> Hierarchy { get; init; } = Array.Empty<HierarchyNode>();
    public IReadOnlyList<RequirementSnapshot> Requirements { get; init; } = Array.Empty<RequirementSnapshot>();
    public IReadOnlyDictionary<Guid, ComplianceNode> NodeById { get; init; } =
        new Dictionary<Guid, ComplianceNode>();

    public AssessmentSummaryInput ToSummaryInput() =>
        new()
        {
            Nodes = Hierarchy,
            Items = Assessment.Items.Select(i => new AssessmentItemSnapshot
            {
                NodeId = i.NodeId,
                Score = i.Score,
                HasActionPlan = i.HasActionPlan
            }).ToList(),
            Requirements = Requirements
        };

    public Guid? FindAncestorOfType(Guid nodeId, string ancestorType)
    {
        var nodeById = Hierarchy.ToDictionary(n => n.Id);
        var current = nodeId;
        while (nodeById.TryGetValue(current, out var n))
        {
            if (string.Equals(n.NodeType, ancestorType, StringComparison.OrdinalIgnoreCase))
                return n.Id;
            if (n.ParentId is null)
                return null;
            current = n.ParentId.Value;
        }

        return null;
    }

    public Guid? FindChapterId(Guid nodeId) => FindAncestorOfType(nodeId, ChapterType);

    public Guid? FindStandardId(Guid nodeId) => FindAncestorOfType(nodeId, StandardType);

    public ComplianceNode? GetNode(Guid nodeId) =>
        NodeById.TryGetValue(nodeId, out var node) ? node : null;
}
