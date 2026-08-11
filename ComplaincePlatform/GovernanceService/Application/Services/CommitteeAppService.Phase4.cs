using GovernanceService.Application.Contracts;
using GovernanceService.Domain.Entities;
using GovernanceService.Domain.Enums;
using GovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GovernanceService.Application.Services;

/// <summary>Phase 4 Sprint 5/6 — committee meetings, MR, action items, KPIs.</summary>
public partial class CommitteeAppService
{
    public async Task<CommitteeMeetingDto> ScheduleMeetingAsync(Guid committeeId, ScheduleMeetingRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        _ = await db.Committees.FirstOrDefaultAsync(c => c.Id == committeeId && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("Committee not found.");
        var meeting = CommitteeMeeting.Create(committeeId, request.ScheduledAt, request.MeetingTypeValueId, request.Agenda, request.AgendaTemplateCode);
        await db.CommitteeMeetings.AddAsync(meeting, ct);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("CommitteeMeeting", meeting.Id, "Scheduled", actorUserId, ct);
        return MapMeeting(meeting);
    }

    public async Task<IReadOnlyList<CommitteeMeetingDto>> ListMeetingsAsync(Guid committeeId, CancellationToken ct = default)
    {
        var rows = await db.CommitteeMeetings.AsNoTracking()
            .Include(m => m.Decisions)
            .Include(m => m.ActionItems)
            .Where(m => m.CommitteeId == committeeId)
            .OrderByDescending(m => m.ScheduledAt)
            .ToListAsync(ct);
        return rows.Select(MapMeeting).ToList();
    }

    public async Task<CommitteeMeetingDto?> GetMeetingAsync(Guid committeeId, Guid meetingId, CancellationToken ct = default)
    {
        var meeting = await db.CommitteeMeetings.AsNoTracking()
            .Include(m => m.Decisions)
            .Include(m => m.ActionItems)
            .FirstOrDefaultAsync(m => m.Id == meetingId && m.CommitteeId == committeeId, ct);
        return meeting == null ? null : MapMeeting(meeting);
    }

    public async Task RecordAttendanceAsync(Guid committeeId, Guid meetingId, RecordAttendanceRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var meeting = await db.CommitteeMeetings.FirstOrDefaultAsync(m => m.Id == meetingId && m.CommitteeId == committeeId, ct)
            ?? throw new InvalidOperationException("Meeting not found.");
        var json = request.AttendanceJson;
        if (string.IsNullOrWhiteSpace(json) && request.Attendees.Count > 0)
            json = JsonSerializer.Serialize(request.Attendees);
        meeting.SetAttendance(json);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("CommitteeMeeting", meetingId, "AttendanceRecorded", actorUserId, ct);
    }

    public async Task ApproveMinutesAsync(Guid committeeId, Guid meetingId, Guid actorUserId, CancellationToken ct = default)
    {
        var meeting = await db.CommitteeMeetings.FirstOrDefaultAsync(m => m.Id == meetingId && m.CommitteeId == committeeId, ct)
            ?? throw new InvalidOperationException("Meeting not found.");
        meeting.ApproveMinutes(actorUserId);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = meetingId,
            OccurredOn = DateTime.UtcNow,
            Type = "CommitteeMinutesApproved",
            Payload = JsonSerializer.Serialize(new { committeeId, meetingId, actorUserId })
        });
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("CommitteeMeeting", meetingId, "MinutesApproved", actorUserId, ct);
    }

    public async Task<CommitteeDecisionDto> RecordDecisionAsync(Guid committeeId, Guid meetingId, RecordDecisionRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var meeting = await db.CommitteeMeetings.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == meetingId && m.CommitteeId == committeeId, ct)
            ?? throw new InvalidOperationException("Meeting not found.");
        if (meeting.Status == MeetingStatus.Completed && meeting.MinutesApproved)
            throw new InvalidOperationException("Cannot record decisions after minutes are approved.");

        var decision = CommitteeDecision.Create(
            meetingId, request.DecisionTypeValueId, request.Summary, request.GovernedResourceType, request.GovernedResourceId,
            request.OwnerAssigneeId, request.ImplementationStatus, request.DeficiencyId, request.RiskId, request.TaskId);
        db.CommitteeDecisions.Add(decision);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = meetingId,
            OccurredOn = DateTime.UtcNow,
            Type = "CommitteeDecisionRecorded",
            Payload = JsonSerializer.Serialize(new { meetingId, decision.Id, request.Summary, request.DeficiencyId, request.RiskId, request.TaskId })
        });
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("CommitteeDecision", decision.Id, "Recorded", actorUserId, ct);
        await timeline.AppendAsync(
            "CommitteeDecisionRecorded", request.Summary, request.GovernedResourceType, request.GovernedResourceId,
            actorUserId, null, new { meetingId, decision.Id, request.OwnerAssigneeId, request.DeficiencyId, request.RiskId, request.TaskId }, ct);
        return MapDecision(decision);
    }

    public async Task<CommitteeActionItemDto> CreateActionItemAsync(Guid committeeId, Guid meetingId, CreateActionItemRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var meeting = await db.CommitteeMeetings.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == meetingId && m.CommitteeId == committeeId, ct)
            ?? throw new InvalidOperationException("Meeting not found.");
        if (meeting.Status == MeetingStatus.Completed && meeting.MinutesApproved)
            throw new InvalidOperationException("Cannot add action items after minutes are approved.");

        var item = CommitteeActionItem.Create(
            meetingId, request.Title, request.Description, request.OwnerAssigneeId, request.DueDate, request.TaskId, request.DecisionId);
        db.CommitteeActionItems.Add(item);
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = meetingId,
            OccurredOn = DateTime.UtcNow,
            Type = "CommitteeActionItemCreated",
            Payload = JsonSerializer.Serialize(new { committeeId, meetingId, actionItemId = item.Id, item.TaskId, item.Title })
        });
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("CommitteeActionItem", item.Id, "Created", actorUserId, ct);
        await timeline.AppendAsync("CommitteeActionItemCreated", item.Title, "CommitteeActionItem", item.Id, actorUserId, null, new { meetingId, item.TaskId }, ct);
        return MapActionItem(item);
    }

    public async Task LinkActionItemTaskAsync(Guid committeeId, Guid meetingId, Guid actionItemId, LinkActionItemTaskRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var item = await db.CommitteeActionItems.FirstOrDefaultAsync(a => a.Id == actionItemId && a.CommitteeMeetingId == meetingId, ct)
            ?? throw new InvalidOperationException("Action item not found.");
        if (!await db.CommitteeMeetings.AnyAsync(m => m.Id == meetingId && m.CommitteeId == committeeId, ct))
            throw new InvalidOperationException("Meeting not found.");
        item.LinkTask(request.TaskId);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("CommitteeActionItem", actionItemId, "TaskLinked", actorUserId, ct);
    }

    public async Task UpdateActionItemStatusAsync(Guid committeeId, Guid meetingId, Guid actionItemId, UpdateActionItemStatusRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var item = await db.CommitteeActionItems.FirstOrDefaultAsync(a => a.Id == actionItemId && a.CommitteeMeetingId == meetingId, ct)
            ?? throw new InvalidOperationException("Action item not found.");
        if (!await db.CommitteeMeetings.AnyAsync(m => m.Id == meetingId && m.CommitteeId == committeeId, ct))
            throw new InvalidOperationException("Meeting not found.");
        if (!Enum.TryParse<CommitteeActionItemStatus>(request.Status, true, out var status))
            throw new InvalidOperationException("Invalid action item status.");
        item.UpdateStatus(status);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("CommitteeActionItem", actionItemId, "StatusUpdated", actorUserId, ct);
    }

    public async Task<IReadOnlyList<AgendaTemplateDto>> ListAgendaTemplatesAsync(CancellationToken ct = default)
    {
        var rows = await db.AgendaTemplates.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync(ct);
        return rows.Select(MapTemplate).ToList();
    }

    public async Task<ManagementReviewAgendaInputDto> GetManagementReviewAgendaAsync(Guid? committeeId, CancellationToken ct = default)
    {
        var template = await db.AgendaTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Code == "ManagementReview", ct);
        var sections = ParseSections(template?.SectionsJson);
        return new ManagementReviewAgendaInputDto
        {
            CommitteeId = committeeId,
            TemplateCode = "ManagementReview",
            AgendaSections = sections.Count > 0 ? sections : new[]
            {
                "Quality policy and objectives",
                "Incident / patient safety summary",
                "CAPA / deficiency status",
                "Risk register review",
                "Internal / external audit outcomes",
                "Resource needs and decisions"
            },
            Incidents = new ManagementReviewSummaryBlockDto
            {
                Title = "Incidents",
                Highlights = new[] { "Open incident count — enrich via Gateway/Compliance" },
                IsPlaceholder = true
            },
            Capas = new ManagementReviewSummaryBlockDto
            {
                Title = "CAPAs",
                Highlights = new[] { "Open CAPA / deficiency count — enrich via Gateway/Compliance" },
                IsPlaceholder = true
            },
            Risks = new ManagementReviewSummaryBlockDto
            {
                Title = "Risks",
                Highlights = new[] { "Open risk count — enrich via Gateway/Compliance" },
                IsPlaceholder = true
            },
            Audits = new ManagementReviewSummaryBlockDto
            {
                Title = "Audits",
                Highlights = new[] { "Recent audit sessions — enrich via Gateway/Compliance" },
                IsPlaceholder = true
            },
        };
    }

    internal static CommitteeMeetingDto MapMeeting(CommitteeMeeting m) => new()
    {
        Id = m.Id,
        CommitteeId = m.CommitteeId,
        ScheduledAt = m.ScheduledAt,
        MeetingTypeValueId = m.MeetingTypeValueId,
        Agenda = m.Agenda,
        AgendaTemplateCode = m.AgendaTemplateCode,
        Status = m.Status.ToString(),
        Minutes = m.Minutes,
        AttendanceJson = m.AttendanceJson,
        MinutesApproved = m.MinutesApproved,
        MinutesApprovedAt = m.MinutesApprovedAt,
        CompletedAt = m.CompletedAt,
        Decisions = m.Decisions.Select(MapDecision).ToList(),
        ActionItems = m.ActionItems.Select(MapActionItem).ToList()
    };

    internal static CommitteeDecisionDto MapDecision(CommitteeDecision d) => new()
    {
        Id = d.Id,
        DecisionTypeValueId = d.DecisionTypeValueId,
        Summary = d.Summary,
        GovernedResourceType = d.GovernedResourceType,
        GovernedResourceId = d.GovernedResourceId,
        DeficiencyId = d.DeficiencyId,
        RiskId = d.RiskId,
        TaskId = d.TaskId,
        OwnerAssigneeId = d.OwnerAssigneeId,
        ImplementationStatus = d.ImplementationStatus,
        RecordedAt = d.RecordedAt
    };

    internal static CommitteeActionItemDto MapActionItem(CommitteeActionItem a) => new()
    {
        Id = a.Id,
        CommitteeMeetingId = a.CommitteeMeetingId,
        Title = a.Title,
        Description = a.Description,
        OwnerAssigneeId = a.OwnerAssigneeId,
        DueDate = a.DueDate,
        Status = a.Status.ToString(),
        TaskId = a.TaskId,
        DecisionId = a.DecisionId,
        CreatedAt = a.CreatedAt
    };

    internal static AgendaTemplateDto MapTemplate(AgendaTemplate t) => new()
    {
        Id = t.Id,
        Code = t.Code,
        Name = t.Name,
        Description = t.Description,
        Sections = ParseSections(t.SectionsJson),
        IsManagementReview = t.IsManagementReview
    };

    internal static IReadOnlyList<string> ParseSections(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch { return Array.Empty<string>(); }
    }
}
