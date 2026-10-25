using ComplianceService.Application.RoleExperience;
using Platform.Dashboard.Models;

namespace ComplianceService.Application.Dashboard.Praman;

/// <summary>
/// Static widget and section metadata for PRAMAN Home Dashboard and Command Center surfaces.
/// Metadata only — no rendering or HTTP orchestration.
/// Permissions include <c>compliance.api</c> plus role-pack tags for experience filtering.
/// </summary>
internal static class PramanDashboardCatalog
{
    private static readonly string App = PramanDashboardConstants.ApplicationName;
    private static readonly string BasePerm = PramanDashboardConstants.Permission;

    private static readonly string PackAdmin = RoleExperiencePackRegistry.ToPermissionTag("hospital-admin");
    private static readonly string PackQm = RoleExperiencePackRegistry.ToPermissionTag("quality-manager");
    private static readonly string PackDept = RoleExperiencePackRegistry.ToPermissionTag("department-head");
    private static readonly string PackConsultant = RoleExperiencePackRegistry.ToPermissionTag("consultant");
    private static readonly string PackAuditor = RoleExperiencePackRegistry.ToPermissionTag("auditor");

    public static IReadOnlyList<DashboardWidgetDescriptor> Widgets { get; } =
    [
        // Home Dashboard — summary / critical
        Widget("home-executive-kpi-strip", "Executive KPI Strip", "bar-chart-2", 10, DashboardLoadingPhase.CriticalWidgets, critical: true, endpoint: "/api/v1/dashboard/summary",
            packs: [PackAdmin, PackQm, PackConsultant]),
        Widget("home-alert-banner", "Alert Banner", "alert-triangle", 20, DashboardLoadingPhase.Summary, critical: true, endpoint: "/api/v1/compliance/snapshots/hospital/{hospitalId}",
            packs: [PackAdmin, PackQm, PackDept, PackConsultant, PackAuditor]),
        Widget("home-compliance-gauge", "Compliance Gauge", "gauge", 30, DashboardLoadingPhase.CriticalWidgets, critical: true, endpoint: "/api/v1/dashboard/summary",
            packs: [PackAdmin, PackQm, PackConsultant, PackAuditor]),
        Widget("home-completion-gauge", "Completion Gauge", "pie-chart", 40, DashboardLoadingPhase.CriticalWidgets, critical: true, endpoint: "/api/v1/dashboard/summary",
            packs: [PackAdmin, PackQm]),

        // Home Dashboard — operational
        Widget("home-framework-overview", "Framework Overview", "layers", 50, DashboardLoadingPhase.OperationalWidgets, lazy: true, endpoint: "/api/v1/compliance/snapshots/hospital/{hospitalId}",
            packs: [PackAdmin, PackQm, PackConsultant]),
        Widget("home-hospital-overview", "Hospital Overview", "building-2", 60, DashboardLoadingPhase.OperationalWidgets, lazy: true, endpoint: "/api/v1/compliance/snapshots/hospital/{hospitalId}",
            packs: [PackAdmin]),
        Widget("home-milestones", "Milestones", "flag", 70, DashboardLoadingPhase.OperationalWidgets, lazy: true, endpoint: "/api/v1/compliance/snapshots/hospital/{hospitalId}",
            packs: [PackAdmin, PackQm, PackDept, PackConsultant]),
        Widget("home-activity-stream", "Activity Stream", "activity", 80, DashboardLoadingPhase.OperationalWidgets, lazy: true, endpoint: "/api/v1/compliance/snapshots/hospital/{hospitalId}",
            packs: [PackDept, PackAuditor]),
        Widget("home-task-type-breakdown", "Task Type Breakdown", "list-checks", 90, DashboardLoadingPhase.OperationalWidgets, lazy: true, endpoint: "/api/v1/compliance/snapshots/hospital/{hospitalId}",
            packs: [PackQm, PackDept]),
        Widget("home-department-heatmap", "Department Heatmap", "grid-3x3", 100, DashboardLoadingPhase.OperationalWidgets, lazy: true, endpoint: "/api/v1/dashboard/hospital",
            packs: [PackAdmin, PackQm, PackDept]),

        // Home Dashboard — analytics
        Widget("home-score-trend", "Score Trend", "trending-up", 110, DashboardLoadingPhase.Analytics, lazy: true, endpoint: "/api/v1/dashboard/compliance-trend",
            packs: [PackAdmin, PackConsultant]),
        Widget("home-chapter-breakdown", "Chapter Breakdown", "book-open", 120, DashboardLoadingPhase.Analytics, lazy: true, endpoint: "/api/v1/dashboard/hospital",
            packs: [PackQm, PackConsultant]),
        Widget("home-risk-distribution", "Risk Distribution", "shield-alert", 130, DashboardLoadingPhase.Analytics, lazy: true, endpoint: "/api/v1/compliance/snapshots/hospital/{hospitalId}",
            packs: [PackAdmin, PackQm, PackDept, PackConsultant, PackAuditor]),

        // Command Center — critical / operational
        Widget("cc-executive-kpi-bar", "Executive KPI Bar", "bar-chart", 10, DashboardLoadingPhase.CriticalWidgets, critical: true, endpoint: "/api/v1/compliance/control-center/overview",
            packs: [PackAdmin]),
        Widget("cc-audit-readiness", "Audit Readiness", "clipboard-check", 20, DashboardLoadingPhase.CriticalWidgets, critical: true, endpoint: "/api/v1/compliance/control-center/audit-readiness",
            packs: [PackAdmin, PackConsultant, PackAuditor]),
        Widget("cc-risk-command", "Risk Command Center", "shield", 30, DashboardLoadingPhase.OperationalWidgets, lazy: true, endpoint: "/api/v1/compliance/control-center/high-risk",
            packs: [PackAdmin, PackQm]),
        Widget("cc-action-center", "Action Center", "check-square", 40, DashboardLoadingPhase.OperationalWidgets, lazy: true, endpoint: "/api/v1/compliance/control-center/action-center",
            packs: [PackQm]),
        Widget("cc-activity-stream", "Activity Stream", "activity", 50, DashboardLoadingPhase.OperationalWidgets, lazy: true, endpoint: "/api/v1/compliance/control-center/activity-stream",
            packs: [PackAuditor]),
        Widget("cc-smart-alerts", "Smart Alerts", "bell", 60, DashboardLoadingPhase.OperationalWidgets, lazy: true, endpoint: "/api/v1/compliance/control-center/alerts",
            packs: [PackQm, PackAdmin]),

        // Command Center — analytics / AI
        Widget("cc-compliance-analytics", "Compliance Analytics", "line-chart", 70, DashboardLoadingPhase.Analytics, lazy: true, endpoint: "/api/v1/compliance/control-center/trend",
            packs: [PackAdmin, PackQm]),
        Widget("cc-ai-intelligence", "AI Intelligence Panel", "sparkles", 80, DashboardLoadingPhase.Ai, lazy: true, endpoint: "/api/v1/compliance/control-center/overview",
            packs: [PackAdmin]),
        Widget("cc-ai-copilot", "AI Copilot", "bot", 90, DashboardLoadingPhase.Ai, lazy: true, endpoint: "/api/v1/ai/assistant",
            packs: [PackAdmin, PackQm, PackDept, PackConsultant, PackAuditor]),
    ];

