using KnowledgeBase.Application.Contracts;

namespace KnowledgeBase.Application.AssessmentSummary;

public sealed class AssessmentSummaryCalculator : IAssessmentSummaryCalculator
{
    private const string ChapterType = "Chapter";
    private const string StandardType = "Standard";

    public AssessmentSummaryResult Calculate(AssessmentSummaryInput input)
    {
        var nodeById = input.Nodes.ToDictionary(n => n.Id);
        var reqByNode = input.Requirements.ToDictionary(r => r.NodeId, r => r.Classification);

        if (input.Items.Count == 0)
        {
            return new AssessmentSummaryResult
            {
                OverallPercent = 0,
                MeetsOverallPercent = false,
                CoreRulePassed = true,
                ChapterAveragesPassed = true,
                StandardAveragesPassed = true,
                CommitmentLevelPassed = true
            };
        }

        var overallPercent = input.Items.Average(i => (decimal)i.Score / 5m * 100m);
        var meetsOverall = overallPercent >= input.MinimumOverallPercent;

        var coreFailures = new List<Guid>();
        foreach (var item in input.Items)
        {
            if (!reqByNode.TryGetValue(item.NodeId, out var cls))
                continue;
            if (!string.Equals(cls, input.CoreClassificationName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (item.Score < 4)
                coreFailures.Add(item.NodeId);
        }

        var chapterAverages = AverageByAncestorType(input.Items, nodeById, ChapterType);
        var standardAverages = AverageByAncestorType(input.Items, nodeById, StandardType);

        var chapterPassed = chapterAverages.Count == 0 || chapterAverages.Values.All(a => a >= input.MinimumAverageScore);
        var standardPassed = standardAverages.Count == 0 || standardAverages.Values.All(a => a >= input.MinimumAverageScore);

        var commitmentItems = input.Items
            .Where(i => reqByNode.TryGetValue(i.NodeId, out var c) &&
                        string.Equals(c, input.CommitmentClassificationName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var commitmentPassed = commitmentItems.Count == 0 ||
            commitmentItems.Average(i => (decimal)i.Score / 5m * 100m) >= 80m;

        var lowByStandard = CountLowScoresPerStandard(input.Items, nodeById);
        var tooMany = lowByStandard.Values.Any(c => c > 1);
        var standardsWithMultiple = lowByStandard.Where(kv => kv.Value > 1).Select(kv => kv.Key).ToList();

        var actionViolations = input.Items
            .Where(i => i.Score <= 3 && !i.HasActionPlan)
            .Select(i => i.NodeId)
            .ToList();

        return new AssessmentSummaryResult
        {
            OverallPercent = decimal.Round(overallPercent, 2),
            MeetsOverallPercent = meetsOverall,
            CoreRulePassed = coreFailures.Count == 0,
            CoreFailureNodeIds = coreFailures,
            ChapterAverages = chapterAverages,
            StandardAverages = standardAverages,
            ChapterAveragesPassed = chapterPassed,
            StandardAveragesPassed = standardPassed,
            CommitmentLevelPassed = commitmentPassed,
            TooManyLowScoresInStandard = tooMany,
            StandardsWithMultipleLowScores = standardsWithMultiple,
            ActionPlanViolationNodeIds = actionViolations
        };
    }

    private static Dictionary<Guid, decimal> AverageByAncestorType(
        IReadOnlyList<AssessmentItemSnapshot> items,
        IReadOnlyDictionary<Guid, HierarchyNode> nodeById,
        string ancestorType)
    {
        var groups = new Dictionary<Guid, List<int>>();
        foreach (var item in items)
        {
            var anc = FindAncestorOfType(item.NodeId, nodeById, ancestorType);
            if (anc is null)
                continue;
            if (!groups.TryGetValue(anc.Value, out var list))
            {
                list = new List<int>();
                groups[anc.Value] = list;
            }
            list.Add(item.Score);
        }

        return groups.ToDictionary(
            g => g.Key,
            g => g.Value.Count == 0 ? 0m : decimal.Round(g.Value.Average(x => (decimal)x), 2));
    }

    private static Guid? FindAncestorOfType(
        Guid nodeId,
        IReadOnlyDictionary<Guid, HierarchyNode> nodeById,
        string ancestorType)
    {
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

    private static Dictionary<Guid, int> CountLowScoresPerStandard(
        IReadOnlyList<AssessmentItemSnapshot> items,
        IReadOnlyDictionary<Guid, HierarchyNode> nodeById)
    {
        var result = new Dictionary<Guid, int>();
        foreach (var item in items.Where(i => i.Score <= 2))
        {
            var std = FindAncestorOfType(item.NodeId, nodeById, StandardType);
            if (std is null)
                continue;
            result[std.Value] = result.GetValueOrDefault(std.Value) + 1;
        }

        return result;
    }
}
