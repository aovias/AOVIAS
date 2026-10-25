using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Utilities;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class AuditScheduleService : IAuditScheduleService
{
    private readonly ComplianceDbContext _db;

    public AuditScheduleService(ComplianceDbContext db) => _db = db;

    public async Task<IReadOnlyList<ScheduledAuditDto>> ListAsync(
        Guid organizationId, AuditCalendarQuery query, CancellationToken ct = default)
    {
        var q = ApplyFilters(_db.ScheduledAudits.AsNoTracking().Where(a => a.OrganizationId == organizationId), query);
        var rows = await q.OrderBy(a => a.ScheduledDate).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public Task<IReadOnlyList<ScheduledAuditDto>> GetCalendarAsync(
        Guid organizationId, AuditCalendarQuery query, CancellationToken ct = default)
    {
        var calendarQuery = query;
        if (!calendarQuery.From.HasValue && !calendarQuery.To.HasValue && !calendarQuery.Year.HasValue)
        {
            var now = DateTime.UtcNow;
            calendarQuery = query with
            {
                From = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                To = new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            };
        }

        return ListAsync(organizationId, calendarQuery, ct);
    }

    public async Task<ScheduledAuditDto> CreateAsync(
        Guid organizationId, Guid hospitalId, CreateScheduledAuditRequest request, CancellationToken ct = default)
    {
        if (request.FrameworkId == Guid.Empty)
            throw new ArgumentException("FrameworkId is required.");
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var hospital = request.HospitalId is { } hid && hid != Guid.Empty ? hid : hospitalId;
        if (hospital == Guid.Empty)
            throw new ArgumentException("HospitalId is required.");

        var scheduled = request.ScheduledDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.ScheduledDate, DateTimeKind.Utc)
            : request.ScheduledDate.ToUniversalTime();

        var year = request.ProgramYear ?? scheduled.Year;
        var month = request.ProgramMonth ?? scheduled.Month;
        if (month is < 1 or > 12)
            throw new ArgumentException("ProgramMonth must be between 1 and 12.");

        var entity = new ScheduledAudit
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            OrganizationId = organizationId,
            HospitalId = hospital,
            FrameworkId = request.FrameworkId,
            ComplianceJourneyId = request.ComplianceJourneyId,
            Title = request.Title.Trim(),
            ScheduledDate = scheduled,
            Status = ScheduledAuditStatus.Planned,
            AuditorName = request.AuditorName?.Trim(),
            ProgramYear = year,
            ProgramMonth = month,
            AuditType = request.AuditType,
            ProgramName = request.ProgramName?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        _db.ScheduledAudits.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    private static IQueryable<ScheduledAudit> ApplyFilters(IQueryable<ScheduledAudit> q, AuditCalendarQuery query)
    {
        if (query.HospitalId is { } hospitalId && hospitalId != Guid.Empty)
            q = q.Where(a => a.HospitalId == hospitalId);

        if (query.FrameworkId is { } frameworkId && frameworkId != Guid.Empty)
            q = q.Where(a => a.FrameworkId == frameworkId);

        if (query.Year is { } year)
            q = q.Where(a => (a.ProgramYear ?? a.ScheduledDate.Year) == year);

        if (query.Month is { } month)
            q = q.Where(a => (a.ProgramMonth ?? a.ScheduledDate.Month) == month);

        if (query.From is { } from)
            q = q.Where(a => a.ScheduledDate >= from.ToUniversalTime());

        if (query.To is { } to)
            q = q.Where(a => a.ScheduledDate <= to.ToUniversalTime());

        return q;
    }

    private static ScheduledAuditDto Map(ScheduledAudit a) => new()
    {
        Id = a.Id,
        OrganizationId = a.OrganizationId,
        HospitalId = a.HospitalId,
        FrameworkId = a.FrameworkId,
        ComplianceJourneyId = a.ComplianceJourneyId,
        Title = a.Title,
        ScheduledDate = a.ScheduledDate,
        Status = a.Status,
        AuditorName = a.AuditorName,
        Result = a.Result,
        ProgramYear = a.ProgramYear ?? a.ScheduledDate.Year,
        ProgramMonth = a.ProgramMonth ?? a.ScheduledDate.Month,
        AuditType = a.AuditType,
        AuditTypeLabel = a.AuditType.HasValue ? AuditTypeMapping.ToCommercialLabel(a.AuditType.Value) : null,
        ProgramName = a.ProgramName,
    };
}
