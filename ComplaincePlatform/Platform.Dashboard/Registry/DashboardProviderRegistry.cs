using Platform.Dashboard.Contracts;
using Platform.Dashboard.Models;

namespace Platform.Dashboard.Registry;

/// <summary>
/// Discovers dashboard providers through DI, filters by application and permissions, and merges contributions.
/// </summary>
public sealed class DashboardProviderRegistry : IDashboardProviderRegistry
{
    private readonly IReadOnlyList<IDashboardProvider> _providers;

    public DashboardProviderRegistry(IEnumerable<IDashboardProvider> providers)
    {
        _providers = providers
            .OrderBy(p => p.Order)
            .ThenBy(p => p.ApplicationName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<IDashboardProvider> GetProviders(DashboardContext context)
    {
        return _providers
            .Where(p => MatchesApplicationFilter(p, context))
            .Where(p => HasRequiredPermissions(p, context))
            .ToList();
    }

    public IDashboardProvider? Resolve(string applicationName)
    {
        return _providers.FirstOrDefault(
            p => string.Equals(p.ApplicationName, applicationName, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<DashboardWidgetDescriptor> GetWidgets(DashboardContext context)
    {
        return GetProviders(context)
            .OfType<IDashboardWidgetProvider>()
            .SelectMany(p => p.GetWidgets(context))
            .Where(w => HasWidgetPermissions(w, context))
            .OrderBy(w => w.Phase)
            .ThenBy(w => w.Priority)
            .ThenBy(w => w.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<DashboardSectionDescriptor> GetSections(DashboardContext context)
    {
        return GetProviders(context)
            .OfType<IDashboardSectionProvider>()
            .SelectMany(p => p.GetSections(context))
            .Where(s => HasSectionPermissions(s, context))
            .OrderBy(s => s.Phase)
            .ThenBy(s => s.Priority)
            .ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool MatchesApplicationFilter(IDashboardProvider provider, DashboardContext context)
    {
        if (context.EnabledApplications is null || context.EnabledApplications.Count == 0)
        {
            return true;
        }

        return context.EnabledApplications.Any(app => provider.SupportsApplication(app));
    }

    private static bool HasRequiredPermissions(IDashboardProvider provider, DashboardContext context)
    {
        var required = provider.GetPermissions();
        if (required.Count == 0)
        {
            return true;
        }

        return required.All(p => context.Permissions.Contains(p));
    }

    private static bool HasWidgetPermissions(DashboardWidgetDescriptor widget, DashboardContext context) =>
        MatchesTaggedPermissions(widget.Permissions, context);

    private static bool HasSectionPermissions(DashboardSectionDescriptor section, DashboardContext context) =>
        MatchesTaggedPermissions(section.Permissions, context);

    /// <summary>
    /// Base permissions (e.g. compliance.api) require All match.
    /// Role-pack tags (<c>role-pack:*</c>) require Any match when present — otherwise widgets would be unreachable.
    /// </summary>
    private static bool MatchesTaggedPermissions(IReadOnlyList<string> required, DashboardContext context)
    {
        if (required.Count == 0)
        {
            return true;
        }

        const string rolePackPrefix = "role-pack:";
        var baseRequired = required.Where(p => !p.StartsWith(rolePackPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
        var packTags = required.Where(p => p.StartsWith(rolePackPrefix, StringComparison.OrdinalIgnoreCase)).ToList();

        if (baseRequired.Count > 0 && !baseRequired.All(p => context.Permissions.Contains(p)))
        {
            return false;
        }

        if (packTags.Count == 0)
        {
            return true;
        }

        // When context has no role-pack claim yet, keep widgets visible (frontend / home API also filters).
        var contextPacks = context.Permissions
            .Where(p => p.StartsWith(rolePackPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (contextPacks.Count == 0)
        {
            return true;
        }

        return packTags.Any(p => context.Permissions.Contains(p));
    }
}
