namespace ComplianceService.Application.Dashboard.Projection;

/// <summary>
/// Documents projection field ownership for H1.4 event mapping.
/// </summary>
public static class DashboardProjectionFieldOwnership
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> EventToFields { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ComplianceScoreUpdatedDomainEvent"] =
            [
                "OverallComplianceScore", "AuditConfidenceScore", "ScoreTrends", "Frameworks.AvgScore", "Summary.AvgComplianceScore"
            ],
            ["ComplianceFrameworkSnapshotRecalculated"] =
            [
                "OverallComplianceScore", "ScoreTrends", "Frameworks", "Summary"
            ],
            ["ObligationStatusUpdatedDomainEvent"] =
            [
                "Summary.CompletionRate", "Frameworks"
            ],
            ["DeficiencyIdentifiedDomainEvent"] =
            [
                "Alerts", "OpenAlertsCount", "TasksSummary"
            ],
            ["DeficiencyClosedDomainEvent"] =
            [
                "Alerts", "OpenAlertsCount", "TasksSummary", "Summary"
            ],
            ["DeficiencyStatusChangedDomainEvent"] =
            [
                "Alerts", "TasksSummary"
            ],
            ["ComplianceStateChangedDomainEvent"] =
            [
                "ActiveJourneysCount", "JourneyStatusCounts", "Frameworks", "Milestones", "RecentActivity", "Summary"
            ],
            ["ComplianceEvent.TaskCreated"] =
            [
                "PendingTasksCount", "TasksSummary", "Summary.OverdueTasks"
            ],
            ["ComplianceEvent.TaskCompleted"] =
            [
                "PendingTasksCount", "TasksSummary", "Summary.CompletionRate"
            ],
            ["ComplianceEvent.FindingCreated"] =
            [
                "OpenAlertsCount", "Alerts"
            ],
            ["ComplianceEvent.EvidenceUploaded"] =
            [
                "RecentActivity"
            ],
            ["ComplianceEvent.ComplianceEvaluated"] =
            [
                "Summary", "Frameworks", "OverallComplianceScore"
            ],
            ["ComplianceEnrolledDomainEvent"] =
            [
                "TotalJourneysCount", "ActiveJourneysCount", "Frameworks", "Hospitals", "Summary"
            ],
        };
}
