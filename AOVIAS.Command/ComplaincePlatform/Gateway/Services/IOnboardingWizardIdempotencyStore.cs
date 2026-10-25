using Gateway.DTOs;

namespace Gateway.Services;

/// <summary>
/// Optional idempotent replay for POST /onboarding/onboard (IMemoryCache, Phase 22.5).
/// </summary>
public interface IOnboardingWizardIdempotencyStore
{
    /// <summary>
    /// Builds cache key from caller identity and idempotency header, or null if header missing/invalid.
    /// </summary>
    string? BuildCacheKey(HttpContext http, OnboardWizardRequestDto request, string? idempotencyKeyHeader);

    bool TryGetReplay(string cacheKey, out OnboardWizardResponseDto? response);

    void StoreReplay(string cacheKey, OnboardWizardResponseDto response);
}
