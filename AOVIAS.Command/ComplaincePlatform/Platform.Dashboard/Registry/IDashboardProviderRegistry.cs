using Platform.Dashboard.Contracts;
using Platform.Dashboard.Models;

namespace Platform.Dashboard.Registry;

public interface IDashboardProviderRegistry
{
    IReadOnlyList<IDashboardProvider> GetProviders(DashboardContext context);

    IDashboardProvider? Resolve(string applicationName);

    IReadOnlyList<DashboardWidgetDescriptor> GetWidgets(DashboardContext context);

    IReadOnlyList<DashboardSectionDescriptor> GetSections(DashboardContext context);
}
