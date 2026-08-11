using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Reliability;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Infrastructure.LLM;

public sealed class ProviderRoutingModelAdapter : IAIModelAdapter
{
    private readonly SarvamModelAdapter _sarvam;
    private readonly AzureOpenAIModelAdapter _azure;
    private readonly AnthropicModelAdapter _anthropic;
    private readonly OllamaModelAdapter _ollama;
    private readonly IFallbackStrategy _fallbackStrategy;
    private readonly IOptionsMonitor<AIProviderRoutingOptions> _routingOptions;
    private readonly IMetricsTracker _metrics;
    private readonly ILogger<ProviderRoutingModelAdapter> _logger;

    public ProviderRoutingModelAdapter(
        SarvamModelAdapter sarvam,
        AzureOpenAIModelAdapter azure,
        AnthropicModelAdapter anthropic,
        OllamaModelAdapter ollama,
        IFallbackStrategy fallbackStrategy,
        IOptionsMonitor<AIProviderRoutingOptions> routingOptions,
        IMetricsTracker metrics,
        ILogger<ProviderRoutingModelAdapter> logger)
    {
        _sarvam = sarvam;
        _azure = azure;
        _anthropic = anthropic;
        _ollama = ollama;
        _fallbackStrategy = fallbackStrategy;
        _routingOptions = routingOptions;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<AIModelResponse> GenerateAsync(AIModelRequest request, CancellationToken cancellationToken)
    {
        var routing = _routingOptions.CurrentValue;
        var provider = (routing.AIProvider ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(provider) || provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return await _azure.GenerateAsync(request, cancellationToken);
        }

        if (provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Routing to Anthropic stub adapter for useCase={UseCase}", request.UseCase);
            return await _anthropic.GenerateAsync(request, cancellationToken);
        }

        if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Routing to Ollama stub adapter for useCase={UseCase}", request.UseCase);
            try
            {
                return await _ollama.GenerateAsync(request, cancellationToken);
            }
            catch (NotSupportedException ex)
            {
                _metrics.TrackFallback("Ollama", "AzureOpenAI", "NotSupported", request.UseCase.ToString(), request.TenantKey, request.PromptTemplateVersion);
                _logger.LogWarning(ex, "Ollama stub not supported; falling back to AzureOpenAI.");
                return await _azure.GenerateAsync(request, cancellationToken);
            }
        }

        if (!provider.Equals("Sarvam", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Unknown AI provider {Provider}, defaulting to AzureOpenAI.", provider);
            return await _azure.GenerateAsync(request, cancellationToken);
        }

        var allowedUseCases = routing.D1AllowedUseCases ?? new List<string>();
        var useCase = request.UseCase.ToString();
        if (allowedUseCases.Count > 0 &&
            !allowedUseCases.Contains(useCase, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Use case {UseCase} is outside Sarvam D1 scope, using AzureOpenAI.",
                useCase);
            return await _azure.GenerateAsync(request, cancellationToken);
        }

        try
        {
            return await _sarvam.GenerateAsync(request, cancellationToken);
        }
        catch (ProviderFailureException failure) when (_fallbackStrategy.ShouldFallback(failure))
        {
            _metrics.TrackFallback("Sarvam", "AzureOpenAI", failure.Reason.ToString(), useCase, request.TenantKey, request.PromptTemplateVersion);
            _logger.LogWarning(
                failure,
                "Sarvam failed with reason {Reason}. Falling back to AzureOpenAI.",
                failure.Reason);
            return await _azure.GenerateAsync(request, cancellationToken);
        }
    }
}
