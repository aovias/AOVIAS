using Platform.Dashboard.Models;

namespace Platform.Dashboard.Models;

/// <summary>
/// Composite platform dashboard experience payload: summary, widget metadata, and sections.
/// </summary>
public sealed class DashboardExperienceResult
{
    public DashboardSummaryResult Summary { get; init; } = new();

    public IReadOnlyList<DashboardWidgetDescriptor> Widgets { get; init; } = [];

    public IReadOnlyList<DashboardSectionDescriptor> Sections { get; init; } = [];
}
