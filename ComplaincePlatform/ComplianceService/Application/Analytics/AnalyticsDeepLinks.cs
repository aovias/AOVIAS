namespace ComplianceService.Application.Analytics;

/// <summary>
/// Canonical deep-link routes from Analytics Hub to operational execution modules (Sprint 12B-1).
/// </summary>
public static class AnalyticsDeepLinks
{
    public const string DccRoute = "/app/audit/corrective-actions";
    public const string FindingsRoute = "/app/audit/findings";
    public const string TasksRoute = "/app/tasks";
    public const string EvidenceRoute = "/app/evidence";
    public const string ComplianceMaturityRoute = "/app/compliance-maturity";
    public const string JourneyRoute = "/app/journey";

    public static Application.DTOs.AnalyticsDeepLinkDto DeficiencyCommandCenter(string? label = null, string? filter = null) =>
        new()
        {
            Label = label ?? "Open Deficiency Command Center",
            Route = DccRoute,
            QueryParams = filter is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["filter"] = filter },
        };

    public static Application.DTOs.AnalyticsDeepLinkDto Findings(string? label = null) =>
        new() { Label = label ?? "Open Findings", Route = FindingsRoute };

    public static Application.DTOs.AnalyticsDeepLinkDto Tasks(string? label = null, string? status = null) =>
        new()
        {
            Label = label ?? "Open Tasks",
            Route = TasksRoute,
            QueryParams = status is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["status"] = status },
        };

    public static Application.DTOs.AnalyticsDeepLinkDto EvidenceReview(string? label = null) =>
        new()
        {
            Label = label ?? "Open Evidence Review",
            Route = EvidenceRoute,
            QueryParams = new Dictionary<string, string> { ["view"] = "pendingReview" },
        };

    public static Application.DTOs.AnalyticsDeepLinkDto ComplianceMaturity(string? label = null) =>
        new() { Label = label ?? "Open Compliance Maturity", Route = ComplianceMaturityRoute };

    public static Application.DTOs.AnalyticsDeepLinkDto Journey(string? label = null) =>
        new() { Label = label ?? "Open Journey", Route = JourneyRoute };

    public static Application.DTOs.AnalyticsDeepLinkDto DepartmentDeficiencies(string department) =>
        new()
        {
            Label = $"Open {department} Deficiencies",
            Route = DccRoute,
            QueryParams = new Dictionary<string, string> { ["department"] = department },
        };

    public static Application.DTOs.AnalyticsDeepLinkDto RecurringIssues() =>
        DeficiencyCommandCenter("Open Recurring Issues", "recurring");

    public static Application.DTOs.AnalyticsDeepLinkDto SlaBreaches() =>
        DeficiencyCommandCenter("Open SLA Breaches", "slaBreached");

    public static Application.DTOs.AnalyticsDeepLinkDto CriticalDeficiencies() =>
        DeficiencyCommandCenter("Open Critical Deficiencies", "critical");

    public static Application.DTOs.AnalyticsDeepLinkDto Escalations() =>
        DeficiencyCommandCenter("Open Escalations", "escalated");
}
