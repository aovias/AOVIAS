using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Services;

public sealed class CompliancePromptBuilder : ICompliancePromptBuilder
{
    public string BuildAskPrompt(
        Framework framework,
        ComplianceNode? node,
        IReadOnlyList<KnowledgeItem> knowledgeItems,
        IReadOnlyList<DecisionLog> priorDecisions,
        string userQuery)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are a healthcare compliance expert.");
        sb.AppendLine($"Framework: {framework.Name} ({framework.Code}) version {framework.Version}.");
        if (node is not null)
        {
            sb.AppendLine($"Node context: [{node.Code}] {node.Title}");
            sb.AppendLine($"Node description: {node.Description}");
        }

        sb.AppendLine("Knowledge:");
        foreach (var k in knowledgeItems.Where(x => x.IsActive))
        {
            sb.AppendLine($"- {k.Explanation}");
            if (!string.IsNullOrWhiteSpace(k.HowToComply))
                sb.AppendLine($"  How to comply: {k.HowToComply}");
        }

        sb.AppendLine("Past decisions on this node:");
        foreach (var d in priorDecisions.OrderByDescending(x => x.CreatedAt).Take(10))
        {
            sb.AppendLine($"- Final: {d.FinalDecision} | Suggested: {d.SuggestedAction} | Outcome: {d.Outcome}");
        }

        sb.AppendLine("User question:");
        sb.AppendLine(userQuery);
        return sb.ToString();
    }
}
