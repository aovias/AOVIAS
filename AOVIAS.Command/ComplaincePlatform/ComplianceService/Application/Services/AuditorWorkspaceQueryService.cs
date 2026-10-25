using System.Text.Json;
using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Application.Journey;
using ComplianceService.Application.Utilities;
using ComplianceService.Domain;
using ComplianceService.Domain.Auditor;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class AuditorWorkspaceQueryService : IAuditorWorkspaceQueryService
{
    private readonly ComplianceDbContext _db;
    private readonly IAnalyticsAggregationService _analytics;
    private readonly IComplianceEventEmitter _eventEmitter;

    public AuditorWorkspaceQueryService(
        ComplianceDbContext db,
        IAnalyticsAggregationService analytics,
        IComplianceEventEmitter eventEmitter)
    {
        _db = db;
        _analytics = analytics;
        _eventEmitter = eventEmitter;
    }

    public async Task<AuditorDashboardDto> GetDashboardAsync(Guid organizationId, Guid hospitalId, CancellationToken ct = default)
    {
        var sessions = await _db.Set<AuditSession>().AsNoTracking()
            .Where(s => s.TenantId == organizationId)
            .ToListAsync(ct);

        var activeSessions = sessions.Count(s =>
            s.Status is AuditSessionStatus.Planned or AuditSessionStatus.InProgress or AuditSessionStatus.Review);

        var sessionIds = sessions.Select(s => s.Id).ToList();

        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.TenantId == organizationId
                        && (f.AuditSessionId == null || sessionIds.Contains(f.AuditSessionId.Value)))
            .ToListAsync(ct);

        var sessionFindings = findings.Where(f => f.AuditSessionId.HasValue).ToList();
        var observations = await _db.Set<AuditObservation>().AsNoTracking()
            .Where(o => o.TenantId == organizationId)
            .CountAsync(ct);

        var evidenceReviewed = await _db.ObjectiveElementEvidences.AsNoTracking()
            .CountAsync(e => e.VerificationStatus == EvidenceVerificationStatus.Verified, ct);

        var oeReviewed = await _db.ObjectiveElementEvidences.AsNoTracking()
            .Select(e => e.ObjectiveElementId)
            .Distinct()
            .CountAsync(ct);

        var totalOes = await _db.ObjectiveElements.AsNoTracking().CountAsync(ct);
        var coverage = totalOes > 0 ? (double)oeReviewed / totalOes * 100.0 : 0;

        decimal readiness = 0;
        var journey = await GetActiveJourneyAsync(hospitalId, ct);
        if (journey != null)
        {
            var scope = AnalyticsScope.ForHospital(organizationId, hospitalId, journey.FrameworkId);
            var readinessDto = await _analytics.GetReadinessAsync(scope, ct);
            readiness = readinessDto.JourneyAccreditationReadiness;
        }

        var obsBySession = await _db.Set<AuditObservation>().AsNoTracking()
            .Where(o => o.TenantId == organizationId)
            .GroupBy(o => o.AuditSessionId)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var recent = sessions
            .OrderByDescending(s => s.CreatedAt)
            .Take(5)
            .Select(s =>
            {
                var sf = sessionFindings.Where(f => f.AuditSessionId == s.Id).ToList();
                var oc = obsBySession.FirstOrDefault(x => x.SessionId == s.Id)?.Count ?? 0;
                return MapSessionSummary(s, sf, oc);
            })
            .ToList();

        return new AuditorDashboardDto
        {
            ActiveSessions = activeSessions,
            AreasReviewed = oeReviewed,
            PendingReviews = await _db.ObjectiveElementEvidences.AsNoTracking()
                .CountAsync(e => e.VerificationStatus == EvidenceVerificationStatus.Pending, ct),
            FindingsRaised = sessionFindings.Count,
            Observations = observations,
            CriticalRisks = sessionFindings.Count(f => f.Severity == FindingSeverity.Critical),
            CoveragePercent = Math.Round(coverage, 1),
            EvidenceReviewed = evidenceReviewed,
            ReadinessSnapshot = readiness,
            RecentSessions = recent,
        };
    }

    public async Task<IReadOnlyList<AuditorSessionSummaryDto>> GetSessionsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var sessions = await _db.Set<AuditSession>().AsNoTracking()
            .Where(s => s.TenantId == organizationId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        var sessionIds = sessions.Select(s => s.Id).ToList();
        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.AuditSessionId != null && sessionIds.Contains(f.AuditSessionId.Value))
            .ToListAsync(ct);
        var obsCounts = await _db.Set<AuditObservation>().AsNoTracking()
            .Where(o => sessionIds.Contains(o.AuditSessionId))
            .GroupBy(o => o.AuditSessionId)
            .Select(g => new { SessionId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return sessions.Select(s =>
        {
            var sf = findings.Where(f => f.AuditSessionId == s.Id).ToList();
            var oc = obsCounts.FirstOrDefault(x => x.SessionId == s.Id)?.Count ?? 0;
            return MapSessionSummary(s, sf, oc);
        }).ToList();
    }

    public async Task<AuditSessionDetailDto?> GetSessionByIdAsync(Guid organizationId, Guid sessionId, CancellationToken ct = default)
    {
        var session = await _db.Set<AuditSession>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == organizationId && s.Id == sessionId, ct);
        if (session == null) return null;

        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.AuditSessionId == sessionId)
            .OrderByDescending(f => f.DateRaised)
            .ToListAsync(ct);

        var observations = await _db.Set<AuditObservation>().AsNoTracking()
            .Where(o => o.AuditSessionId == sessionId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var comments = await _db.Set<AuditComment>().AsNoTracking()
            .Where(c => c.AuditSessionId == sessionId && c.ParentCommentId == null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var allReplies = await _db.Set<AuditComment>().AsNoTracking()
            .Where(c => c.AuditSessionId == sessionId && c.ParentCommentId != null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        return new AuditSessionDetailDto
        {
            Id = session.Id,
            SessionNumber = session.SessionNumber,
            AuditType = session.AuditType,
            Status = session.Status,
            OverallResult = session.OverallResult,
            Scope = session.Scope,
            LeadAuditorName = session.LeadAuditorName,
            LeadAuditorUserId = session.LeadAuditorUserId,
            HospitalId = session.HospitalId,
            FrameworkId = session.FrameworkId,
            PlannedStartDate = session.PlannedStartDate,
            PlannedEndDate = session.PlannedEndDate,
            ActualStartDate = session.ActualStartDate,
            ActualEndDate = session.ActualEndDate,
            Summary = session.Summary,
            CreatedAt = session.CreatedAt,
            ScheduledAuditId = session.ScheduledAuditId,
            FindingsCount = findings.Count,
            ObservationsCount = observations.Count,
            AuditorTeam = ParseTeam(session.AuditorTeamJson),
            Findings = findings.Select(MapFinding).ToList(),
            Observations = observations.Select(MapObservation).ToList(),
            Comments = comments.Select(c => MapComment(c, allReplies)).ToList(),
        };
    }

    public async Task<AuditSessionDetailDto> CreateSessionAsync(
        Guid organizationId, Guid hospitalId, Guid userId, CreateAuditSessionRequest request, CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Set<AuditSession>().CountAsync(s => s.TenantId == organizationId, ct);
        var sessionNumber = $"AUD-{year}-{(count + 1):D4}";

        var session = new AuditSession
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            HospitalId = hospitalId,
            FrameworkId = request.FrameworkId,
            SessionNumber = sessionNumber,
            AuditType = request.AuditType,
            Scope = request.Scope.Trim(),
            LeadAuditorUserId = userId,
            LeadAuditorName = request.LeadAuditorName?.Trim(),
            AuditorTeamJson = request.AuditorTeam != null
                ? JsonSerializer.Serialize(request.AuditorTeam)
                : null,
            PlannedStartDate = request.PlannedStartDate,
            PlannedEndDate = request.PlannedEndDate,
            Summary = request.Summary?.Trim(),
            Status = AuditSessionStatus.Planned,
            ScheduledAuditId = request.ScheduledAuditId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
        };

        _db.Set<AuditSession>().Add(session);
        await _db.SaveChangesAsync(ct);

        return (await GetSessionByIdAsync(organizationId, session.Id, ct))!;
    }

    public async Task<AuditSessionDetailDto> CloseSessionAsync(
        Guid organizationId, Guid sessionId, Guid userId, CloseAuditSessionRequest request, CancellationToken ct = default)
    {
        var session = await _db.Set<AuditSession>()
            .FirstOrDefaultAsync(s => s.TenantId == organizationId && s.Id == sessionId, ct)
            ?? throw new InvalidOperationException("Audit session not found.");

        if (session.Status == AuditSessionStatus.Closed)
            throw new InvalidOperationException("Audit session is already closed.");

        var findings = await _db.Findings
            .Where(f => f.AuditSessionId == sessionId)
            .ToListAsync(ct);

        AuditSessionCloseGate.EnsureCanClose(findings);

        session.TransitionStatus(AuditSessionStatus.Closed, userId);
        session.ActualEndDate ??= DateTime.UtcNow;
        if (request.OverallResult.HasValue)
            session.OverallResult = request.OverallResult.Value;
        if (!string.IsNullOrWhiteSpace(request.Summary))
            session.Summary = request.Summary.Trim();

        await _eventEmitter.EmitAsync(
            ComplianceEventType.AuditSessionClosed,
            session.Id,
            nameof(AuditSession),
            organizationId,
            userId,
            new
            {
                sessionId = session.Id,
                session.HospitalId,
                session.FrameworkId,
                session.SessionNumber,
                findingCount = findings.Count,
                acceptedWithoutCapa = findings.Count(f =>
                    f.Status == FindingStatus.Accepted && f.PromotedDeficiencyId == null),
            },
            ct);

        await _db.SaveChangesAsync(ct);
        return (await GetSessionByIdAsync(organizationId, session.Id, ct))!;
    }

    public async Task<AuditObservationListItemDto> CreateObservationAsync(
        Guid organizationId, Guid userId, string? userName, CreateAuditObservationRequest request, CancellationToken ct = default)
    {
        var session = await _db.Set<AuditSession>()
            .FirstOrDefaultAsync(s => s.TenantId == organizationId && s.Id == request.AuditSessionId, ct)
            ?? throw new InvalidOperationException("Audit session not found.");

        session.EnsureAcceptsFindings();

        var count = await _db.Set<AuditObservation>().CountAsync(o => o.AuditSessionId == request.AuditSessionId, ct);
        var obsNumber = $"OBS-{session.SessionNumber}-{(count + 1):D3}";

        var obs = new AuditObservation
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            AuditSessionId = request.AuditSessionId,
            ObservationNumber = obsNumber,
            Type = request.Type,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ObjectiveElementId = request.ObjectiveElementId,
            ChapterId = request.ChapterId,
            ChapterCode = NormalizeCode(request.ChapterCode),
            OeCode = NormalizeCode(request.OeCode),
            Department = request.Department,
            CreatedByUserId = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
        };

        // Resolve codes from linked OE/chapter when only Guids were supplied.
        if (string.IsNullOrEmpty(obs.OeCode) && obs.ObjectiveElementId.HasValue)
        {
            var oe = await _db.ObjectiveElements.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == obs.ObjectiveElementId.Value, ct);
            if (oe != null)
                obs.OeCode = oe.Code;
        }

        if (string.IsNullOrEmpty(obs.ChapterCode) && obs.ChapterId.HasValue)
        {
            var ch = await _db.ComplianceChapters.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == obs.ChapterId.Value, ct);
            if (ch != null)
                obs.ChapterCode = ch.Code;
        }

        _db.Set<AuditObservation>().Add(obs);
        await _db.SaveChangesAsync(ct);

        return MapObservation(obs);
    }

    public async Task<AuditCommentDto> CreateCommentAsync(
        Guid organizationId, Guid userId, string? userName, bool isAuditor, CreateAuditCommentRequest request, CancellationToken ct = default)
    {
        var exists = await _db.Set<AuditSession>().AnyAsync(
            s => s.TenantId == organizationId && s.Id == request.AuditSessionId, ct);
        if (!exists)
            throw new InvalidOperationException("Audit session not found.");

        var comment = new AuditComment
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            AuditSessionId = request.AuditSessionId,
            Type = request.Type,
            Content = request.Content.Trim(),
            AuthorUserId = userId,
            AuthorName = userName,
            IsAuditorAuthor = isAuditor,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Set<AuditComment>().Add(comment);
        await _db.SaveChangesAsync(ct);

        return MapComment(comment, Array.Empty<AuditComment>());
    }

    public async Task<AuditorAssessmentContextDto?> GetAssessmentContextAsync(
        Guid organizationId, Guid hospitalId, Guid? frameworkId, CancellationToken ct = default)
    {
        var journey = await ComplianceJourneySnapshotResolver.GetActiveJourneyForSnapshotAsync(
            _db, hospitalId, frameworkId ?? Guid.Empty, ct);
        if (journey == null && frameworkId == null)
            journey = await GetActiveJourneyAsync(hospitalId, ct);
        if (journey == null) return null;

        var fwId = journey.FrameworkId;
        var framework = await _db.ComplianceFrameworks.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fwId, ct);

        var chapters = await _db.ComplianceChapters.AsNoTracking()
            .Where(c => c.FrameworkId == fwId)
            .OrderBy(c => c.Sequence)
            .ToListAsync(ct);

        var standards = await _db.ComplianceStandards.AsNoTracking()
            .Where(s => chapters.Select(c => c.Id).Contains(s.ChapterId))
            .ToListAsync(ct);

        var oes = await _db.ObjectiveElements.AsNoTracking()
            .Where(oe => standards.Select(s => s.Id).Contains(oe.ComplianceStandardId))
            .ToListAsync(ct);

        var oeIds = oes.Select(o => o.Id).ToList();
        var evidenceLinks = await _db.ObjectiveElementEvidences.AsNoTracking()
            .Where(e => oeIds.Contains(e.ObjectiveElementId))
            .ToListAsync(ct);

        var deficiencies = await _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => d.HospitalId == hospitalId && d.FrameworkId == fwId && d.ObjectiveElementId != null)
            .ToListAsync(ct);

        var tasks = await _db.ComplianceTasks.AsNoTracking()
            .Where(t => oeIds.Contains(t.ObjectiveElementId))
            .ToListAsync(ct);

        var scores = await _db.ObjectiveElementScores.AsNoTracking()
            .Where(s => s.JourneyId == journey.Id && oeIds.Contains(s.ObjectiveElementId))
            .ToListAsync(ct);

        var chapterDtos = chapters.Select(ch =>
        {
            var chStandards = standards.Where(s => s.ChapterId == ch.Id).ToList();
            return new AuditorChapterContextDto
            {
                Id = ch.Id,
                Code = ch.Code,
                Name = ch.Name,
                Standards = chStandards.Select(st =>
                {
                    var stOes = oes.Where(o => o.ComplianceStandardId == st.Id).ToList();
                    return new AuditorStandardContextDto
                    {
                        Id = st.Id,
                        Code = st.Code,
                        Name = st.Name,
                        ObjectiveElements = stOes.Select(oe =>
                        {
                            var ev = evidenceLinks.Where(e => e.ObjectiveElementId == oe.Id).ToList();
                            var defs = deficiencies.Where(d => d.ObjectiveElementId == oe.Id).ToList();
                            var score = scores.FirstOrDefault(s => s.ObjectiveElementId == oe.Id);
                            return new AuditorObjectiveElementContextDto
                            {
                                Id = oe.Id,
                                Code = oe.Code,
                                Description = oe.Description,
                                EvidenceCount = ev.Count,
                                VerifiedEvidenceCount = ev.Count(e => e.VerificationStatus == EvidenceVerificationStatus.Verified),
                                DeficiencyCount = defs.Count(d => d.Status != DeficiencyStatus.Closed),
                                OpenCapaCount = defs.Count(d =>
                                    d.CorrectiveActionStatus != CapaActionStatus.Completed
                                    || d.PreventiveActionStatus != CapaActionStatus.Completed),
                                TaskCount = tasks.Count(t => t.ObjectiveElementId == oe.Id),
                                ReadinessScore = score?.EvidenceSampleCompliancePercent,
                            };
                        }).ToList(),
                    };
                }).ToList(),
            };
        }).ToList();

        return new AuditorAssessmentContextDto
        {
            FrameworkId = fwId,
            FrameworkName = framework?.Name,
            Chapters = chapterDtos,
        };
    }

    private async Task<Domain.ComplianceJourney?> GetActiveJourneyAsync(Guid hospitalId, CancellationToken ct) =>
        await _db.ComplianceJourneys.IgnoreQueryFilters().AsNoTracking()
            .Where(j => j.HospitalId == hospitalId)
            .OrderByDescending(j => j.UpdatedAt)
            .FirstOrDefaultAsync(ct);

    private static AuditorSessionSummaryDto MapSessionSummary(
        AuditSession s, IReadOnlyList<Domain.Finding> findings, int obsCount) =>
        new()
        {
            Id = s.Id,
            SessionNumber = s.SessionNumber,
            AuditType = s.AuditType,
            Status = s.Status,
            OverallResult = s.OverallResult,
            Scope = s.Scope,
            LeadAuditorName = s.LeadAuditorName,
            PlannedStartDate = s.PlannedStartDate,
            PlannedEndDate = s.PlannedEndDate,
            FindingsCount = findings.Count(f => f.AuditSessionId == s.Id),
            ObservationsCount = obsCount,
            CreatedAt = s.CreatedAt,
            ScheduledAuditId = s.ScheduledAuditId,
        };

    private static AuditObservationListItemDto MapObservation(AuditObservation o) => new()
    {
        Id = o.Id,
        ObservationNumber = o.ObservationNumber,
        Type = o.Type,
        Title = o.Title,
        Description = o.Description,
        ObjectiveElementId = o.ObjectiveElementId,
        ChapterId = o.ChapterId,
        ChapterCode = o.ChapterCode,
        OeCode = o.OeCode,
        Department = o.Department,
        CreatedByName = o.CreatedByName,
        CreatedAt = o.CreatedAt,
    };

    private static string? NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim();

    private static AuditorFindingSummaryDto MapFinding(Domain.Finding f) => new()
    {
        Id = f.Id,
        FindingNumber = string.IsNullOrWhiteSpace(f.FindingNumber)
            ? $"FND-{f.Id.ToString()[..8].ToUpperInvariant()}"
            : f.FindingNumber,
        Title = f.Title,
        Severity = f.Severity,
        Status = f.Status,
        ObjectiveElementId = f.ObjectiveElementId,
        PromotedDeficiencyId = f.PromotedDeficiencyId,
        DateRaised = f.DateRaised,
    };

    private static AuditCommentDto MapComment(AuditComment c, IReadOnlyList<AuditComment> allReplies) => new()
    {
        Id = c.Id,
        Type = c.Type,
        Content = c.Content,
        AuthorUserId = c.AuthorUserId,
        AuthorName = c.AuthorName,
        IsAuditorAuthor = c.IsAuditorAuthor,
        ParentCommentId = c.ParentCommentId,
        CreatedAt = c.CreatedAt,
        Replies = allReplies.Where(r => r.ParentCommentId == c.Id).Select(r => MapComment(r, allReplies)).ToList(),
    };

    private static IReadOnlyList<AuditorTeamMemberDto> ParseTeam(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<AuditorTeamMemberDto>();
        try
        {
            return JsonSerializer.Deserialize<List<AuditorTeamMemberDto>>(json) ?? new List<AuditorTeamMemberDto>();
        }
        catch
        {
            return Array.Empty<AuditorTeamMemberDto>();
        }
    }
}
