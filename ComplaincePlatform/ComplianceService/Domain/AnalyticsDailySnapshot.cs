namespace ComplianceService.Domain;

/// <summary>
/// Append-only daily analytics rollup for historical trend analysis (Sprint 12B-1).
/// One row per organization, hospital, framework, and calendar day.
/// </summary>
public class AnalyticsDailySnapshot
{
    public Guid Id { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid FrameworkId { get; set; }

    public decimal ReadinessScore { get; set; }
    public decimal ComplianceMaturityIndex { get; set; }
    public int OpenDeficiencies { get; set; }
    public int CriticalDeficiencies { get; set; }
    public int CapaOpen { get; set; }
    public int CapaEffective { get; set; }
    public int CapaIneffective { get; set; }
    public int EscalationsOpen { get; set; }
    public int SlaBreached { get; set; }
    public int StageUnlockedCount { get; set; }
    public int RecurringIssues { get; set; }
    public int EvidencePendingVerification { get; set; }
    public int TaskBacklog { get; set; }

    public DateTime CreatedAt { get; set; }
}
