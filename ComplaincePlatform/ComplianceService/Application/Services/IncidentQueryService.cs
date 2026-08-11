using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class IncidentQueryService : IIncidentQueryService
{
    private readonly ComplianceDbContext _db;

    public IncidentQueryService(ComplianceDbContext db) => _db = db;

    public async Task<IncidentDashboardDto> GetDashboardAsync(Guid organizationId, CancellationToken ct = default)
    {
        var rows = await _db.Incidents.AsNoTracking()
            .Where(i => i.TenantId == organizationId)
            .Select(i => new
            {
                i.Status,
                i.Severity,
                i.IncidentType,
                i.Category,
                i.Department,
                i.InvestigationStatus,
                i.RcaRequired,
                i.ClosedAt,
                i.CreatedAt,
                i.DeficiencyId,
                i.CapaId,
                i.Title,
            })
            .ToListAsync(ct);

        var dto = new IncidentDashboardDto();
        var openStatuses = new[] { IncidentStatus.Reported, IncidentStatus.UnderInvestigation, IncidentStatus.RcaInProgress, IncidentStatus.PendingReview };

        foreach (var r in rows)
        {
            if (openStatuses.Contains(r.Status)) dto.OpenIncidents++;
            if (r.Severity is IncidentSeverity.Critical or IncidentSeverity.Sentinel) dto.CriticalIncidents++;
            if (r.IncidentType == IncidentType.NearMiss || r.Category == IncidentCategory.NearMiss) dto.NearMisses++;
            if (r.IncidentType == IncidentType.SentinelEvent || r.Category == IncidentCategory.SentinelEvent) dto.SentinelEvents++;
            if (r.InvestigationStatus == InvestigationStatus.InProgress) dto.InvestigationsInProgress++;
            if (r.RcaRequired && r.Status != IncidentStatus.Closed) dto.RcaPending++;
        }

        var titleGroups = rows.GroupBy(r => r.Title.Trim().ToLowerInvariant()).Where(g => g.Count() > 1);
        dto.RepeatedIncidents = titleGroups.Sum(g => g.Count());

        var deptRisk = rows.Where(r => openStatuses.Contains(r.Status))
            .GroupBy(r => r.Department).Count(g => g.Count() >= 3);
        dto.DepartmentsAtRisk = deptRisk;

        var closed = rows.Where(r => r.ClosedAt.HasValue).ToList();
        dto.AverageClosureDays = closed.Count > 0
            ? Math.Round((decimal)closed.Average(r => (r.ClosedAt!.Value - r.CreatedAt).TotalDays), 1)
            : 0;

        // CAPA SSOT = ComplianceDeficiency linked via DeficiencyId (legacy CapaId ignored for KPIs).
        dto.OpenCapas = rows.Count(r => r.DeficiencyId.HasValue && r.Status != IncidentStatus.Closed);

        var total = Math.Max(1, rows.Count);
        var closedCount = rows.Count(r => r.Status == IncidentStatus.Closed);
        var criticalOpen = rows.Count(r => openStatuses.Contains(r.Status) && r.Severity is IncidentSeverity.Critical or IncidentSeverity.Sentinel);
        dto.PatientSafetyScore = Math.Round(Math.Max(0, 100 - (criticalOpen * 15) - (dto.OpenIncidents * 2m) + (closedCount * 0.5m)), 1);

        dto.ByCategory = rows.GroupBy(r => r.Category.ToString())
            .Select(g => new IncidentCountByLabelDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();

        dto.ByDepartment = rows.GroupBy(r => r.Department.ToString())
            .Select(g => new IncidentCountByLabelDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();

        dto.BySeverity = rows.GroupBy(r => r.Severity.ToString())
            .Select(g => new IncidentCountByLabelDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();

        dto.Trend = rows.GroupBy(r => r.CreatedAt.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new IncidentTrendPointDto { Period = g.Key, Count = g.Count() })
            .TakeLast(12)
            .ToList();

        return dto;
    }

    public async Task<IncidentListDto> GetIncidentsAsync(
        Guid organizationId, string? status, string? severity, string? category,
        string? department, string? incidentType, string? quickFilter, string? search,
        string? sortBy, bool sortDesc, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Incidents.AsNoTracking().Where(i => i.TenantId == organizationId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<IncidentStatus>(status, true, out var st))
            query = query.Where(i => i.Status == st);
        if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<IncidentSeverity>(severity, true, out var sev))
            query = query.Where(i => i.Severity == sev);
        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<IncidentCategory>(category, true, out var cat))
            query = query.Where(i => i.Category == cat);
        if (!string.IsNullOrWhiteSpace(department) && Enum.TryParse<FindingDepartment>(department, true, out var dept))
            query = query.Where(i => i.Department == dept);
        if (!string.IsNullOrWhiteSpace(incidentType) && Enum.TryParse<IncidentType>(incidentType, true, out var it))
            query = query.Where(i => i.IncidentType == it);

        if (quickFilter == "critical")
            query = query.Where(i => i.Severity == IncidentSeverity.Critical || i.Severity == IncidentSeverity.Sentinel);
        else if (quickFilter == "nearMiss")
            query = query.Where(i => i.IncidentType == IncidentType.NearMiss || i.Category == IncidentCategory.NearMiss);
        else if (quickFilter == "sentinel")
            query = query.Where(i => i.IncidentType == IncidentType.SentinelEvent || i.Category == IncidentCategory.SentinelEvent);
        else if (quickFilter == "open")
            query = query.Where(i => i.Status != IncidentStatus.Closed && i.Status != IncidentStatus.Cancelled);
        else if (quickFilter == "rcaPending")
            query = query.Where(i => i.RcaRequired && i.Status != IncidentStatus.Closed);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(i => i.Title.ToLower().Contains(s) || i.IncidentNumber.ToLower().Contains(s)
                || i.Description.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        query = sortBy?.ToLowerInvariant() switch
        {
            "severity" => sortDesc ? query.OrderByDescending(i => i.Severity) : query.OrderBy(i => i.Severity),
            "department" => sortDesc ? query.OrderByDescending(i => i.Department) : query.OrderBy(i => i.Department),
            "status" => sortDesc ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status),
            _ => sortDesc ? query.OrderByDescending(i => i.IncidentDateTime) : query.OrderBy(i => i.IncidentDateTime),
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => MapListItem(i))
            .ToListAsync(ct);

        return new IncidentListDto { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<IncidentDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct = default)
    {
        var incident = await _db.Incidents.AsNoTracking()
            .Include(i => i.Timeline)
            .Include(i => i.Attachments)
            .Include(i => i.Investigation)
            .Include(i => i.RootCauseAnalysis)
            .FirstOrDefaultAsync(i => i.TenantId == organizationId && i.Id == id, ct);

        if (incident == null) return null;

        var dto = MapDetail(incident);
        dto.RelatedDocumentation = await LoadDocumentationLinksAsync(incident.RelatedDocumentationJson, ct);
        return dto;
    }

    public async Task<IncidentAnalyticsDto> GetAnalyticsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var dashboard = await GetDashboardAsync(organizationId, ct);
        var lessonsCount = await _db.LessonsLearned.CountAsync(l => l.TenantId == organizationId, ct);
        var withCapa = await _db.Incidents.CountAsync(i => i.TenantId == organizationId && i.DeficiencyId != null, ct);
        var total = Math.Max(1, await _db.Incidents.CountAsync(i => i.TenantId == organizationId, ct));

        return new IncidentAnalyticsDto
        {
            IncidentTrends = dashboard.Trend,
            SeverityDistribution = dashboard.BySeverity,
            DepartmentHeatmap = dashboard.ByDepartment,
            NearMissTrends = dashboard.Trend,
            RepeatIncidents = dashboard.RepeatedIncidents,
            AverageResolutionDays = dashboard.AverageClosureDays,
            LessonsLearnedCount = lessonsCount,
            CapaConversionRate = Math.Round((decimal)withCapa / total * 100, 1),
            DocumentationRevisionRate = 0,
        };
    }

    public async Task<IReadOnlyList<PatientFeedbackDto>> GetPatientFeedbackAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await _db.PatientFeedbacks.AsNoTracking()
            .Where(f => f.TenantId == organizationId)
            .OrderByDescending(f => f.ReceivedAt)
            .Select(f => new PatientFeedbackDto
            {
                Id = f.Id,
                FeedbackNumber = f.FeedbackNumber,
                FeedbackType = f.FeedbackType.ToString(),
                Title = f.Title,
                Description = f.Description,
                PatientName = f.PatientName,
                Department = f.Department,
                LinkedIncidentId = f.LinkedIncidentId,
                Status = f.Status,
                ReceivedAt = f.ReceivedAt,
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LessonLearnedDto>> GetLessonsLearnedAsync(Guid organizationId, string? search, CancellationToken ct = default)
    {
        var query = _db.LessonsLearned.AsNoTracking()
            .Where(l => l.TenantId == organizationId && l.IsPublished);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(l => l.Summary.ToLower().Contains(s) || l.Recommendation.ToLower().Contains(s));
        }

        return await query.OrderByDescending(l => l.PublishedAt)
            .Select(l => new LessonLearnedDto
            {
                Id = l.Id,
                LessonNumber = l.LessonNumber,
                Summary = l.Summary,
                Recommendation = l.Recommendation,
                DepartmentsJson = l.DepartmentsJson,
                RelatedSopsJson = l.RelatedSopsJson,
                TrainingNeeded = l.TrainingNeeded,
                PreventiveActions = l.PreventiveActions,
                SourceIncidentId = l.SourceIncidentId,
                PublishedAt = l.PublishedAt,
            })
            .ToListAsync(ct);
    }

    private static IncidentListItemDto MapListItem(Incident i) => new()
    {
        Id = i.Id,
        IncidentNumber = i.IncidentNumber,
        Title = i.Title,
        Description = i.Description,
        IncidentType = i.IncidentType.ToString(),
        Severity = i.Severity.ToString(),
        Priority = i.Priority.ToString(),
        Category = i.Category.ToString(),
        Location = i.Location,
        Department = i.Department.ToString(),
        ReporterName = i.IsAnonymous ? "Anonymous" : i.ReporterName,
        IsAnonymous = i.IsAnonymous,
        PatientInvolved = i.PatientInvolved,
        IncidentDateTime = i.IncidentDateTime,
        Status = i.Status.ToString(),
        InvestigationStatus = i.InvestigationStatus.ToString(),
        RcaRequired = i.RcaRequired,
        OwnerName = i.OwnerName,
        FindingId = i.FindingId,
        DeficiencyId = i.DeficiencyId,
        CapaId = i.CapaId,
        CreatedDate = i.CreatedAt,
        LastUpdated = i.LastUpdatedAt,
        AttachmentCount = i.Attachments?.Count ?? 0,
        TimelineCount = i.Timeline?.Count ?? 0,
    };

    private static IncidentDetailDto MapDetail(Incident i)
    {
        var dto = new IncidentDetailDto
        {
            Id = i.Id,
            IncidentNumber = i.IncidentNumber,
            Title = i.Title,
            Description = i.Description,
            IncidentType = i.IncidentType.ToString(),
            Severity = i.Severity.ToString(),
            Priority = i.Priority.ToString(),
            Category = i.Category.ToString(),
            Location = i.Location,
            Department = i.Department.ToString(),
            ReporterName = i.IsAnonymous ? "Anonymous" : i.ReporterName,
            IsAnonymous = i.IsAnonymous,
            PatientInvolved = i.PatientInvolved,
            StaffInvolvedJson = i.StaffInvolvedJson,
            IncidentDateTime = i.IncidentDateTime,
            Status = i.Status.ToString(),
            InvestigationStatus = i.InvestigationStatus.ToString(),
            RcaRequired = i.RcaRequired,
            OwnerName = i.OwnerName,
            FindingId = i.FindingId,
            DeficiencyId = i.DeficiencyId,
            CapaId = i.CapaId,
            CreatedDate = i.CreatedAt,
            LastUpdated = i.LastUpdatedAt,
            AttachmentCount = i.Attachments.Count,
            TimelineCount = i.Timeline.Count,
            Timeline = i.Timeline.OrderByDescending(t => t.OccurredAt).Select(t => new IncidentTimelineEntryDto
            {
                Id = t.Id,
                OccurredAt = t.OccurredAt,
                EventType = t.EventType,
                Description = t.Description,
                ActorName = t.ActorName,
            }).ToList(),
            Attachments = i.Attachments.Select(a => new IncidentAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                FileUrl = a.FileUrl,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes,
                UploadedAt = a.CreatedAt,
            }).ToList(),
            Investigation = i.Investigation == null ? null : new IncidentInvestigationDto
            {
                Id = i.Investigation.Id,
                IncidentSummary = i.Investigation.IncidentSummary,
                ContributingFactorsJson = i.Investigation.ContributingFactorsJson,
                PeopleFactorsJson = i.Investigation.PeopleFactorsJson,
                EnvironmentFactorsJson = i.Investigation.EnvironmentFactorsJson,
                EquipmentFactorsJson = i.Investigation.EquipmentFactorsJson,
                ProcessFactorsJson = i.Investigation.ProcessFactorsJson,
                CommunicationFactorsJson = i.Investigation.CommunicationFactorsJson,
                TimelineJson = i.Investigation.TimelineJson,
                Recommendations = i.Investigation.Recommendations,
                ReviewersJson = i.Investigation.ReviewersJson,
                Status = i.Investigation.Status.ToString(),
                ApprovedAt = i.Investigation.ApprovedAt,
            },
            RootCauseAnalysis = i.RootCauseAnalysis == null ? null : IncidentRcaMapper.ToDto(i.RootCauseAnalysis),
        };
        return dto;
    }

    private async Task<IReadOnlyList<IncidentDocumentationLinkDto>> LoadDocumentationLinksAsync(string? json, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<IncidentDocumentationLinkDto>();
        try
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
            if (ids.Count == 0) return Array.Empty<IncidentDocumentationLinkDto>();

            return await _db.ControlledDocuments.AsNoTracking()
                .Where(d => ids.Contains(d.Id))
                .Select(d => new IncidentDocumentationLinkDto
                {
                    DocumentId = d.Id,
                    DocumentNumber = d.DocumentNumber,
                    Title = d.Title,
                    DocumentType = d.DocumentType.ToString(),
                    CurrentVersion = d.CurrentVersionLabel,
                    AcknowledgementPercent = d.Acknowledgements.Count(a => a.AcknowledgedAt != null) * 100m
                        / Math.Max(1, d.Acknowledgements.Count),
                })
                .ToListAsync(ct);
        }
        catch
        {
            return Array.Empty<IncidentDocumentationLinkDto>();
        }
    }
}
