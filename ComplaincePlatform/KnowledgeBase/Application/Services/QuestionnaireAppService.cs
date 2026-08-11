using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Questionnaire;

namespace KnowledgeBase.Application.Services;

public sealed class QuestionnaireAppService
{
    private readonly IFrameworkRepository _frameworks;
    private readonly INodeRepository _nodes;
    private readonly IQuestionnaireGenerator _generator;

    public QuestionnaireAppService(
        IFrameworkRepository frameworks,
        INodeRepository nodes,
        IQuestionnaireGenerator generator)
    {
        _frameworks = frameworks;
        _nodes = nodes;
        _generator = generator;
    }

    public async Task<FrameworkQuestionnaireDto?> GetFrameworkQuestionnaireAsync(
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var framework = await _frameworks.GetByIdAsync(frameworkId, cancellationToken);
        if (framework is null)
            return null;

        var nodes = await _nodes.ListByFrameworkWithDetailsAsync(frameworkId, cancellationToken);
        return _generator.Generate(new QuestionnaireInput
        {
            FrameworkId = framework.Id,
            FrameworkName = framework.Name,
            Nodes = nodes
        });
    }
}
