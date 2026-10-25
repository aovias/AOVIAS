namespace Platform.Dashboard.Models;

/// <summary>
/// Describes an AOVIAS application that contributes to the platform dashboard.
/// </summary>
public sealed class DashboardApplicationInfo
{
    public required string ApplicationId { get; init; }

    public required string DisplayName { get; init; }

    public string? Description { get; init; }

    public int Order { get; init; }

    public bool IsEnabled { get; init; } = true;
}
