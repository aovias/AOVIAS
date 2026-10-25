using Microsoft.Extensions.Caching.Memory;

namespace ComplianceService.Infrastructure.Services;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string jti, DateTime expiry);
    Task<bool> IsTokenBlacklistedAsync(string jti);
    Task CleanupExpiredTokensAsync();
}

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenBlacklistService> _logger;
    private const string CacheKeyPrefix = "blacklisted_token_";

    public TokenBlacklistService(IMemoryCache cache, ILogger<TokenBlacklistService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task BlacklistTokenAsync(string jti, DateTime expiry)
    {
        var cacheKey = $"{CacheKeyPrefix}{jti}";
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiry,
            SlidingExpiration = null
        };

        _cache.Set(cacheKey, true, cacheOptions);
        _logger.LogInformation("Token {Jti} blacklisted until {Expiry}", jti, expiry);
        
        return Task.CompletedTask;
    }

    public Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        var cacheKey = $"{CacheKeyPrefix}{jti}";
        var isBlacklisted = _cache.TryGetValue(cacheKey, out _);
        return Task.FromResult(isBlacklisted);
    }

    public Task CleanupExpiredTokensAsync()
    {
        // MemoryCache automatically removes expired entries, so this is a no-op
        // In a distributed scenario, you might want to implement periodic cleanup
        return Task.CompletedTask;
    }
}
