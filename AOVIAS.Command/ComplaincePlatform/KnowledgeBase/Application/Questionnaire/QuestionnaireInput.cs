using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Questionnaire;

public sealed class QuestionnaireInput
{
    public Guid FrameworkId { get; init; }
    public string FrameworkName { get; init; } = string.Empty;
    public IReadOnlyList<ComplianceNode> Nodes { get; init; } = Array.Empty<ComplianceNode>();
}
