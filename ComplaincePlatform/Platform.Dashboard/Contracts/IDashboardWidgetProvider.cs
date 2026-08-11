using Platform.Dashboard.Models;

namespace Platform.Dashboard.Contracts;

/// <summary>
/// Exposes widget metadata for a dashboard application.
/// </summary>
public interface IDashboardWidgetProvider
{
    IReadOnlyList<DashboardWidgetDescriptor> GetWidgets(DashboardContext context);
}
