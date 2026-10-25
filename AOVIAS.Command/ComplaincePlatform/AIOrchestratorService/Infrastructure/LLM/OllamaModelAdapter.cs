using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Infrastructure.LLM;

/// <summary>
/// Stub Ollama (local) adapter. Registered for DI but only selected when
/// <c>AIProvider</c> / <c>AIProviderRouting:AIProvider</c> is set to <c>Ollama</c>.
/// Throws <see cref="NotSupportedException"/> unless <c>AllowDeterministicFallback</c> path is used
/// via the routing adapter (routing catches and returns fallback).
/// </summary>
public sealed class OllamaModelAdapter : IAIModelAdapter
{
    private readonly ILogger<OllamaModelAdapter> _logger;
    private readonly bool _returnDeterministicFallback;

    public OllamaModelAdapter(ILogger<OllamaModelAdapter> logger)
        : this(logger, returnDeterministicFallback: true)
    {
    }

    internal OllamaModelAdapter(ILogger<OllamaModelAdapter> logger, bool returnDeterministicFallback)
    {
        _logger = logger;
        _returnDeterministicFallback = returnDeterministicFallback;
    }

    public Task<AIModelResponse> GenerateAsync(AIModelRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogWarning(
            "OllamaModelAdapter is a Phase-5 stub for useCase={UseCase} tenant={Tenant}",
            request.UseCase,
            request.TenantKey);

        if (!_returnDeterministicFallback)
        {
            throw new NotSupportedException(
                "Ollama local inference is not enabled in this build. Configure AIProvider=AzureOpenAI or Sarvam.");
        }

        return Task.FromResult(new AIModelResponse
        {
            Hint = "AI advisory fallback: Ollama provider is registered but not fully implemented. Use AzureOpenAI or Sarvam for hosted inference.",
            Confidence = 0.3m,
            ReasoningBullets = new List<string>
            {
                "Ollama adapter stub — no local model process was invoked.",
                "Set AIProvider=AzureOpenAI or Sarvam for production inference."
            }
        });
    }
}
