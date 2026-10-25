using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace Shared.Infrastructure.Auth;

/// <summary>
/// Adds X-Internal-Api-Key to outbound requests when configured.
/// </summary>
public sealed class InternalApiKeyForwardingHandler : DelegatingHandler
{
    private readonly string? _apiKey;

    public InternalApiKeyForwardingHandler(IConfiguration configuration)
    {
        _apiKey = configuration["InternalApi:ApiKey"];
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_apiKey)
            && !request.Headers.Contains("X-Internal-Api-Key"))
        {
            request.Headers.Add("X-Internal-Api-Key", _apiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
