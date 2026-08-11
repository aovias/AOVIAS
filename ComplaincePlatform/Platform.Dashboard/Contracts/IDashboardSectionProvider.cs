using Platform.Dashboard.Models;

namespace Platform.Dashboard.Contracts;

/// <summary>
/// Exposes section metadata for a dashboard application.
/// </summary>
public interface IDashboardSectionProvider
{
    IReadOnlyList<DashboardSectionDescriptor> GetSections(DashboardContext context);
}
