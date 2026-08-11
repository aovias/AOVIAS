using KnowledgeBase.Application.AssessmentSummary;
using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.GapAnalysis;

public sealed class GapAnalysisOrchestrator
{
    private readonly IGapClassifier _classifier;
    private readonly IGapRecommendationBuilder _recommendations;
    private readonly IKnowledgeRepository _knowledge;

    public GapAnalysisOrchestrator(
        IGapClassifier classifier,
        IGapRecommendationBuilder recommendations,
        IKnowledgeRepository knowledge)
    {
        _classifier = classifier;
        _recommendations = recommendations;
        _knowledge = knowledge;
    }

    public async Task<IReadOnlyList<GapAnalysisItem>> BuildItemsAsync(
        AssessmentEvaluationContext context,
        AssessmentSummaryResult summary,
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var reqByNode = context.Requirements.ToDictionary(r => r.NodeId);
        var items = new List<GapAnalysisItem>();

        foreach (var assessmentItem in context.Assessment.Items)
        {
            var snapshot = new AssessmentItemSnapshot
            {
                NodeId = assessmentItem.NodeId,
                Score = assessmentItem.Score,
                HasActionPlan = assessmentItem.HasActionPlan
            };

            reqByNode.TryGetValue(assessmentItem.NodeId, out var reqSnapshot);
            reqSnapshot ??= new RequirementSnapshot { NodeId = assessmentItem.NodeId, Classification = string.Empty };

            var standardId = context.FindStandardId(assessmentItem.NodeId);
            var category = _classifier.Classify(snapshot, reqSnapshot, summary, standardId);

            var knowledgeItems = await _knowledge.GetByNodeAsync(assessmentItem.NodeId, cancellationToken);
            var activeKnowledge = knowledgeItems
                .Where(k => k.IsActive)
                .OrderByDescending(k => k.Version)
                .FirstOrDefault();

            var node = context.GetNode(assessmentItem.NodeId);

            items.Add(new GapAnalysisItem
            {
                Id = Guid.NewGuid(),
                ReportId = reportId,
                NodeId = assessmentItem.NodeId,
                NodeCode = node?.Code ?? string.Empty,
                NodeTitle = node?.Title ?? string.Empty,
                ChapterId = context.FindChapterId(assessmentItem.NodeId),
                StandardId = standardId,
                Score = assessmentItem.Score,
                ComplianceStatus = assessmentItem.ComplianceStatus,
                Classification = reqSnapshot.Classification,
                Category = category.ToString(),
                Recommendation = _recommendations.Build(activeKnowledge, category)
            });
        }

        return items;
    }
}
