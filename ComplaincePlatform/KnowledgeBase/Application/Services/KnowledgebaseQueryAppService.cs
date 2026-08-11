using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Services;

namespace KnowledgeBase.Application.Services;

public sealed class KnowledgebaseQueryAppService
{
    private readonly IFrameworkRepository _frameworks;
    private readonly INodeRepository _nodes;
    private readonly IKnowledgeRepository _knowledge;
    private readonly IScoringRepository _scoring;

    public KnowledgebaseQueryAppService(
        IFrameworkRepository frameworks,
        INodeRepository nodes,
        IKnowledgeRepository knowledge,
        IScoringRepository scoring)
    {
        _frameworks = frameworks;
        _nodes = nodes;
        _knowledge = knowledge;
        _scoring = scoring;
    }

    public async Task<IReadOnlyList<FrameworkDto>> ListFrameworksAsync(CancellationToken cancellationToken = default)
    {
        var list = await _frameworks.GetActiveAsync(cancellationToken);
        return list.Select(f => new FrameworkDto(f.Id, f.Code, f.Name, f.Version, f.Description, f.IsActive)).ToList();
    }

    public async Task<FrameworkDto?> GetFrameworkAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var f = await _frameworks.GetByIdAsync(id, cancellationToken);
        return f is null ? null : new FrameworkDto(f.Id, f.Code, f.Name, f.Version, f.Description, f.IsActive);
    }

    public async Task<IReadOnlyList<ComplianceNodeDto>> ListNodesAsync(Guid frameworkId, CancellationToken cancellationToken = default)
    {
        await EnsureFrameworkExistsAsync(frameworkId, cancellationToken);
        var nodes = await _nodes.ListByFrameworkAsync(frameworkId, cancellationToken);
        return OrderFlat(nodes).Select(MapNode).ToList();
    }

    public async Task<ComplianceNodeDto?> GetNodeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var n = await _nodes.GetByIdAsync(id, cancellationToken);
        return n is null ? null : MapNode(n);
    }

    public async Task<IReadOnlyList<ComplianceNodeDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var children = await _nodes.GetChildrenAsync(parentId, cancellationToken);
        var comparer = new ComplianceNodeSequenceComparer();
        return children.OrderBy(x => x, comparer).Select(MapNode).ToList();
    }

    public async Task<NodeDetailsDto?> GetNodeDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var node = await _nodes.GetWithDetailsAsync(id, cancellationToken);
        if (node is null)
            return null;

        var rs = await _scoring.GetRequirementScoreAsync(id, cancellationToken);
        return new NodeDetailsDto(
            MapNode(node),
            node.RequirementDetail is null ? null : MapReq(node.RequirementDetail),
            node.KnowledgeItems.Select(MapKnowledge).ToList(),
            rs is null ? null : new RequirementScoreDto(rs.Id, rs.NodeId, rs.MaxScore, rs.Weight, rs.ClassificationId));
    }

    public async Task<IReadOnlyList<KnowledgeItemDto>> GetKnowledgeForNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var items = await _knowledge.GetByNodeAsync(nodeId, cancellationToken);
        return items.Select(MapKnowledge).ToList();
    }

    public async Task<KnowledgeByNodeCodeResult> GetKnowledgeForNodeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var nodes = await _nodes.FindNodesByCodeAsync(code, cancellationToken);
        if (nodes.Count == 0)
            return new KnowledgeByNodeCodeResult(KnowledgeByNodeCodeStatus.NotFound, null);
        if (nodes.Count > 1)
            return new KnowledgeByNodeCodeResult(KnowledgeByNodeCodeStatus.Ambiguous, null);

        var items = await GetKnowledgeForNodeAsync(nodes[0].Id, cancellationToken);
        return new KnowledgeByNodeCodeResult(KnowledgeByNodeCodeStatus.Ok, items);
    }

    public async Task<NodeDetailsByCodeResult> GetNodeDetailsByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var nodes = await _nodes.FindNodesByCodeAsync(code, cancellationToken);
        if (nodes.Count == 0)
            return new NodeDetailsByCodeResult(KnowledgeByNodeCodeStatus.NotFound, null);
        if (nodes.Count > 1)
            return new NodeDetailsByCodeResult(KnowledgeByNodeCodeStatus.Ambiguous, null);

        var details = await GetNodeDetailsAsync(nodes[0].Id, cancellationToken);
        if (details is null)
            return new NodeDetailsByCodeResult(KnowledgeByNodeCodeStatus.NotFound, null);

        return new NodeDetailsByCodeResult(KnowledgeByNodeCodeStatus.Ok, details);
    }

    public async Task<IReadOnlyList<KnowledgeItemDto>> SearchKnowledgeAsync(string? q, Guid? frameworkId, CancellationToken cancellationToken = default)
    {
        if (frameworkId.HasValue && frameworkId.Value != Guid.Empty)
        {
            await EnsureFrameworkExistsAsync(frameworkId.Value, cancellationToken);
        }

        var items = await _knowledge.SearchAsync(q, frameworkId, cancellationToken);
        return items.Select(MapKnowledge).ToList();
    }

    private async Task EnsureFrameworkExistsAsync(Guid frameworkId, CancellationToken cancellationToken)
    {
        var framework = await _frameworks.GetByIdAsync(frameworkId, cancellationToken);
        if (framework is null)
        {
            throw new ArgumentException("FrameworkId is invalid or non-canonical.", nameof(frameworkId));
        }
    }

    private static IEnumerable<ComplianceNode> OrderFlat(IReadOnlyList<ComplianceNode> nodes)
    {
        var byParent = new Dictionary<Guid, List<ComplianceNode>>();
        foreach (var n in nodes)
        {
            var parentKey = n.ParentId ?? Guid.Empty;
            if (!byParent.TryGetValue(parentKey, out var list))
            {
                list = new List<ComplianceNode>();
                byParent[parentKey] = list;
            }

            list.Add(n);
        }

        foreach (var list in byParent.Values)
            list.Sort(new ComplianceNodeSequenceComparer());

        var roots = byParent.GetValueOrDefault(Guid.Empty) ?? new List<ComplianceNode>();
        foreach (var r in roots)
        {
            foreach (var x in DepthFirst(r, byParent))
                yield return x;
        }
    }

    private static IEnumerable<ComplianceNode> DepthFirst(
        ComplianceNode node,
        IReadOnlyDictionary<Guid, List<ComplianceNode>> byParent)
    {
        yield return node;
        if (!byParent.TryGetValue(node.Id, out var kids))
            yield break;
        foreach (var k in kids)
        {
            foreach (var x in DepthFirst(k, byParent))
                yield return x;
        }
    }

    private static ComplianceNodeDto MapNode(ComplianceNode n) =>
        new(n.Id, n.FrameworkId, n.ParentId, n.Level, n.Code, n.Title, n.Description, n.NodeType, n.Sequence);

    private static RequirementDetailDto MapReq(RequirementDetail r) =>
        new(r.Id, r.NodeId, r.Intent, r.MeasurableElements, r.Classification, r.IsMandatory, r.ApplicableDepartments, r.RequiresDocumentation);

    private static KnowledgeItemDto MapKnowledge(KnowledgeItem k) =>
        new(k.Id, k.NodeId, k.Explanation, k.WhyItMatters, k.HowToComply, k.EvidenceRequired, k.AuditChecklist,
            k.RiskLevel, k.ImpactLevel, k.Tags, k.Version, k.IsActive);
}
