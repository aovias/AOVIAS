using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class IncidentCommandService : IIncidentCommandService
{
    private readonly ComplianceDbContext _db;
    private readonly IComplianceNotificationPublisher _notifications;
    private readonly ILogger<IncidentCommandService> _logger;

    public IncidentCommandService(
        ComplianceDbContext db,
        IComplianceNotificationPublisher notifications,
        ILogger<IncidentCommandService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<IncidentDetailDto> CreateAsync(
        Guid organizationId, Guid? hospitalId, Guid userId,
        CreateIncidentRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var year = DateTime.UtcNow.Year;
        var seq = await _db.Incidents.CountAsync(i => i.TenantId == organizationId && i.CreatedAt.Year == year, ct);
        var severity = ParseEnum(request.Severity, IncidentSeverity.Moderate);

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            HospitalId = hospitalId,
            IncidentNumber = $"INC-{year}-{(seq + 1):D5}",
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            IncidentType = ParseEnum(request.IncidentType, IncidentType.AdverseEvent),
            Severity = severity,
            Priority = ParseEnum(request.Priority, IncidentPriority.Routine),
            Category = ParseEnum(request.Category, IncidentCategory.Other),
            Location = request.Location,
            Department = ParseEnum(request.Department, FindingDepartment.Clinical),
            ReporterUserId = request.IsAnonymous ? null : userId,
            ReporterName = request.IsAnonymous ? null : "Reporter",
            IsAnonymous = request.IsAnonymous,
            PatientInvolved = request.PatientInvolved,
            StaffInvolvedJson = request.StaffInvolvedJson,
            IncidentDateTime = request.IncidentDateTime ?? DateTime.UtcNow,
            Status = IncidentStatus.Reported,
            InvestigationStatus = InvestigationStatus.NotStarted,
            RcaRequired = request.RcaRequired || severity is IncidentSeverity.Critical or IncidentSeverity.Sentinel,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            LastUpdatedByUserId = userId,
            RowVersion = new byte[8],
        };

        incident.Timeline.Add(new IncidentTimelineEntry
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            OccurredAt = DateTime.UtcNow,
            EventType = "Reported",
            Description = "Incident reported",
            ActorUserId = userId,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        });

        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync(ct);

        if (IsHighSeverity(severity))
            await NotifyHighSeverityAsync(incident, ct);

        _logger.LogInformation("Incident {Number} created for org {OrgId}", incident.IncidentNumber, organizationId);
        return (await new IncidentQueryService(_db).GetByIdAsync(organizationId, incident.Id, ct))!;
    }

    public async Task<IncidentDetailDto> PatchAsync(
        Guid organizationId, Guid id, Guid userId,
        PatchIncidentRequestDto request, CancellationToken ct = default)
    {
        var incident = await _db.Incidents
            .Include(i => i.Timeline)
            .Include(i => i.Attachments)
            .Include(i => i.Investigation)
            .Include(i => i.RootCauseAnalysis)
            .FirstOrDefaultAsync(i => i.TenantId == organizationId && i.Id == id, ct)
            ?? throw new InvalidOperationException("Incident not found.");

        // Allow only status reopen transitions? Spec: closed immutable on mutate — reject all patches when closed/cancelled.
        incident.EnsureMutable();

        var previousSeverity = incident.Severity;

        if (!string.IsNullOrWhiteSpace(request.Title)) incident.Title = request.Title.Trim();
        if (request.Description != null) incident.Description = request.Description.Trim();
        if (!string.IsNullOrWhiteSpace(request.Severity) && Enum.TryParse<IncidentSeverity>(request.Severity, true, out var sev))
            incident.Severity = sev;
        if (!string.IsNullOrWhiteSpace(request.Priority) && Enum.TryParse<IncidentPriority>(request.Priority, true, out var pri))
            incident.Priority = pri;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<IncidentStatus>(request.Status, true, out var st))
        {
            incident.Status = st;
            if (st == IncidentStatus.Closed)
            {
                incident.ClosedAt = DateTime.UtcNow;
                incident.ClosedByUserId = userId;
            }
        }
        if (request.OwnerName != null) incident.OwnerName = request.OwnerName;
        if (request.OwnerUserId.HasValue) incident.OwnerUserId = request.OwnerUserId;
        if (request.RcaRequired.HasValue) incident.RcaRequired = request.RcaRequired.Value;

        incident.LastUpdatedAt = DateTime.UtcNow;
        incident.LastUpdatedByUserId = userId;

        incident.Timeline.Add(new IncidentTimelineEntry
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            OccurredAt = DateTime.UtcNow,
            EventType = "Updated",
            Description = "Incident updated",
            ActorUserId = userId,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        });

        await _db.SaveChangesAsync(ct);

        if (incident.Severity != previousSeverity && IsHighSeverity(incident.Severity))
            await NotifyHighSeverityAsync(incident, ct);

        return (await new IncidentQueryService(_db).GetByIdAsync(organizationId, id, ct))!;
    }

    public async Task<IncidentInvestigationDto> SaveInvestigationAsync(
        Guid organizationId, Guid userId, SaveInvestigationRequestDto request, CancellationToken ct = default)
    {
        var incident = await _db.Incidents
            .Include(i => i.Investigation)
            .FirstOrDefaultAsync(i => i.TenantId == organizationId && i.Id == request.IncidentId, ct)
            ?? throw new InvalidOperationException("Incident not found.");

        incident.EnsureMutable();

        if (incident.Investigation == null)
        {
            incident.Investigation = new IncidentInvestigation
            {
                Id = Guid.NewGuid(),
                IncidentId = incident.Id,
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[8],
            };
            _db.IncidentInvestigations.Add(incident.Investigation);
        }

        var inv = incident.Investigation;
        inv.IncidentSummary = request.IncidentSummary;
        inv.ContributingFactorsJson = request.ContributingFactorsJson;
        inv.PeopleFactorsJson = request.PeopleFactorsJson;
        inv.EnvironmentFactorsJson = request.EnvironmentFactorsJson;
        inv.EquipmentFactorsJson = request.EquipmentFactorsJson;
        inv.ProcessFactorsJson = request.ProcessFactorsJson;
        inv.CommunicationFactorsJson = request.CommunicationFactorsJson;
        inv.TimelineJson = request.TimelineJson;
        inv.Recommendations = request.Recommendations;
        inv.ReviewersJson = request.ReviewersJson;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<InvestigationStatus>(request.Status, true, out var st))
            inv.Status = st;
        inv.LastUpdatedAt = DateTime.UtcNow;

        incident.InvestigationStatus = inv.Status;
        incident.Status = IncidentStatus.UnderInvestigation;
        incident.LastUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new IncidentInvestigationDto
        {
            Id = inv.Id,
            IncidentSummary = inv.IncidentSummary,
            ContributingFactorsJson = inv.ContributingFactorsJson,
            PeopleFactorsJson = inv.PeopleFactorsJson,
            EnvironmentFactorsJson = inv.EnvironmentFactorsJson,
            EquipmentFactorsJson = inv.EquipmentFactorsJson,
            ProcessFactorsJson = inv.ProcessFactorsJson,
            CommunicationFactorsJson = inv.CommunicationFactorsJson,
            TimelineJson = inv.TimelineJson,
            Recommendations = inv.Recommendations,
            ReviewersJson = inv.ReviewersJson,
            Status = inv.Status.ToString(),
            ApprovedAt = inv.ApprovedAt,
        };
    }

    public async Task<IncidentRcaDto> SaveRcaAsync(
        Guid organizationId, Guid userId, SaveRcaRequestDto request, CancellationToken ct = default)
    {
        var incident = await _db.Incidents
            .Include(i => i.RootCauseAnalysis)
            .FirstOrDefaultAsync(i => i.TenantId == organizationId && i.Id == request.IncidentId, ct)
            ?? throw new InvalidOperationException("Incident not found.");

        incident.EnsureMutable();

        if (incident.RootCauseAnalysis == null)
        {
            incident.RootCauseAnalysis = new IncidentRca
            {
                Id = Guid.NewGuid(),
                IncidentId = incident.Id,
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[8],
            };
            _db.IncidentRcas.Add(incident.RootCauseAnalysis);
        }

        var rca = incident.RootCauseAnalysis;
        rca.Method = ParseEnum(request.Method, RcaMethod.FiveWhys);
        IncidentRcaMapper.ApplySaveRequest(rca, request);
        rca.LastUpdatedAt = DateTime.UtcNow;
        rca.CompletedByUserId = userId;

        incident.Status = IncidentStatus.RcaInProgress;
        incident.LastUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return IncidentRcaMapper.ToDto(rca);
    }

    public async Task<PatientFeedbackDto> CreatePatientFeedbackAsync(
        Guid organizationId, Guid? hospitalId, CreatePatientFeedbackRequestDto request, CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var seq = await _db.PatientFeedbacks.CountAsync(f => f.TenantId == organizationId && f.CreatedAt.Year == year, ct);

        var feedback = new PatientFeedback
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            HospitalId = hospitalId,
            FeedbackNumber = $"PFB-{year}-{(seq + 1):D5}",
            FeedbackType = ParseEnum(request.FeedbackType, PatientFeedbackType.Feedback),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            PatientName = request.PatientName,
            ContactInfo = request.ContactInfo,
            Department = request.Department,
            LinkedIncidentId = request.LinkedIncidentId,
            Status = "Open",
            ReceivedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };

        _db.PatientFeedbacks.Add(feedback);
        await _db.SaveChangesAsync(ct);

        return new PatientFeedbackDto
        {
            Id = feedback.Id,
            FeedbackNumber = feedback.FeedbackNumber,
            FeedbackType = feedback.FeedbackType.ToString(),
            Title = feedback.Title,
            Description = feedback.Description,
            PatientName = feedback.PatientName,
            Department = feedback.Department,
            LinkedIncidentId = feedback.LinkedIncidentId,
            Status = feedback.Status,
            ReceivedAt = feedback.ReceivedAt,
        };
    }

    public async Task<LessonLearnedDto> CreateLessonLearnedAsync(
        Guid organizationId, Guid? hospitalId, Guid userId,
        CreateLessonLearnedRequestDto request, CancellationToken ct = default)
    {
        var incident = await _db.Incidents.FirstOrDefaultAsync(
            i => i.TenantId == organizationId && i.Id == request.IncidentId, ct)
            ?? throw new InvalidOperationException("Incident not found.");

        if (incident.Status != IncidentStatus.Closed)
            throw new InvalidOperationException("Lessons learned can only be created from closed incidents.");

        var year = DateTime.UtcNow.Year;
        var seq = await _db.LessonsLearned.CountAsync(l => l.TenantId == organizationId && l.CreatedAt.Year == year, ct);

        var lesson = new LessonLearned
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            HospitalId = hospitalId,
            SourceIncidentId = incident.Id,
            LessonNumber = $"LL-{year}-{(seq + 1):D5}",
            Summary = request.Summary.Trim(),
            Recommendation = request.Recommendation.Trim(),
            DepartmentsJson = request.DepartmentsJson,
            RelatedSopsJson = request.RelatedSopsJson,
            TrainingNeeded = request.TrainingNeeded,
            PreventiveActions = request.PreventiveActions,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };

        _db.LessonsLearned.Add(lesson);
        await _db.SaveChangesAsync(ct);

        return new LessonLearnedDto
        {
            Id = lesson.Id,
            LessonNumber = lesson.LessonNumber,
            Summary = lesson.Summary,
            Recommendation = lesson.Recommendation,
            DepartmentsJson = lesson.DepartmentsJson,
            RelatedSopsJson = lesson.RelatedSopsJson,
            TrainingNeeded = lesson.TrainingNeeded,
            PreventiveActions = lesson.PreventiveActions,
            SourceIncidentId = lesson.SourceIncidentId,
            SourceIncidentNumber = incident.IncidentNumber,
            PublishedAt = lesson.PublishedAt,
        };
    }

    private async Task NotifyHighSeverityAsync(Incident incident, CancellationToken ct)
    {
        await _notifications.PublishAsync(new ComplianceNotificationMessage
        {
            EventType = "HighSeverityIncident",
            Title = $"High severity incident {incident.IncidentNumber}",
            Body = $"{incident.Severity}: {incident.Title}",
            HospitalId = incident.HospitalId,
            OrganizationId = incident.TenantId,
            RecipientUserId = incident.OwnerUserId?.ToString(),
            Data = new Dictionary<string, string>
            {
                ["incidentId"] = incident.Id.ToString(),
                ["incidentNumber"] = incident.IncidentNumber,
                ["severity"] = incident.Severity.ToString(),
                ["department"] = incident.Department.ToString(),
            },
        }, ct);
    }

    private static bool IsHighSeverity(IncidentSeverity severity) =>
        severity is IncidentSeverity.Major or IncidentSeverity.Critical or IncidentSeverity.Sentinel;

    private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, true, out var parsed) ? parsed : fallback;
}
