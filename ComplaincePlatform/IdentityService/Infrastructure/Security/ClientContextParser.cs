namespace IdentityService.Infrastructure.Security;

public static class ClientContextParser
{
    public static string? GetIpAddress(HttpContext? context)
        => context?.Connection.RemoteIpAddress?.ToString();

    public static string? GetUserAgent(HttpContext? context)
        => context?.Request.Headers.UserAgent.FirstOrDefault();

    public static string? ParseOperatingSystem(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            return "Windows";
        if (userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase))
            return "macOS";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "Android";
        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            return "iOS";
        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            return "Linux";

        return "Unknown";
    }

    public static string? ParseBrowser(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return null;
        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) return "Edge";
        if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase)) return "Chrome";
        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase)) return "Firefox";
        if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase)) return "Safari";
        return "Unknown";
    }

    public static string? ParsePlatform(string? userAgent) => ParseOperatingSystem(userAgent);

    public static string BuildFingerprint(string? deviceId, string? userAgent, string? ip)
        => $"{deviceId ?? "na"}|{userAgent ?? "na"}|{ip ?? "na"}";

    public static string? ResolveGeoLocation(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return null;
        if (ipAddress.StartsWith("127.") || ipAddress == "::1") return "Local";
        return "Unknown";
    }

    public static string? GetCorrelationId(HttpContext? context)
    {
        if (context == null)
            return null;

        return context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
               ?? context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
               ?? context.TraceIdentifier;
    }
}
