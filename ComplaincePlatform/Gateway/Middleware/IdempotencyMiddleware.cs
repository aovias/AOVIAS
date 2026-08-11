using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Caching;

namespace Gateway.Middleware;

/// <summary>
/// Middleware for handling idempotency keys on POST /accreditations endpoint.
/// Uses <see cref="IDistributedCache"/> when Redis is configured; otherwise <see cref="IMemoryCache"/>.
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly bool _useDistributed;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(60);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public IdempotencyMiddleware(
        RequestDelegate next,
        IMemoryCache memoryCache,
        IConfiguration configuration,
        ILogger<IdempotencyMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _memoryCache = memoryCache;
        _logger = logger;
        _useDistributed = RedisConfiguration.IsConfigured(configuration);
        _distributedCache = _useDistributed
            ? serviceProvider.GetService<IDistributedCache>()
            : null;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsAccreditationActivationRequest(context))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = ExtractIdempotencyKey(context);
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _logger.LogWarning(
                "POST /accreditations request missing Idempotency-Key header. Request rejected.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    success = false,
                    error = new
                    {
                        message = "Idempotency-Key header is required for accreditation activation.",
                        code = "IDEMPOTENCY_KEY_REQUIRED"
                    }
                }),
                Encoding.UTF8);
            return;
        }

        if (!context.Request.RouteValues.TryGetValue("orgId", out var orgIdValue) ||
            !Guid.TryParse(orgIdValue?.ToString(), out var orgId))
        {
            _logger.LogWarning("POST /accreditations request missing or invalid orgId in route");
            await _next(context);
            return;
        }

        var frameworkId = await ExtractFrameworkIdFromBody(context);
        if (!frameworkId.HasValue)
        {
            _logger.LogWarning("POST /accreditations request missing or invalid frameworkId in body");
            await _next(context);
            return;
        }

        var cacheKey = $"idempotency:accreditation:{idempotencyKey}:{orgId}:{frameworkId.Value}";

        var cachedEntry = await TryGetAsync(cacheKey, context.RequestAborted);
        if (cachedEntry != null)
        {
            _logger.LogInformation(
                "Duplicate idempotency key detected: {IdempotencyKey} for Org {OrgId}, Framework {FrameworkId}. Returning cached response.",
                idempotencyKey, orgId, frameworkId.Value);

            context.Response.StatusCode = cachedEntry.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedEntry.ResponseBody, Encoding.UTF8);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            responseBody.Seek(0, SeekOrigin.Begin);
            var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            if (context.Response.StatusCode == StatusCodes.Status201Created)
            {
                var cacheEntry = new IdempotencyCacheEntry
                {
                    StatusCode = context.Response.StatusCode,
                    ResponseBody = responseBodyText
                };

                await SetAsync(cacheKey, cacheEntry, context.RequestAborted);

                _logger.LogInformation(
                    "Stored idempotency key: {IdempotencyKey} for Org {OrgId}, Framework {FrameworkId}. Response cached for {Minutes} minutes (distributed={Distributed}).",
                    idempotencyKey, orgId, frameworkId.Value, CacheExpiration.TotalMinutes, _useDistributed && _distributedCache != null);
            }

            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task<IdempotencyCacheEntry?> TryGetAsync(string cacheKey, CancellationToken cancellationToken)
    {
        if (_useDistributed && _distributedCache != null)
        {
            var bytes = await _distributedCache.GetAsync(cacheKey, cancellationToken);
            if (bytes is { Length: > 0 })
            {
                return JsonSerializer.Deserialize<IdempotencyCacheEntry>(bytes, JsonOptions);
            }

            return null;
        }

        return _memoryCache.TryGetValue(cacheKey, out IdempotencyCacheEntry? entry) ? entry : null;
    }

    private async Task SetAsync(string cacheKey, IdempotencyCacheEntry entry, CancellationToken cancellationToken)
    {
        if (_useDistributed && _distributedCache != null)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(entry, JsonOptions);
            await _distributedCache.SetAsync(
                cacheKey,
                bytes,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheExpiration
                },
                cancellationToken);
            return;
        }

        _memoryCache.Set(
            cacheKey,
            entry,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                Priority = CacheItemPriority.High
            });
    }

    private static bool IsAccreditationActivationRequest(HttpContext context)
    {
        return context.Request.Method == HttpMethods.Post &&
               context.Request.Path.Value?.EndsWith("/accreditations", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? ExtractIdempotencyKey(HttpContext context)
    {
        return context.Request.Headers["Idempotency-Key"].FirstOrDefault()
            ?? context.Request.Headers["idempotency-key"].FirstOrDefault();
    }

    private async Task<Guid?> ExtractFrameworkIdFromBody(HttpContext context)
    {
        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var bodyText = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        try
        {
            using var doc = JsonDocument.Parse(bodyText);
            if (doc.RootElement.TryGetProperty("frameworkId", out var frameworkIdElement))
            {
                if (Guid.TryParse(frameworkIdElement.GetString(), out var frameworkId))
                {
                    return frameworkId;
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse request body for frameworkId extraction");
        }

        return null;
    }

    private sealed class IdempotencyCacheEntry
    {
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
    }
}
