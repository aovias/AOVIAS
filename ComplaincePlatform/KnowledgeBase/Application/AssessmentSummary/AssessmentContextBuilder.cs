using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.AssessmentSummary;

public sealed class AssessmentContextBuilder
{
    private readonly IAssessmentRepository _assessments;
    private readonly INodeRepository _nodes;
    private readonly IRequirementRepository _requirements;

    public AssessmentContextBuilder(
        IAssessmentRepository assessments,
        INodeRepository nodes,
        IRequirementRepository requirements)
    {
        _assessments = assessments;
        _nodes = nodes;
        _requirements = requirements;
    }

    public async Task<AssessmentEvaluationContext> BuildAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await _assessments.GetByIdWithItemsAsync(assessmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assessment not found.");

        var nodes = await _nodes.ListByFrameworkAsync(assessment.FrameworkId, cancellationToken);
        var hierarchy = nodes.Select(n => new HierarchyNode
        {
            Id = n.Id,
            ParentId = n.ParentId,
            NodeType = n.NodeType
        }).ToList();

        var requirements = new List<RequirementSnapshot>();
        foreach (var item in assessment.Items)
        {
            var r = await _requirements.GetByNodeAsync(item.NodeId, cancellationToken);
            requirements.Add(new RequirementSnapshot
            {
                NodeId = item.NodeId,
                Classification = r?.Classification ?? string.Empty
            });
        }

        return new AssessmentEvaluationContext
        {
            Assessment = assessment,
            Hierarchy = hierarchy,
            Requirements = requirements,
            NodeById = nodes.ToDictionary(n => n.Id)
        };
    }
}
