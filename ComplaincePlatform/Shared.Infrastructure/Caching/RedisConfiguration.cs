using Microsoft.Extensions.Configuration;

namespace Shared.Infrastructure.Caching;

/// <summary>
/// Resolves optional Redis connection from <c>ConnectionStrings:Redis</c> or <c>Redis:Configuration</c>.
/// </summary>
public static class RedisConfiguration
{
    public static string? GetConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var value = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:Configuration"];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static bool IsConfigured(IConfiguration configuration)
        => !string.IsNullOrWhiteSpace(GetConnectionString(configuration));
}
