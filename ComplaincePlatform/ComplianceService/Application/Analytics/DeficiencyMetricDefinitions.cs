using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Analytics;

/// <summary>
/// Canonical deficiency metric predicates (Sprint 12B-0 SSOT).
/// All modules MUST use these definitions for operational deficiency KPIs.
/// </summary>
public static class DeficiencyMetricDefinitions
{
    /// <summary>Active remediation — not closed and not cancelled.</summary>
    public static bool IsOpen(ComplianceDeficiency d) =>
        d.Status != DeficiencyStatus.Closed && d.Status != DeficiencyStatus.Cancelled;

    public static bool IsOpen(DeficiencyStatus status) =>
        status != DeficiencyStatus.Closed && status != DeficiencyStatus.Cancelled;

    /// <summary>EF-translatable open filter for IQueryable pipelines.</summary>
    public static IQueryable<ComplianceDeficiency> WhereOpen(IQueryable<ComplianceDeficiency> query) =>
        query.Where(d => d.Status != DeficiencyStatus.Closed && d.Status != DeficiencyStatus.Cancelled);

    /// <summary>Remediation complete.</summary>
    public static bool IsClosed(ComplianceDeficiency d) =>
        d.Status == DeficiencyStatus.Closed;

    /// <summary>Withdrawn from active remediation.</summary>
    public static bool IsCancelled(ComplianceDeficiency d) =>
        d.Status == DeficiencyStatus.Cancelled;

    /// <summary>Verification authority has approved resolution.</summary>
    public static bool IsVerified(ComplianceDeficiency d) =>
        d.Status == DeficiencyStatus.Verified || d.Status == DeficiencyStatus.Closed;

    /// <summary>Flagged by recurrence evaluation engine.</summary>
    public static bool IsRecurring(ComplianceDeficiency d) =>
        d.IsRecurringIssue;

    /// <summary>Active escalation workflow.</summary>
    public static bool IsEscalated(ComplianceDeficiency d) =>
        d.CurrentEscalationState is EscalationState.Escalated or EscalationState.Acknowledged;

    /// <summary>Operational critical severity on the deficiency record (accreditation blocker when open).</summary>
    public static bool IsCriticalDeficiency(ComplianceDeficiency d) =>
        d.Severity == DeficiencySeverity.Critical;

    /// <summary>Assessed risk matrix critical (distinct from severity).</summary>
    public static bool IsCriticalRisk(ComplianceDeficiency d) =>
        d.AssessedRiskLevel == DeficiencyRiskLevel.Critical;

    /// <summary>Open deficiency with active CAPA work.</summary>
    public static bool HasOpenCapa(ComplianceDeficiency d) =>
        d.CorrectiveActionStatus != CapaActionStatus.NotStarted
        || d.PreventiveActionStatus != CapaActionStatus.NotStarted
        || !string.IsNullOrWhiteSpace(d.CorrectiveActionSummary);

    /// <summary>Open deficiency eligible for SLA tracking.</summary>
    public static bool IsSlaTrackedOpen(ComplianceDeficiency d) =>
        IsOpen(d) && d.SlaPolicyId != null;
}
