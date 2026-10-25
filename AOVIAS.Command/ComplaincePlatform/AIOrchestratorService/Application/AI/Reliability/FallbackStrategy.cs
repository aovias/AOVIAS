using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Inference;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Reliability;

public interface IFallbackStrategy
{
    bool ShouldFallback(ProviderFailureException failure);
}

public sealed class FallbackStrategy : IFallbackStrategy
{
    private readonly IOptionsMonitor<SarvamOptions> _sarvamOptions;
    private readonly IOptionsMonitor<AzureOpenAIOptions> _azureOptions;

    public FallbackStrategy(
        IOptionsMonitor<SarvamOptions> sarvamOptions,
        IOptionsMonitor<AzureOpenAIOptions> azureOptions)
    {
        _sarvamOptions = sarvamOptions;
        _azureOptions = azureOptions;
    }

    public bool ShouldFallback(ProviderFailureException failure)
    {
        if (!_sarvamOptions.CurrentValue.EnableFallbackToAzure)
        {
            return false;
        }

        var azure = _azureOptions.CurrentValue;
        var azureConfigured = !string.IsNullOrWhiteSpace(azure.Endpoint)
            && !string.IsNullOrWhiteSpace(azure.ApiKey)
            && !string.IsNullOrWhiteSpace(azure.DefaultDeployment);
        if (!azureConfigured)
        {
            return false;
        }

        return failure.Reason is ProviderFailureReason.InvalidJson
            or ProviderFailureReason.Timeout
            or ProviderFailureReason.RateLimited
            or ProviderFailureReason.ServerError
            or ProviderFailureReason.TransportError;
    }
}
