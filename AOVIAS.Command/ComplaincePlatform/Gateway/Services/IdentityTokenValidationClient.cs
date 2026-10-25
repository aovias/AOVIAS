using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gateway.Services;

public sealed class IdentityTokenValidationClient
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IdentityTokenValidationClient> _logger;

    public IdentityTokenValidationClient(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<IdentityTokenValidationClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        Guid userId,
        string jti,
        int tokenVersion,
        string securityStamp,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"jwt-valid:{userId:N}:{jti}:{tokenVersion}:{securityStamp}";
        if (_cache.TryGetValue(cacheKey, out bool cached))
            return cached;

        try
        {
            var client = _httpClientFactory.CreateClient("IdentityClient");
            var response = await client.PostAsJsonAsync(
                "/internal/token/validate",
                new ValidateTokenPayload(userId, jti, tokenVersion, securityStamp),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Identity token validation returned {StatusCode}", response.StatusCode);
                return false;
            }

            var body = await response.Content.ReadFromJsonAsync<ValidateTokenResponse>(cancellationToken);
            var valid = body?.Valid ?? false;
            _cache.Set(cacheKey, valid, CacheTtl);
            return valid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Identity token validation call failed");
            return false;
        }
    }

    private sealed record ValidateTokenPayload(Guid UserId, string Jti, int TokenVersion, string? SecurityStamp);

    private sealed record ValidateTokenResponse(bool Valid);
}
