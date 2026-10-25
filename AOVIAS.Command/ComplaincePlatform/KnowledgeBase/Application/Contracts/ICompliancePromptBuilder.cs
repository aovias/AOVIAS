using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface ICompliancePromptBuilder
{
    string BuildAskPrompt(
        Framework framework,
        ComplianceNode? node,
        IReadOnlyList<KnowledgeItem> knowledgeItems,
        IReadOnlyList<DecisionLog> priorDecisions,
        string userQuery);
}
