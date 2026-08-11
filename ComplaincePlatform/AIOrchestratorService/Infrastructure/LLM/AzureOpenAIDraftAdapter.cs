using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.Services;

namespace AIOrchestratorService.Infrastructure.LLM;

public sealed class AzureOpenAIDraftAdapter : ILLMAdapter
{
    private readonly IAIModelAdapter _modelAdapter;

    public AzureOpenAIDraftAdapter(IAIModelAdapter modelAdapter)
    {
        _modelAdapter = modelAdapter;
    }

    public async Task<LLMResult> GenerateDraftAsync(DraftingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SourceEntityId == Guid.Empty)
        {
            return LLMResult.Failure("SourceEntityId is required");
        }

        var modelResponse = await _modelAdapter.GenerateAsync(new AIModelRequest
        {
            TenantKey = request.ComplianceProgram?.OrganizationId.ToString("N") ?? "default",
            SystemPrompt = "Generate safe compliance draft text. Advisory only.",
            Prompt = $"DraftType={request.DraftType}; SourceEntity={request.SourceEntityId}",
            Context = new AIOrchestratorService.Application.AI.Models.AIRetrievalContext
            {
                Facts = new Dictionary<string, string>
                {
                    ["draftType"] = request.DraftType.ToString(),
                    ["sourceEntityId"] = request.SourceEntityId.ToString()
                },
                ContextVersionId = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            }
        }, cancellationToken);

        return LLMResult.Success(modelResponse.Hint);
    }
}
