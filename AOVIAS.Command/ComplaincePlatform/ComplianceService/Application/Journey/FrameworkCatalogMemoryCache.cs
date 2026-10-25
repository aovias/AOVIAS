using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Shared.Infrastructure.Caching;

namespace ComplianceService.Application.Journey;

/// <summary>
/// Framework catalog cache. Uses <see cref="IDistributedCache"/> when Redis is configured;
/// falls back to <see cref="IMemoryCache"/> (and on distributed serialize/deserialize failure).
/// </summary>
public sealed class FrameworkCatalogMemoryCache : IFrameworkCatalogCache
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(45);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly bool _useDistributed;
    private readonly ConcurrentDictionary<Guid, long> _frameworkVersions = new();
    private long _globalVersion = 1;

    public FrameworkCatalogMemoryCache(
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

    public async Task<FrameworkCatalogSnapshot> GetOrLoadAsync(
        Guid frameworkId,
        string modeKey,
        Func<CancellationToken, Task<FrameworkCatalogSnapshot>> loader,
        CancellationToken cancellationToken = default)
    {
        var version = _frameworkVersions.GetOrAdd(frameworkId, 1);
        var globalVersion = Interlocked.Read(ref _globalVersion);
        var cacheKey = $"framework:{frameworkId:N}:mode:{modeKey}:version:{version}:global:{globalVersion}";

        if (_useDistributed && _distributedCache != null)
        {
            var bytes = await _distributedCache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            if (bytes is { Length: > 0 })
            {
                try
                {
                    var fromDistributed = JsonSerializer.Deserialize<FrameworkCatalogSnapshot>(bytes, JsonOptions);
                    if (fromDistributed != null)
                    {
                        return fromDistributed;
                    }
                }
                catch (JsonException)
                {
                    // Fall through to memory / loader.
                }
            }
        }

        return (await _memoryCache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultTtl;
                var loaded = await loader(cancellationToken).ConfigureAwait(false);

                if (_useDistributed && _distributedCache != null)
                {
                    try
                    {
                        var payload = JsonSerializer.SerializeToUtf8Bytes(loaded, JsonOptions);
                        await _distributedCache.SetAsync(
                            cacheKey,
                            payload,
                            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = DefaultTtl },
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (JsonException)
                    {
                        // Keep memory entry only.
                    }
                }

                return loaded;
            }).ConfigureAwait(false))!;
    }

    public void InvalidateFramework(Guid frameworkId)
    {
        _frameworkVersions.AddOrUpdate(frameworkId, 2, (_, current) => current + 1);
    }

    public void InvalidateAll()
    {
        Interlocked.Increment(ref _globalVersion);
    }
}
