using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Application.AI.Validation;

public interface IAIMediator
{
    AIResponse Mediate(AIRequest request, AIResponse response);
}

public sealed class AIMediator : IAIMediator
{
    private readonly IAIResponseValidator _validator;

    public AIMediator(IAIResponseValidator validator)
    {
        _validator = validator;
    }

    public AIResponse Mediate(AIRequest request, AIResponse response)
    {
        // Normalize minimal defaults before validation.
        response.Reasoning ??= new List<AIReasoningStep>();
        response.SourceReferences ??= new List<AISourceReference>();
        response.ComplianceHint = response.ComplianceHint?.Trim() ?? string.Empty;
        return _validator.ValidateAndNormalize(request, response);
    }
}
