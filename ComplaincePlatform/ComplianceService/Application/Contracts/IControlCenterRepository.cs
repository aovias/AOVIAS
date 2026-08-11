using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Read model + scoped raw data the Control Center aggregation engine works from. Loaded in a
/// single pass per request to keep the aggregation pure and DB round-trips bounded.
/// </summary>
public sealed class ControlCenterDataset
{
    public Guid TenantId { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;

    public List<ComplianceJourney> Journeys { get; set; } = new();
    public List<ComplianceTask> Tasks { get; set; } = new();
    public List<Finding> OpenFindings { get; set; } = new();

    /// <summary>
    /// Legacy <see cref="Capa"/> rows (orphaned write path). Kept for transitional reads only.
    /// CAPA KPIs must use <see cref="ActiveDeficiencies"/> (SSOT).
    /// </summary>
    public List<Capa> Capas { get; set; } = new();

    /// <summary>Active deficiencies carrying CAPA fields — Control Center CAPA SSOT.</summary>
    public List<ComplianceDeficiency> ActiveDeficiencies { get; set; } = new();

    public List<ComplianceChapter> Chapters { get; set; } = new();
    public List<ComplianceDepartment> Departments { get; set; } = new();
    public List<Evidence> RecentEvidence { get; set; } = new();
    public List<ScheduledAudit> ScheduledAudits { get; set; } = new();
    public List<Alert> Alerts { get; set; } = new();
    public List<ActivityEvent> Activities { get; set; } = new();

    /// <summary>Most recent compliance snapshots in scope (descending CalculatedAt), for trend + deltas.</summary>
    public List<ComplianceSnapshot> Snapshots { get; set; } = new();

    // Evidence review posture (from ObjectiveElementEvidence within scope).
    public int EvidenceApproved { get; set; }
    public int EvidencePending { get; set; }
    public int EvidenceRejected { get; set; }

    // Coverage basis.
    public int ScorableObjectiveElements { get; set; }
    public int ObjectiveElementsWithVerifiedEvidence { get; set; }
}

public interface IControlCenterRepository
{
    /// <summary>Loads all scoped data the aggregation engine needs in a single pass.</summary>
    Task<ControlCenterDataset> LoadDatasetAsync(ControlCenterScope scope, CancellationToken cancellationToken = default);

    /// <summary>Resolves the organization name (best-effort) for the header.</summary>
    Task<string> GetOrganizationNameAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Idempotently seeds the canonical department master for the organization.</summary>
    Task SeedDepartmentsAsync(ControlCenterScope scope, IReadOnlyCollection<string> departmentNames, CancellationToken cancellationToken = default);

    /// <summary>Idempotently upserts scheduled audits (by source journey) and ad-hoc planned audits.</summary>
    Task UpsertScheduledAuditsAsync(IReadOnlyCollection<ScheduledAudit> audits, CancellationToken cancellationToken = default);

    /// <summary>Idempotently inserts activity events (by deterministic id) that do not yet exist.</summary>
    Task UpsertActivityEventsAsync(IReadOnlyCollection<ActivityEvent> events, CancellationToken cancellationToken = default);

    /// <summary>Idempotently upserts live alerts (by deterministic id / dedupe key).</summary>
    Task UpsertAlertsAsync(IReadOnlyCollection<Alert> alerts, CancellationToken cancellationToken = default);
}
