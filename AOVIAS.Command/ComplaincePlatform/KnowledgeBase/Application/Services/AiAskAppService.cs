using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using System.Linq;

namespace KnowledgeBase.Application.Services;

public sealed class AiAskAppService
{
    private readonly IFrameworkRepository _frameworks;
    private readonly INodeRepository _nodes;
    private readonly IKnowledgeRepository _knowledge;
    private readonly IDecisionRepository _decisions;
    private readonly IAIRepository _ai;
    private readonly ICompliancePromptBuilder _promptBuilder;
    private readonly IAIService _aiService;

    public AiAskAppService(
        IFrameworkRepository frameworks,
        INodeRepository nodes,
        IKnowledgeRepository knowledge,
        IDecisionRepository decisions,
        IAIRepository ai,
        ICompliancePromptBuilder promptBuilder,
        IAIService aiService)
    {
        _frameworks = frameworks;
        _nodes = nodes;
        _knowledge = knowledge;
        _decisions = decisions;
        _ai = ai;
        _promptBuilder = promptBuilder;
        _aiService = aiService;
    }

    public async Task<AiAskResponseDto> AskAsync(AiAskRequestDto request, CancellationToken cancellationToken = default)
    {
        var framework = await _frameworks.GetByIdAsync(request.FrameworkId, cancellationToken)
            ?? throw new ArgumentException("FrameworkId is invalid or non-canonical.", nameof(request.FrameworkId));

        ComplianceNode? node = null;
        IReadOnlyList<KnowledgeItem> knowledge;
        IReadOnlyList<DecisionLog> decisions;

        if (request.NodeId is not null)
        {
            node = await _nodes.GetByIdAsync(request.NodeId.Value, cancellationToken);
            if (node is null)
            {
                throw new ArgumentException("NodeId does not exist.", nameof(request.NodeId));
            }

            if (node.FrameworkId != request.FrameworkId)
            {
                throw new ArgumentException(
                    "NodeId does not belong to the provided FrameworkId.",
                    nameof(request.NodeId));
            }

            knowledge = await _knowledge.GetByNodeAsync(request.NodeId.Value, cancellationToken);
            decisions = await _decisions.ListByNodeAsync(request.NodeId.Value, cancellationToken);
        }
        else
        {
            knowledge = await _knowledge.SearchAsync(request.Query, request.FrameworkId, cancellationToken);
            decisions = Array.Empty<DecisionLog>();
            node = knowledge.FirstOrDefault()?.Node;
        }

        var prompt = _promptBuilder.BuildAskPrompt(framework, node, knowledge, decisions, request.Query);
        var completion = await _aiService.CompleteAsync(prompt, cancellationToken);

        var interaction = new AIInteraction
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FrameworkId = request.FrameworkId,
            ContextNodeId = request.NodeId,
            Prompt = prompt,
            Response = completion.Text,
            ModelUsed = completion.ModelUsed,
            TokensUsed = completion.TokensUsed,
            CreatedAt = DateTime.UtcNow
        };

        await _ai.AddAsync(interaction, cancellationToken);
        await _ai.SaveChangesAsync(cancellationToken);

        return new AiAskResponseDto(interaction.Id, completion.Text, completion.ModelUsed, completion.TokensUsed);
    }
}
