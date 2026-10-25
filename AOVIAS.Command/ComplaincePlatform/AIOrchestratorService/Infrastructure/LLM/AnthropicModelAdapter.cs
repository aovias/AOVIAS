using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Infrastructure.LLM;

/// <summary>
/// Stub Anthropic adapter. Registered for DI but only selected when
/// <c>AIProvider</c> / <c>AIProviderRouting:AIProvider</c> is set to <c>Anthropic</c>.
/// Returns a deterministic fallback (does not call external APIs in this phase).
/// </summary>
public sealed class AnthropicModelAdapter : IAIModelAdapter
{
    private readonly ILogger<AnthropicModelAdapter> _logger;

    public AnthropicModelAdapter(ILogger<AnthropicModelAdapter> logger)
    {
        _logger = logger;
    }

    public Task<AIModelResponse> GenerateAsync(AIModelRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogWarning(
            "AnthropicModelAdapter is a Phase-5 stub; returning deterministic fallback for useCase={UseCase} tenant={Tenant}",
            request.UseCase,
            request.TenantKey);

        return Task.FromResult(new AIModelResponse
        {
            Hint = "AI advisory fallback: Anthropic provider is registered but not fully implemented. Use AzureOpenAI or Sarvam, or review deterministic compliance dashboards.",
            Confidence = 0.3m,
            ReasoningBullets = new List<string>
            {
                "Anthropic adapter stub — no external Claude API call was made.",
                "Set AIProvider=AzureOpenAI or Sarvam for production inference."
            }
        });
    }
}
