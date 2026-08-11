namespace Gateway.DTOs;

public sealed class PlatformActivityEntryDto
{
    public string Id { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public string? Target { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? CorrelationId { get; init; }
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
    public string? DetailsJson { get; init; }
}

public sealed class PlatformActivitySummaryDto
{
    public int AdministrativeActions { get; init; }
    public int SecurityEvents { get; init; }
    public int ConfigurationChanges { get; init; }
    public int UserManagementEvents { get; init; }
    public int IntegrationEvents { get; init; }
    public int NotificationEvents { get; init; }
    public int RecentErrors { get; init; }
}

public sealed class PlatformActivityResponseDto
{
    public PlatformActivitySummaryDto Summary { get; init; } = new();
    public IReadOnlyList<PlatformActivityEntryDto> Items { get; init; } = Array.Empty<PlatformActivityEntryDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