    public static IReadOnlyList<DashboardSectionDescriptor> Sections { get; } =
    [
        Section("home-summary", "Home Summary", "layout-dashboard", 10, DashboardLoadingPhase.Summary, critical: true,
            widgetIds: ["home-executive-kpi-strip", "home-alert-banner", "home-compliance-gauge", "home-completion-gauge"],
            packs: [PackAdmin, PackQm, PackDept, PackConsultant, PackAuditor]),
        Section("home-operations", "Home Operations", "briefcase", 20, DashboardLoadingPhase.OperationalWidgets,
            widgetIds: ["home-framework-overview", "home-hospital-overview", "home-milestones", "home-activity-stream", "home-task-type-breakdown", "home-department-heatmap"],
            packs: [PackAdmin, PackQm, PackDept, PackConsultant]),
        Section("home-analytics", "Home Analytics", "trending-up", 30, DashboardLoadingPhase.Analytics,
            widgetIds: ["home-score-trend", "home-chapter-breakdown", "home-risk-distribution"],
            packs: [PackAdmin, PackQm, PackConsultant, PackAuditor]),
        Section("cc-command", "Command Center Operations", "command", 10, DashboardLoadingPhase.OperationalWidgets, critical: true,
            endpoint: "/api/v1/compliance/control-center/overview",
            widgetIds: ["cc-executive-kpi-bar", "cc-audit-readiness", "cc-risk-command", "cc-action-center", "cc-activity-stream", "cc-smart-alerts"],
            packs: [PackAdmin, PackQm, PackAuditor, PackConsultant]),
        Section("cc-analytics-ai", "Command Center Analytics & AI", "brain", 20, DashboardLoadingPhase.Analytics,
            widgetIds: ["cc-compliance-analytics", "cc-ai-intelligence", "cc-ai-copilot"],
            packs: [PackAdmin, PackQm, PackDept, PackConsultant, PackAuditor]),
    ];

    private static DashboardWidgetDescriptor Widget(
        string id,
        string title,
        string icon,
        int priority,
        DashboardLoadingPhase phase,
        bool critical = false,
        bool lazy = false,
        string? endpoint = null,
        string[]? packs = null) =>
        new()
        {
            Id = id,
            Application = App,
            Title = title,
            Icon = icon,
            Priority = priority,
            Phase = phase,
            Permissions = BuildPermissions(packs),
            CacheTtl = null,
            Lazy = lazy,
            Critical = critical,
            SectionEndpoint = endpoint,
            Visibility = packs is { Length: > 0 } ? "Conditional" : "Visible",
        };

    private static DashboardSectionDescriptor Section(
        string id,
        string title,
        string icon,
        int priority,
        DashboardLoadingPhase phase,
        bool critical = false,
        bool lazy = false,
        string? endpoint = null,
        string[]? widgetIds = null,
        string[]? packs = null) =>
        new()
        {
            Id = id,
            Application = App,
            Title = title,
            Icon = icon,
            Priority = priority,
            Phase = phase,
            Permissions = BuildPermissions(packs),
            CacheTtl = null,
            Lazy = lazy,
            Critical = critical,
            SectionEndpoint = endpoint,
            Visibility = packs is { Length: > 0 } ? "Conditional" : "Visible",
            WidgetIds = widgetIds ?? [],
        };

    private static string[] BuildPermissions(string[]? packs)
    {
        if (packs is null || packs.Length == 0)
            return [BasePerm];

        var list = new List<string>(packs.Length + 1) { BasePerm };
        list.AddRange(packs);
        return list.ToArray();
    }
}
