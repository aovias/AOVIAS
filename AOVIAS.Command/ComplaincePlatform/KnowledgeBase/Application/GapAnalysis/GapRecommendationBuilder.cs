using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.GapAnalysis;

public interface IGapRecommendationBuilder
{
    string Build(KnowledgeItem? activeKnowledge, GapCategory category);
}

public sealed class GapRecommendationBuilder : IGapRecommendationBuilder
{
    public string Build(KnowledgeItem? activeKnowledge, GapCategory category)
    {
        if (category == GapCategory.Compliant)
            return string.Empty;

        if (activeKnowledge is null)
            return "Review requirement and establish corrective actions to close this gap.";

        if (!string.IsNullOrWhiteSpace(activeKnowledge.HowToComply))
            return activeKnowledge.HowToComply.Trim();

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(activeKnowledge.EvidenceRequired))
            parts.Add($"Evidence: {activeKnowledge.EvidenceRequired.Trim()}");
        if (!string.IsNullOrWhiteSpace(activeKnowledge.AuditChecklist))
            parts.Add($"Checklist: {activeKnowledge.AuditChecklist.Trim()}");

        return parts.Count > 0
            ? string.Join(" ", parts)
            : "Review requirement and establish corrective actions to close this gap.";
    }
}
