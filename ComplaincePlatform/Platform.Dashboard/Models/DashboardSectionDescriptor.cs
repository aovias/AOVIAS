namespace Platform.Dashboard.Models;

/// <summary>
/// Metadata describing a dashboard section grouping one or more widgets.
/// </summary>
public sealed class DashboardSectionDescriptor
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

    public required string Visibility { get; init; }

    public IReadOnlyList<string> WidgetIds { get; init; } = [];
}
