using Microsoft.Extensions.Configuration;

namespace ComplianceService.Infrastructure.Notifications;

/// <summary>
/// Resolves whether Compliance should publish via CommunicationService HTTP client.
/// Accepts either <c>CommunicationService:BaseUrl</c> (preferred) or <c>Communication:BaseUrl</c>.
/// </summary>
public static class ComplianceNotificationOptions
{
    public static string? ResolveBaseUrl(IConfiguration configuration)
        => FirstNonEmpty(
            configuration["CommunicationService:BaseUrl"],
            configuration["Communication:BaseUrl"]);

    public static bool IsCommunicationConfigured(IConfiguration configuration)
        => !string.IsNullOrWhiteSpace(ResolveBaseUrl(configuration));

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
