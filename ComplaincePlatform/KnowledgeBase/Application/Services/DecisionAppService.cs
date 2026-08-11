using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Services;

public sealed class DecisionAppService
{
    private readonly IAIRepository _ai;
    private readonly IDecisionRepository _decisions;

    public DecisionAppService(IAIRepository ai, IDecisionRepository decisions)
    {
        _ai = ai;
        _decisions = decisions;
    }

    public async Task<DecisionLogDto> CreateAsync(CreateDecisionRequestDto dto, CancellationToken cancellationToken = default)
    {
        var interaction = await _ai.GetByIdAsync(dto.AIInteractionId, cancellationToken)
            ?? throw new InvalidOperationException("AI interaction not found.");

        var log = new DecisionLog
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            FrameworkId = dto.FrameworkId,
            NodeId = dto.NodeId,
            AIInteractionId = interaction.Id,
            ProblemStatement = dto.ProblemStatement,
            SuggestedAction = dto.SuggestedAction,
            FinalDecision = dto.FinalDecision,
            Outcome = dto.Outcome,
            ConfidenceScore = dto.ConfidenceScore,
            CreatedAt = DateTime.UtcNow
        };

        await _decisions.AddAsync(log, cancellationToken);
        await _decisions.SaveChangesAsync(cancellationToken);

        return ToDto(log);
    }

    public async Task<IReadOnlyList<DecisionLogDto>> ListByNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var list = await _decisions.ListByNodeAsync(nodeId, cancellationToken);
        return list.Select(ToDto).ToList();
    }

    private static DecisionLogDto ToDto(DecisionLog d) =>
        new(d.Id, d.UserId, d.FrameworkId, d.NodeId, d.AIInteractionId,
            d.ProblemStatement, d.SuggestedAction, d.FinalDecision, d.Outcome,
            d.ConfidenceScore, d.CreatedAt);
}
