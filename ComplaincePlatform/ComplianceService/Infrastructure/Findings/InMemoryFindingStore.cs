using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Infrastructure.Findings;

/// <summary>Thread-safe in-memory store; replace with EF repository later without changing API.</summary>
public sealed class InMemoryFindingStore
{
    private readonly object _sync = new();
    private readonly List<ManagedFindingRecord> _items = new();
    private readonly HashSet<Guid> _seededOrgs = new();

    public IReadOnlyList<ManagedFindingRecord> GetForOrganization(Guid organizationId)
    {
        lock (_sync)
        {
            EnsureSeedLocked(organizationId);
            return _items.Where(x => x.OrganizationId == organizationId).ToList();
        }
    }

    public ManagedFindingRecord? GetById(Guid organizationId, Guid id)
    {
        lock (_sync)
        {
            EnsureSeedLocked(organizationId);
            return _items.FirstOrDefault(x => x.OrganizationId == organizationId && x.Id == id);
        }
    }

    public void Add(ManagedFindingRecord record)
    {
        lock (_sync)
        {
            _items.Add(record);
        }
    }

    private void EnsureSeedLocked(Guid organizationId)
    {
        if (_seededOrgs.Contains(organizationId))
            return;

        var now = DateTime.UtcNow;
        var u1 = Guid.NewGuid();
        var seeds = new[]
        {
            New(organizationId, "Fire exit signage missing on West wing", "FSE-2", FindingSeverity.Critical, FindingStatus.Open, FindingDepartment.Security, FindingRaisedBy.InternalAudit, now.AddDays(-10)),
            New(organizationId, "CCTV blind spot at emergency entrance", "SEC-4", FindingSeverity.Major, FindingStatus.InProgress, FindingDepartment.Security, FindingRaisedBy.ExternalAudit, now.AddDays(-8)),
            New(organizationId, "HR personnel files incomplete for new hires", "HR-1", FindingSeverity.Major, FindingStatus.Verified, FindingDepartment.HR, FindingRaisedBy.ComplianceTeam, now.AddDays(-14)),
            New(organizationId, "Sterilizer calibration certificate expired", "EQ-9", FindingSeverity.Critical, FindingStatus.Open, FindingDepartment.Clinical, FindingRaisedBy.InternalAudit, now.AddDays(-3)),
            New(organizationId, "Biomedical equipment service log gaps", "EQ-3", FindingSeverity.Minor, FindingStatus.Closed, FindingDepartment.Clinical, FindingRaisedBy.InternalAudit, now.AddDays(-60)),
            New(organizationId, "Medical gas pipeline inspection overdue", "SAF-7", FindingSeverity.Major, FindingStatus.InProgress, FindingDepartment.Clinical, FindingRaisedBy.ComplianceTeam, now.AddDays(-5)),
            New(organizationId, "Access control badge deprovisioning delay", "SEC-1", FindingSeverity.Minor, FindingStatus.Open, FindingDepartment.Security, FindingRaisedBy.ComplianceTeam, now.AddDays(-2)),
            New(organizationId, "Staff training records not updated post-audit", "HR-5", FindingSeverity.Major, FindingStatus.Verified, FindingDepartment.HR, FindingRaisedBy.ExternalAudit, now.AddDays(-12)),
            New(organizationId, "Pharmacy refrigerator temperature monitoring gap", "PH-2", FindingSeverity.Critical, FindingStatus.Open, FindingDepartment.Clinical, FindingRaisedBy.InternalAudit, now.AddDays(-1)),
            New(organizationId, "Incident reporting form not on intranet", "ADM-4", FindingSeverity.Minor, FindingStatus.InProgress, FindingDepartment.Admin, FindingRaisedBy.InternalAudit, now.AddDays(-6)),
            New(organizationId, "Backup generator weekly test not documented", "FAC-8", FindingSeverity.Major, FindingStatus.Verified, FindingDepartment.Admin, FindingRaisedBy.ComplianceTeam, now.AddDays(-20)),
            New(organizationId, "Patient data access audit trail sample failed", "ISC-2", FindingSeverity.Major, FindingStatus.Open, FindingDepartment.Security, FindingRaisedBy.ExternalAudit, now.AddDays(-4)),
            New(organizationId, "Roster compliance for night shift nursing", "CL-11", FindingSeverity.Minor, FindingStatus.Closed, FindingDepartment.Clinical, FindingRaisedBy.InternalAudit, now.AddDays(-90)),
            New(organizationId, "Vendor background check missing for logistics", "HR-8", FindingSeverity.Major, FindingStatus.InProgress, FindingDepartment.HR, FindingRaisedBy.ComplianceTeam, now.AddDays(-7)),
            New(organizationId, "Water quality testing schedule not followed", "FAC-3", FindingSeverity.Minor, FindingStatus.Verified, FindingDepartment.Admin, FindingRaisedBy.InternalAudit, now.AddDays(-15)),
            New(organizationId, "Anesthesia machine check list not signed", "OP-6", FindingSeverity.Critical, FindingStatus.Open, FindingDepartment.Clinical, FindingRaisedBy.InternalAudit, now.AddDays(-1)),
            New(organizationId, "Diesel tank bund wall crack observed", "FSE-1", FindingSeverity.Major, FindingStatus.Verified, FindingDepartment.Security, FindingRaisedBy.ExternalAudit, now.AddDays(-11)),
        };

        foreach (var r in seeds)
        {
            r.EvidenceCount = Random.Shared.Next(0, 4);
            r.Description = "Sample compliance finding for demo. Replace with repository-backed data.";
            r.CreatedAt = r.DateRaised;
            r.LastUpdatedAt = r.DateRaised;
            r.CreatedBy = u1;
            r.LastUpdatedBy = u1;
        }

        _items.AddRange(seeds);
        _seededOrgs.Add(organizationId);
    }

    private static ManagedFindingRecord New(
        Guid org,
        string title,
        string clause,
        FindingSeverity sev,
        FindingStatus st,
        FindingDepartment dept,
        FindingRaisedBy raised,
        DateTime dateRaised)
    {
        return new ManagedFindingRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = org,
            Title = title,
            ClauseReference = clause,
            Severity = sev,
            Status = st,
            Department = dept,
            RaisedBy = raised,
            DateRaised = dateRaised,
        };
    }
}
