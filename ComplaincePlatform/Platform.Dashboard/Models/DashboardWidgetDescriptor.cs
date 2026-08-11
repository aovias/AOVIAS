namespace Platform.Dashboard.Models;

/// <summary>
/// Metadata describing a dashboard widget. No rendering logic.
/// </summary>
public sealed class DashboardWidgetDescriptor
{
    public required string Id { get; init; }

    public required string Application { get; init; }

    public required string Title { get; init; }

    public string? Icon { get; init; }

    public int Priority { get; init; }

    public DashboardLoadingPhase Phase { get; init; }

    public IReadOnlyList<string> Permissions { get; init; } = [];

    public TimeSpan? CacheTtl { get; init; }

    public bool Lazy { get; init; }

    public bool Critical { get; init; }

    public string? SectionEndpoint { get; init; }

    /// <summary>Visible, Hidden, Conditional</summary>
    public required string Visibility { get; init; }
}
