using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Shared.Infrastructure.Caching;

namespace ComplianceService.Application.Dashboard.Summary;

/// <summary>
/// Scoped-key cache with version-bump invalidation and 60s TTL.
/// Uses <see cref="IDistributedCache"/> when Redis is configured; otherwise <see cref="IMemoryCache"/>.
/// </summary>
public sealed class DashboardSummaryCache : IDashboardSummaryCache
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly bool _useDistributed;
    private readonly ConcurrentDictionary<string, long> _keyVersions = new();

    public DashboardSummaryCache(
        IMemoryCache memoryCache,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _memoryCache = memoryCache;
        _useDistributed = RedisConfiguration.IsConfigured(configuration);
        _distributedCache = _useDistributed
            ? serviceProvider.GetService<IDistributedCache>()
            : null;
    }

    public async Task<DashboardSummaryPayload?> TryGetAsync(
        Guid tenantId,
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken = default)
    {
        var scopeKey = BuildScopeKey(tenantId, hospitalId, frameworkId);
        var version = _keyVersions.GetOrAdd(scopeKey, 1);
        var cacheKey = $"{scopeKey}:v:{version}";

        if (_useDistributed && _distributedCache != null)
        {
            var bytes = await _distributedCache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            if (bytes is { Length: > 0 })
            {
                var entry = JsonSerializer.Deserialize<CachedEntry>(bytes, JsonOptions);
                return entry?.Payload;
            }

            return null;
        }

        if (_memoryCache.TryGetValue(cacheKey, out CachedEntry? memEntry) && memEntry is not null)
        {
            return memEntry.Payload;
        }

        return null;
    }

    public async Task SetAsync(
        Guid tenantId,
        Guid? hospitalId,
        Guid? frameworkId,
        DashboardSummaryPayload payload,
        CancellationToken cancellationToken = default)
    {
        var scopeKey = BuildScopeKey(tenantId, hospitalId, frameworkId);
        var version = _keyVersions.GetOrAdd(scopeKey, 1);
        var cacheKey = $"{scopeKey}:v:{version}";
        var entry = new CachedEntry(version, payload);

        if (_useDistributed && _distributedCache != null)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(entry, JsonOptions);
            await _distributedCache.SetAsync(
                cacheKey,
                bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
                cancellationToken).ConfigureAwait(false);
            return;
        }

        _memoryCache.Set(
            cacheKey,
            entry,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
            });
    }

    public void Invalidate(Guid tenantId, Guid? hospitalId = null, Guid? frameworkId = null)
    {
        Bump(BuildScopeKey(tenantId, hospitalId, frameworkId));

        if (frameworkId.HasValue && frameworkId.Value != Guid.Empty)
        {
            Bump(BuildScopeKey(tenantId, hospitalId, null));
        }

        if (hospitalId.HasValue && hospitalId.Value != Guid.Empty)
        {
            Bump(BuildScopeKey(tenantId, null, null));
        }
    }

    private void Bump(string scopeKey) =>
        _keyVersions.AddOrUpdate(scopeKey, 2, (_, current) => current + 1);

    internal static string BuildScopeKey(Guid tenantId, Guid? hospitalId, Guid? frameworkId)
    {
        var hospitalSegment = hospitalId is { } hid && hid != Guid.Empty ? hid.ToString("N") : "all";
        var frameworkSegment = frameworkId is { } fid && fid != Guid.Empty ? fid.ToString("N") : "all";
        return $"dashboard-summary:{tenantId:N}:{hospitalSegment}:{frameworkSegment}";
    }

    private sealed record CachedEntry(long Version, DashboardSummaryPayload Payload);
}
