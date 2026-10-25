using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Infrastructure.Repositories;

/// <summary>
/// Scoped, read-heavy data access for the Control Center plus idempotent population helpers for the
/// new derived tables (departments, scheduled audits, activity events, alerts).
/// </summary>
public sealed class ControlCenterRepository : IControlCenterRepository
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<ControlCenterRepository> _logger;

    public ControlCenterRepository(ComplianceDbContext db, ILogger<ControlCenterRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ControlCenterDataset> LoadDatasetAsync(ControlCenterScope scope, CancellationToken cancellationToken = default)
    {
        var hospitalIds = scope.HospitalIds.ToList();
        var frameworkId = scope.FrameworkId;

        var dataset = new ControlCenterDataset
        {
            TenantId = scope.TenantId,
            OrganizationId = scope.OrganizationId
        };

        if (hospitalIds.Count == 0)
        {
            return dataset;
        }

        // Journeys (with framework) in scope.
        var journeyQuery = _db.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.TenantId == scope.TenantId && hospitalIds.Contains(j.HospitalId));
        if (frameworkId.HasValue)
        {
            journeyQuery = journeyQuery.Where(j => j.FrameworkId == frameworkId.Value);
        }
        dataset.Journeys = await journeyQuery.Include(j => j.Framework).ToListAsync(cancellationToken);

        var scopeFrameworkIds = frameworkId.HasValue
            ? new List<Guid> { frameworkId.Value }
            : dataset.Journeys.Select(j => j.FrameworkId).Distinct().ToList();

        // Tasks (with standard -> chapter -> framework). AsNoTracking + bounded page for hot path.
        const int taskPageSize = 2000;
        var taskQuery = _db.ComplianceTasks
            .AsNoTracking()
            .Include(t => t.ComplianceStandard)
                .ThenInclude(s => s.Chapter)
                    .ThenInclude(c => c.Framework)
            .Where(t => hospitalIds.Contains(t.HospitalId));
        if (frameworkId.HasValue)
        {
            taskQuery = taskQuery.Where(t => t.ComplianceStandard != null &&
                                             t.ComplianceStandard.Chapter != null &&
                                             t.ComplianceStandard.Chapter.FrameworkId == frameworkId.Value);
        }
        dataset.Tasks = await taskQuery
            .OrderByDescending(t => t.CreatedAt)
            .Take(taskPageSize)
            .ToListAsync(cancellationToken);

        // Open findings for the tenant (filtered to scope hospital via journey when possible).
        var findingQuery = _db.Findings
            .AsNoTracking()
            .Include(f => f.ComplianceJourney)
            .Where(f => f.TenantId == scope.TenantId && !f.IsResolved);
        if (frameworkId.HasValue)
        {
            findingQuery = findingQuery.Where(f => f.ComplianceJourney == null || f.ComplianceJourney.FrameworkId == frameworkId.Value);
        }
        var findings = await findingQuery.OrderByDescending(f => f.DateRaised).Take(500).ToListAsync(cancellationToken);
        dataset.OpenFindings = findings
            .Where(f => f.ComplianceJourney == null || hospitalIds.Contains(f.ComplianceJourney.HospitalId))
            .ToList();

        // CAPA SSOT: deficiency-embedded CAPA fields (orphan Capas table no longer drives KPIs).
        var deficiencyQuery = _db.ComplianceDeficiencies
            .AsNoTracking()
            .Where(d => d.OrganizationId == scope.OrganizationId
                        && hospitalIds.Contains(d.HospitalId)
                        && d.Status != Domain.Enums.DeficiencyStatus.Closed
                        && d.Status != Domain.Enums.DeficiencyStatus.Cancelled);
        if (frameworkId.HasValue)
        {
            deficiencyQuery = deficiencyQuery.Where(d => d.FrameworkId == frameworkId.Value);
        }
        dataset.ActiveDeficiencies = await deficiencyQuery.ToListAsync(cancellationToken);
        dataset.Capas = new List<Capa>();

        // Chapters for the in-scope frameworks.
        if (scopeFrameworkIds.Count > 0)
        {
            dataset.Chapters = await _db.ComplianceChapters
                .AsNoTracking()
                .Where(c => scopeFrameworkIds.Contains(c.FrameworkId))
                .ToListAsync(cancellationToken);
        }

        // Department master for the organization.
        dataset.Departments = await _db.Departments
            .AsNoTracking()
            .Where(d => d.OrganizationId == scope.OrganizationId && d.IsActive)
            .ToListAsync(cancellationToken);

        // Recent evidence (for the activity stream projection).
        var evidenceQuery = _db.Evidences
            .AsNoTracking()
            .Where(e => hospitalIds.Contains(e.HospitalId));
        if (frameworkId.HasValue)
        {
            evidenceQuery = evidenceQuery.Where(e => e.FrameworkId == frameworkId.Value);
        }
        dataset.RecentEvidence = await evidenceQuery
            .OrderByDescending(e => e.CreatedAt)
            .Take(15)
            .ToListAsync(cancellationToken);

        // Existing derived tables (will be merged/refreshed by population).
        var auditQuery = _db.ScheduledAudits
            .AsNoTracking()
            .Where(a => a.TenantId == scope.TenantId && hospitalIds.Contains(a.HospitalId));
        if (frameworkId.HasValue)
        {
            auditQuery = auditQuery.Where(a => a.FrameworkId == frameworkId.Value);
        }
        dataset.ScheduledAudits = await auditQuery.ToListAsync(cancellationToken);

        dataset.Alerts = await _db.Alerts
            .AsNoTracking()
            .Where(a => a.TenantId == scope.TenantId &&
                        (a.HospitalId == null || hospitalIds.Contains(a.HospitalId.Value)))
            .ToListAsync(cancellationToken);

        dataset.Activities = await _db.ActivityEvents
            .AsNoTracking()
            .Where(a => a.TenantId == scope.TenantId &&
                        (a.HospitalId == null || hospitalIds.Contains(a.HospitalId.Value)))
            .OrderByDescending(a => a.OccurredAt)
            .Take(60)
            .ToListAsync(cancellationToken);

        // Compliance snapshots for trend + score deltas.
        var snapshotQuery = _db.ComplianceSnapshots
            .AsNoTracking()
            .Where(s => hospitalIds.Contains(s.HospitalId));
        if (frameworkId.HasValue)
        {
            snapshotQuery = snapshotQuery.Where(s => s.FrameworkId == frameworkId.Value);
        }
        dataset.Snapshots = await snapshotQuery
            .OrderByDescending(s => s.CalculatedAt)
            .Take(120)
            .ToListAsync(cancellationToken);

        // Evidence review posture from OE-evidence links scoped via the parent evidence.
        var oeEvidence = await (from link in _db.ObjectiveElementEvidences.AsNoTracking()
                                join e in _db.Evidences.AsNoTracking() on link.EvidenceId equals e.Id
                                where hospitalIds.Contains(e.HospitalId) &&
                                      (frameworkId == null || e.FrameworkId == frameworkId.Value)
                                select new { link.VerificationStatus, link.ObjectiveElementId })
                               .ToListAsync(cancellationToken);

        dataset.EvidenceApproved = oeEvidence.Count(x => x.VerificationStatus == EvidenceVerificationStatus.Verified);
        dataset.EvidencePending = oeEvidence.Count(x => x.VerificationStatus == EvidenceVerificationStatus.Pending);
        dataset.EvidenceRejected = oeEvidence.Count(x => x.VerificationStatus == EvidenceVerificationStatus.Rejected);
        dataset.ObjectiveElementsWithVerifiedEvidence = oeEvidence
            .Where(x => x.VerificationStatus == EvidenceVerificationStatus.Verified)
            .Select(x => x.ObjectiveElementId)
            .Distinct()
            .Count();

        // Scorable/applicable objective elements for the in-scope frameworks (coverage denominator).
        if (scopeFrameworkIds.Count > 0)
        {
            dataset.ScorableObjectiveElements = await _db.ObjectiveElements
                .AsNoTracking()
                .Where(oe => oe.IsScorable && oe.IsApplicable &&
                             oe.ComplianceStandard.Chapter != null &&
                             scopeFrameworkIds.Contains(oe.ComplianceStandard.Chapter.FrameworkId))
                .CountAsync(cancellationToken);
        }

        return dataset;
    }

    public Task<string> GetOrganizationNameAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => Task.FromResult(string.Empty);

    public async Task SeedDepartmentsAsync(ControlCenterScope scope, IReadOnlyCollection<string> departmentNames, CancellationToken cancellationToken = default)
    {
        if (scope.OrganizationId == Guid.Empty || departmentNames.Count == 0)
        {
            return;
        }

        var existingCodes = await _db.Departments
            .Where(d => d.OrganizationId == scope.OrganizationId)
            .Select(d => d.Code)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<ComplianceDepartment>();
        foreach (var name in departmentNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var code = Slug(name);
            if (existing.Add(code))
            {
                toAdd.Add(new ComplianceDepartment
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = scope.OrganizationId,
                    Code = code,
                    Name = name.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (toAdd.Count > 0)
        {
            _db.Departments.AddRange(toAdd);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpsertScheduledAuditsAsync(IReadOnlyCollection<ScheduledAudit> audits, CancellationToken cancellationToken = default)
    {
        if (audits.Count == 0)
        {
            return;
        }

        var ids = audits.Select(a => a.Id).ToList();
        var existing = await _db.ScheduledAudits
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        foreach (var audit in audits)
        {
            if (existing.TryGetValue(audit.Id, out var current))
            {
                current.ScheduledDate = audit.ScheduledDate;
                current.Title = audit.Title;
                current.Status = audit.Status;
                current.AuditorName = audit.AuditorName;
                current.Result = audit.Result;
                current.ProgramYear = audit.ProgramYear ?? audit.ScheduledDate.Year;
                current.ProgramMonth = audit.ProgramMonth ?? audit.ScheduledDate.Month;
                if (audit.AuditType.HasValue)
                    current.AuditType = audit.AuditType;
                if (!string.IsNullOrWhiteSpace(audit.ProgramName))
                    current.ProgramName = audit.ProgramName;
            }
            else
            {
                _db.ScheduledAudits.Add(audit);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertActivityEventsAsync(IReadOnlyCollection<ActivityEvent> events, CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return;
        }

        var ids = events.Select(e => e.Id).ToList();
        var existingIds = await _db.ActivityEvents
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<Guid>(existingIds);

        var toAdd = events.Where(e => !existing.Contains(e.Id)).ToList();
        if (toAdd.Count > 0)
        {
            _db.ActivityEvents.AddRange(toAdd);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpsertAlertsAsync(IReadOnlyCollection<Alert> alerts, CancellationToken cancellationToken = default)
    {
        if (alerts.Count == 0)
        {
            return;
        }

        var ids = alerts.Select(a => a.Id).ToList();
        var existing = await _db.Alerts
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        foreach (var alert in alerts)
        {
            if (existing.TryGetValue(alert.Id, out var current))
            {
                // Refresh live content but preserve acknowledgement state.
                current.Severity = alert.Severity;
                current.Title = alert.Title;
                current.Description = alert.Description;
                current.Metric = alert.Metric;
                current.FrameworkId = alert.FrameworkId;
            }
            else
            {
                _db.Alerts.Add(alert);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string Slug(string value)
    {
        var trimmed = (value ?? string.Empty).Trim().ToUpperInvariant();
        var chars = trimmed.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray();
        var cleaned = new string(chars).Replace(' ', '-');
        return cleaned.Length > 50 ? cleaned[..50] : (cleaned.Length == 0 ? "DEPT" : cleaned);
    }
}
