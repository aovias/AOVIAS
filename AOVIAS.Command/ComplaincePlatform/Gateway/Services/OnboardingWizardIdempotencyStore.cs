using System.Security.Claims;
using System.Text.Json;
using Gateway.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace Gateway.Services;

/// <summary>
/// In-memory idempotency for onboarding wizard (24h TTL). Suitable for single-node / dev; cluster deployments should use a distributed cache later.
/// </summary>
public sealed class OnboardingWizardIdempotencyStore : IOnboardingWizardIdempotencyStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);
    private readonly IMemoryCache _cache;
    private readonly ILogger<OnboardingWizardIdempotencyStore> _logger;

    public OnboardingWizardIdempotencyStore(IMemoryCache cache, ILogger<OnboardingWizardIdempotencyStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public string? BuildCacheKey(HttpContext http, OnboardWizardRequestDto request, string? idempotencyKeyHeader)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKeyHeader))
        {
            return null;
        }

        var userId =
            http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var trimmed = idempotencyKeyHeader.Trim();
        return $"onboard-wizard:{userId}:{request.OrganizationId:N}:{request.HospitalId:N}:{trimmed}";
    }

    public bool TryGetReplay(string cacheKey, out OnboardWizardResponseDto? response)
    {
        if (_cache.TryGetValue(cacheKey, out string? json) && !string.IsNullOrEmpty(json))
        {
            try
            {
                response = JsonSerializer.Deserialize<OnboardWizardResponseDto>(json, RehydrateJsonOptions);
                if (response != null)
                {
                    _logger.LogInformation("Onboarding wizard idempotent replay for cache key prefix {Prefix}", cacheKey[..Math.Min(48, cacheKey.Length)]);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached onboarding wizard response");
            }
        }

        response = null;
        return false;
    }

    public void StoreReplay(string cacheKey, OnboardWizardResponseDto response)
    {
        var json = JsonSerializer.Serialize(response, RehydrateJsonOptions);
        _cache.Set(cacheKey, json, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl });
    }

    private static readonly JsonSerializerOptions RehydrateJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
