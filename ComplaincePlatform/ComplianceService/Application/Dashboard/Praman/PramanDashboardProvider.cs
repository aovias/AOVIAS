using ComplianceService.Application.Dashboard.Summary;
using Platform.Dashboard.Contracts;
using Platform.Dashboard.Models;

namespace ComplianceService.Application.Dashboard.Praman;

/// <summary>
/// PRAMAN compliance dashboard provider. Exposes widget/section metadata and lightweight summary contributions.
/// No UI logic; Phase 1 KPIs sourced from <see cref="IDashboardSummaryEngine"/>.
/// </summary>
public sealed class PramanDashboardProvider
    : IDashboardProvider,
      IDashboardWidgetProvider,
      IDashboardSectionProvider
{
    private readonly IDashboardSummaryEngine _summaryEngine;

    public PramanDashboardProvider(IDashboardSummaryEngine summaryEngine)
    {
        _summaryEngine = summaryEngine;
    }

    public string ApplicationName => PramanDashboardConstants.ApplicationName;

    public int Order => 10;

    public IReadOnlyList<string> GetPermissions() => [PramanDashboardConstants.Permission];

    public bool SupportsApplication(string applicationId) =>
        string.Equals(applicationId, PramanDashboardConstants.ApplicationName, StringComparison.OrdinalIgnoreCase);

    public async Task<DashboardSummaryContribution> GetSummaryContributionAsync(
        DashboardContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.HospitalId.HasValue || context.HospitalId.Value == Guid.Empty)
        {
            return BuildEmptyContribution();
        }

        var summary = await _summaryEngine.GetAsync(context, cancellationToken);
        var healthStatus = ResolveHealthStatus(summary.OverallCompliancePercent, summary.AlertsCount);

        return new DashboardSummaryContribution
        {
            Application = new DashboardApplicationInfo
            {
                ApplicationId = PramanDashboardConstants.ApplicationName,
                DisplayName = PramanDashboardConstants.ApplicationDisplayName,
                Description = "Compliance program overview and command center.",
                Order = Order,
                IsEnabled = true,
            },
            OverallHealth = new DashboardOverallHealth
            {
                Status = healthStatus,
                Message = $"Compliance score {summary.OverallCompliancePercent:F1}%",
                Score = summary.OverallCompliancePercent,
            },
            CriticalAlerts = summary.AlertsCount,
            UnreadNotifications = summary.NotificationsCount,
            PendingTasks = summary.PendingTasks,
            LastUpdated = summary.LastUpdated,
        };
    }

    public IReadOnlyList<DashboardWidgetDescriptor> GetWidgets(DashboardContext context) =>
        PramanDashboardCatalog.Widgets;

    public IReadOnlyList<DashboardSectionDescriptor> GetSections(DashboardContext context) =>
        PramanDashboardCatalog.Sections;

    private static DashboardSummaryContribution BuildEmptyContribution() =>
        new()
        {
            Application = new DashboardApplicationInfo
            {
                ApplicationId = PramanDashboardConstants.ApplicationName,
                DisplayName = PramanDashboardConstants.ApplicationDisplayName,
                Order = 10,
            },
            OverallHealth = new DashboardOverallHealth
            {
                Status = "Unknown",
                Message = "Hospital context required for PRAMAN summary.",
            },
            CriticalAlerts = 0,
            UnreadNotifications = 0,
            PendingTasks = 0,
            LastUpdated = DateTimeOffset.UtcNow,
        };

    private static string ResolveHealthStatus(decimal complianceScore, int openFindings)
    {
        if (openFindings > 0 || complianceScore < 60m)
        {
            return "Critical";
        }

        if (complianceScore < 80m)
        {
            return "Warning";
        }

        return "Healthy";
    }
}
