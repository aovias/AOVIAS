using System.IO;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Presentation;
using ComplianceService.Application.Journey;
using ComplianceService.Application.TaskExecution;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ComplianceService.Application.Services;

public sealed class TaskQueryService : ITaskQueryService
{
    public const int KanbanMaxTasks = 2000;

    private readonly ComplianceDbContext _db;
    private readonly IUserContextService _userContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IObligationNextActionService _nextActionService;

    public TaskQueryService(
        ComplianceDbContext db,
        IUserContextService userContext,
        IHttpContextAccessor httpContextAccessor,
        IObligationNextActionService nextActionService)
    {
        _db = db;
        _userContext = userContext;
        _httpContextAccessor = httpContextAccessor;
        _nextActionService = nextActionService;
    }

    public async Task<ComplianceTaskFrameworkContext?> ResolveTaskFrameworkAsync(
        Guid taskId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        if (taskId == Guid.Empty || frameworkId == Guid.Empty)
        {
            return null;
        }

        var hospitalId = _userContext.HospitalId;
        var fwId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db,
            hospitalId,
            frameworkId,
            cancellationToken);

        var row = await (
            from t in _db.ComplianceTasks.AsNoTracking()
            join oe in _db.ObjectiveElements.AsNoTracking() on t.ObjectiveElementId equals oe.Id
            join s in _db.ComplianceStandards.AsNoTracking() on t.ComplianceStandardId equals s.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on s.ChapterId equals ch.Id
            join fw in _db.ComplianceFrameworks.AsNoTracking() on ch.FrameworkId equals fw.Id
            where t.Id == taskId
                  && t.HospitalId == hospitalId
                  && ch.FrameworkId == fwId
                  && oe.IsApplicable
                  && fw.IsActive
            select new { t.ObjectiveElementId, ResolvedFw = fwId }).FirstOrDefaultAsync(cancellationToken);

        if (row == null)
        {
            return null;
        }

        return new ComplianceTaskFrameworkContext
        {
            TaskId = taskId,
            ObjectiveElementId = row.ObjectiveElementId,
            ResolvedFrameworkId = row.ResolvedFw
        };
    }

    public async Task<TaskSummaryDto> GetSummaryAsync(TaskQuerySpec spec, CancellationToken cancellationToken = default)
    {
        var hospitalId = spec.HospitalId ?? _userContext.HospitalId;
        var today = DateTime.UtcNow.Date;
        var fwId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db,
            hospitalId,
            spec.FrameworkId,
            cancellationToken);

        var q = BuildFilteredJoin(hospitalId, fwId, spec, today);

        var total = await q.CountAsync(cancellationToken);
        var notStarted = await q.CountAsync(x => x.Task.Status == ComplianceTaskStatus.Pending, cancellationToken);
        var inProgress = await q.CountAsync(x => x.Task.Status == ComplianceTaskStatus.InProgress, cancellationToken);
        var blocked = await q.CountAsync(
            x => x.Task.Status == ComplianceTaskStatus.OnHold || x.Task.Status == ComplianceTaskStatus.Blocked,
            cancellationToken);
        var completed = await q.CountAsync(x => x.Task.Status == ComplianceTaskStatus.Completed, cancellationToken);
        var overdue = await q.CountAsync(
            x => x.Task.DueDate != null
                 && x.Task.DueDate.Value.Date < today
                 && x.Task.Status != ComplianceTaskStatus.Completed,
            cancellationToken);

        return new TaskSummaryDto
        {
            Total = total,
            NotStarted = notStarted,
            InProgress = inProgress,
            Blocked = blocked,
            Completed = completed,
            Overdue = overdue
        };
    }

    public async Task<PagedResultDto<TaskListItemResponseDto>> GetPagedAsync(
        TaskQuerySpec spec,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var hospitalId = _userContext.HospitalId;
        var today = DateTime.UtcNow.Date;
        var fwId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db,
            hospitalId,
            spec.FrameworkId,
            cancellationToken);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = BuildFilteredJoin(hospitalId, fwId, spec, today);
        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderBy(x => x.StandardCode)
            .ThenBy(x => x.OeCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var evidenceCounts = await GetMergedEvidenceCountsByTaskAsync(rows, hospitalId, fwId, cancellationToken);
        var remediation = await LoadRemediationContextAsync(rows, hospitalId, cancellationToken);
        var items = rows.ConvertAll(x =>
        {
            var (tot, app, pend, _) = evidenceCounts.TryGetValue(x.Task.Id, out var c) ? c : (0, 0, 0, 0);
            remediation.TryGetValue(x.Task.Id, out var ctx);
            return MapListItem(x, today, tot, app, pend, ctx);
        });
        return new PagedResultDto<TaskListItemResponseDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TaskKanbanResponseDto> GetKanbanAsync(TaskQuerySpec spec, CancellationToken cancellationToken = default)
    {
        var hospitalId = _userContext.HospitalId;
        var today = DateTime.UtcNow.Date;
        var fwId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db,
            hospitalId,
            spec.FrameworkId,
            cancellationToken);

        var q = BuildFilteredJoin(hospitalId, fwId, spec, today);
        var total = await q.CountAsync(cancellationToken);
        var truncated = total > KanbanMaxTasks;
        var rows = await q
            .OrderBy(x => x.StandardCode)
            .ThenBy(x => x.OeCode)
            .Take(KanbanMaxTasks)
            .ToListAsync(cancellationToken);

        var evidenceCounts = await GetMergedEvidenceCountsByTaskAsync(rows, hospitalId, fwId, cancellationToken);
        var remediation = await LoadRemediationContextAsync(rows, hospitalId, cancellationToken);
        var items = rows.ConvertAll(x =>
        {
            var (tot, app, pend, _) = evidenceCounts.TryGetValue(x.Task.Id, out var c) ? c : (0, 0, 0, 0);
            remediation.TryGetValue(x.Task.Id, out var ctx);
            return MapListItem(x, today, tot, app, pend, ctx);
        });
        return new TaskKanbanResponseDto
        {
            NotStarted = items.Where(i => i.Status == TaskApiStatus.NotStarted).ToList(),
            InProgress = items.Where(i => i.Status == TaskApiStatus.InProgress).ToList(),
            Blocked = items.Where(i => i.Status == TaskApiStatus.Blocked).ToList(),
            Completed = items.Where(i => i.Status == TaskApiStatus.Completed).ToList(),
            Truncated = truncated
        };
    }

    public async Task<TaskDetailResponseDto?> GetDetailAsync(
        Guid taskId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        if (taskId == Guid.Empty)
        {
            return null;
        }

        var hospitalId = _userContext.HospitalId;
        var today = DateTime.UtcNow.Date;
        var fwId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db,
            hospitalId,
            frameworkId,
            cancellationToken);

        var row = await FetchTaskJoinAsync(taskId, hospitalId, fwId, cancellationToken);
        if (row == null)
        {
            return null;
        }

        var oneRow = new List<TaskJoinResult> { row };
        var evidenceCounts = await GetMergedEvidenceCountsByTaskAsync(oneRow, hospitalId, fwId, cancellationToken);
        var remediation = await LoadRemediationContextAsync(oneRow, hospitalId, cancellationToken);
        var (tot, app, pend, _) = evidenceCounts.TryGetValue(row.Task.Id, out var c) ? c : (0, 0, 0, 0);
        remediation.TryGetValue(row.Task.Id, out var ctx);
        var list = MapListItem(row, today, tot, app, pend, ctx);
        return new TaskDetailResponseDto
        {
            Id = list.Id,
            Title = list.Title,
            Description = list.Description,
            ObjectiveElementId = list.ObjectiveElementId,
            ObjectiveElementCode = list.ObjectiveElementCode,
            ObjectiveElementTitle = list.ObjectiveElementTitle,
            ChapterId = list.ChapterId,
            AssignedToUserId = list.AssignedToUserId,
            AssignedToDisplayName = list.AssignedToDisplayName,
            DepartmentId = list.DepartmentId,
            DepartmentName = list.DepartmentName,
            Priority = list.Priority,
            Status = list.Status,
            RiskLevel = list.RiskLevel,
            RiskScore = list.RiskScore,
            IsOverdue = list.IsOverdue,
            DueDate = list.DueDate,
            CreatedAt = list.CreatedAt,
            RowVersion = list.RowVersion,
            Criticality = list.Criticality,
            EvidenceTotalCount = list.EvidenceTotalCount,
            EvidenceApprovedCount = list.EvidenceApprovedCount,
            EvidencePendingReviewCount = list.EvidencePendingReviewCount,
            SourceDeficiencyId = list.SourceDeficiencyId,
            RelatedDeficiency = list.RelatedDeficiency,
            RelatedObligation = list.RelatedObligation,
            MaturityStageLabel = list.MaturityStageLabel,
            Category = list.Category,
            TaskKind = list.TaskKind,
            BlockedByTaskId = list.BlockedByTaskId,
            IsBlockedByDependency = list.IsBlockedByDependency,
            ComplianceStandardId = row.Task.ComplianceStandardId,
            StandardCode = row.StandardCode,
            MaturityState = row.Task.MaturityState
        };
    }

    public async Task<int> GetEvidencePendingReviewCountAsync(
        Guid taskId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        if (taskId == Guid.Empty)
        {
            return 0;
        }

        var hospitalId = _userContext.HospitalId;
        var fwId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db,
            hospitalId,
            frameworkId,
            cancellationToken);

        var row = await FetchTaskJoinAsync(taskId, hospitalId, fwId, cancellationToken);
        if (row == null)
        {
            return 0;
        }

        var evidenceCounts = await GetMergedEvidenceCountsByTaskAsync(
            new List<TaskJoinResult> { row },
            hospitalId,
            fwId,
            cancellationToken);

        return evidenceCounts.TryGetValue(taskId, out var c) ? c.PendingReview : 0;
    }

    public async Task<int> GetEvidenceRejectedOrNeedsMoreCountAsync(
        Guid taskId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        if (taskId == Guid.Empty)
        {
            return 0;
        }

        var hospitalId = _userContext.HospitalId;
        var fwId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db,
            hospitalId,
            frameworkId,
            cancellationToken);

        var row = await FetchTaskJoinAsync(taskId, hospitalId, fwId, cancellationToken);
        if (row == null)
        {
            return 0;
        }

        var evidenceCounts = await GetMergedEvidenceCountsByTaskAsync(
            new List<TaskJoinResult> { row },
            hospitalId,
            fwId,
            cancellationToken);

        return evidenceCounts.TryGetValue(taskId, out var c) ? c.RejectedOrNeedsMore : 0;
    }

    public async Task<bool> HasMissingRequiredEvidenceAsync(
        Guid taskId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        if (taskId == Guid.Empty)
        {
            return false;
        }

        var hospitalId = _userContext.HospitalId;
        var fwId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db,
            hospitalId,
            frameworkId,
            cancellationToken);

        var row = await FetchTaskJoinAsync(taskId, hospitalId, fwId, cancellationToken);
        if (row == null)
        {
            return false;
        }

        var evidenceCounts = await GetMergedEvidenceCountsByTaskAsync(
            new List<TaskJoinResult> { row },
            hospitalId,
            fwId,
            cancellationToken);

        return evidenceCounts.TryGetValue(taskId, out var c) && c.Total == 0;
    }

    private Task<TaskJoinResult?> FetchTaskJoinAsync(
        Guid taskId,
        Guid hospitalId,
        Guid fwId,
        CancellationToken cancellationToken) =>
        (
            from t in _db.ComplianceTasks.AsNoTracking()
            join oe in _db.ObjectiveElements.AsNoTracking() on t.ObjectiveElementId equals oe.Id
            join s in _db.ComplianceStandards.AsNoTracking() on t.ComplianceStandardId equals s.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on s.ChapterId equals ch.Id
            join fw in _db.ComplianceFrameworks.AsNoTracking() on ch.FrameworkId equals fw.Id
            where t.Id == taskId
                  && t.HospitalId == hospitalId
                  && ch.FrameworkId == fwId
                  && oe.IsApplicable
                  && fw.IsActive
            select new TaskJoinResult
            {
                Task = t,
                ChapterId = ch.Id,
                OeCode = oe.Code,
                OeTitle = oe.Title,
                StandardCode = s.Code
            }).FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResultDto<TaskEvidenceItemDto>> GetMergedEvidencesAsync(
        Guid taskId,
        Guid frameworkId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var ctx = await ResolveTaskFrameworkAsync(taskId, frameworkId, cancellationToken);
        if (ctx == null)
        {
            return EmptyEvidencePage(page, pageSize);
        }

        var hospitalId = _userContext.HospitalId;
        var fwId = ctx.ResolvedFrameworkId;
        var oeId = ctx.ObjectiveElementId;

        var oeRows = await (
            from link in _db.ObjectiveElementEvidences.AsNoTracking()
            join e in _db.Evidences.AsNoTracking() on link.EvidenceId equals e.Id
            join oe in _db.ObjectiveElements.AsNoTracking() on link.ObjectiveElementId equals oe.Id
            join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
            join fw in _db.ComplianceFrameworks.AsNoTracking() on ch.FrameworkId equals fw.Id
            where link.ObjectiveElementId == oeId
                  && e.HospitalId == hospitalId
                  && e.FrameworkId == fwId
                  && ch.FrameworkId == fwId
                  && oe.IsApplicable
                  && fw.IsActive
            select new { link, e }).ToListAsync(cancellationToken);

        var taskRows = await _db.Evidences.AsNoTracking()
            .Where(e => e.TaskId == taskId && e.HospitalId == hospitalId && e.FrameworkId == fwId)
            .ToListAsync(cancellationToken);

        var oeEvidenceIds = new HashSet<Guid>(oeRows.Select(r => r.e.Id));
        var taskEvidenceIds = new HashSet<Guid>(taskRows.Select(e => e.Id));

        var linkByEvidence = oeRows.GroupBy(r => r.e.Id).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.link.LinkedAt).First().link);

        var combined = new List<Evidence>();
        combined.AddRange(oeRows.Select(r => r.e));
        combined.AddRange(taskRows);
        var mergedEvidences = combined
            .GroupBy(e => e.Id)
            .Select(g => g.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id).First())
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        var total = mergedEvidences.Count;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var slice = mergedEvidences.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var items = slice.ConvertAll(e =>
        {
            var fromOe = oeEvidenceIds.Contains(e.Id);
            var fromTask = taskEvidenceIds.Contains(e.Id);
            var source = fromOe && fromTask
                ? TaskEvidenceSourceApi.Both
                : fromOe
                    ? TaskEvidenceSourceApi.ObjectiveElement
                    : TaskEvidenceSourceApi.Task;

            linkByEvidence.TryGetValue(e.Id, out var link);
            var status = link != null ? MapVerificationStatus(link.VerificationStatus) : "pendingReview";

            return new TaskEvidenceItemDto
            {
                EvidenceId = e.Id,
                FileName = FileNameFromUrl(e.FileUrl, e.Title),
                FileUrl = e.FileUrl ?? string.Empty,
                FileType = Path.GetExtension(e.FileUrl ?? string.Empty).TrimStart('.'),
                FileSizeBytes = 0,
                UploadedBy = e.UploadedBy ?? string.Empty,
                UploadedAt = e.UploadedOn,
                Status = status,
                IsApproved = link != null && link.VerificationStatus == EvidenceVerificationStatus.Verified,
                ReviewComment = link?.ReviewComment ?? link?.Remarks,
                Source = source,
                Version = e.CurrentVersion
            };
        });

        return new PagedResultDto<TaskEvidenceItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultDto<TaskCommentDto>> GetCommentsAsync(
        Guid taskId,
        Guid frameworkId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var ctx = await ResolveTaskFrameworkAsync(taskId, frameworkId, cancellationToken);
        if (ctx == null)
        {
            return new PagedResultDto<TaskCommentDto>
            {
                Items = Array.Empty<TaskCommentDto>(),
                TotalCount = 0,
                Page = Math.Max(1, page),
                PageSize = Math.Clamp(pageSize, 1, 200)
            };
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.ComplianceTaskComments.AsNoTracking()
            .Where(c => c.TaskId == taskId && c.HospitalId == _userContext.HospitalId && c.FrameworkId == ctx.ResolvedFrameworkId)
            .OrderByDescending(c => c.CreatedAt);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = rows.ConvertAll(c => new TaskCommentDto
        {
            Id = c.Id,
            TaskId = c.TaskId,
            AuthorUserId = c.AuthorUserId,
            Body = c.Body,
            CreatedAt = c.CreatedAt
        });

        return new PagedResultDto<TaskCommentDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultDto<TaskHistoryEntryDto>> GetHistoryAsync(
        Guid taskId,
        Guid frameworkId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var ctx = await ResolveTaskFrameworkAsync(taskId, frameworkId, cancellationToken);
        if (ctx == null)
        {
            return new PagedResultDto<TaskHistoryEntryDto>
            {
                Items = Array.Empty<TaskHistoryEntryDto>(),
                TotalCount = 0,
                Page = Math.Max(1, page),
                PageSize = Math.Clamp(pageSize, 1, 200)
            };
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.AuditLogs.AsNoTracking()
            .Where(l => l.EntityType == ComplianceTaskAudit.EntityType && l.EntityId == taskId)
            .OrderByDescending(l => l.Timestamp);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = rows.ConvertAll(l => new TaskHistoryEntryDto
        {
            Id = l.Id,
            Action = l.Action,
            PerformedBy = l.PerformedBy,
            Timestamp = l.Timestamp,
            MetadataJson = l.MetadataJson
        });

        return new PagedResultDto<TaskHistoryEntryDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private IQueryable<TaskJoinResult> BuildFilteredJoin(Guid hospitalId, Guid fwId, TaskQuerySpec spec, DateTime today)
    {
        var q = from t in _db.ComplianceTasks.AsNoTracking()
            join oe in _db.ObjectiveElements.AsNoTracking() on t.ObjectiveElementId equals oe.Id
            join s in _db.ComplianceStandards.AsNoTracking() on t.ComplianceStandardId equals s.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on s.ChapterId equals ch.Id
            join fw in _db.ComplianceFrameworks.AsNoTracking() on ch.FrameworkId equals fw.Id
            where t.HospitalId == hospitalId
                  && ch.FrameworkId == fwId
                  && oe.IsApplicable
                  && fw.IsActive
            select new TaskJoinResult
            {
                Task = t,
                ChapterId = ch.Id,
                OeCode = oe.Code,
                OeTitle = oe.Title,
                StandardCode = s.Code
            };

        if (spec.Status is { } st)
        {
            q = st switch
            {
                TaskApiStatus.NotStarted => q.Where(x => x.Task.Status == ComplianceTaskStatus.Pending),
                TaskApiStatus.InProgress => q.Where(x => x.Task.Status == ComplianceTaskStatus.InProgress),
                TaskApiStatus.Blocked => q.Where(x =>
                    x.Task.Status == ComplianceTaskStatus.OnHold || x.Task.Status == ComplianceTaskStatus.Blocked),
                TaskApiStatus.Completed => q.Where(x => x.Task.Status == ComplianceTaskStatus.Completed),
                _ => q
            };
        }

        if (spec.Priority is { } pr)
        {
            q = q.Where(x => x.Task.Priority == pr);
        }

        if (!string.IsNullOrWhiteSpace(spec.OwnerId))
        {
            var oid = spec.OwnerId.Trim();
            q = q.Where(x => x.Task.AssignedToUserId == oid);
        }

        if (spec.DepartmentId is { } dept && dept != Guid.Empty)
        {
            q = q.Where(x => x.Task.DepartmentId == dept);
        }

        if (spec.ChapterId is { } chId && chId != Guid.Empty)
        {
            q = q.Where(x => x.ChapterId == chId);
        }

        if (!string.IsNullOrWhiteSpace(spec.Search))
        {
            var term = spec.Search.Trim();
            q = q.Where(x =>
                (x.Task.Title != null && x.Task.Title.Contains(term))
                || (x.Task.Description != null && x.Task.Description.Contains(term))
                || x.OeCode.Contains(term)
                || x.StandardCode.Contains(term));
        }

        q = ApplyRiskLevelFilter(q, spec.RiskLevel, today);

        if (spec.OverdueOnly == true)
        {
            q = q.Where(x =>
                x.Task.DueDate != null
                && x.Task.DueDate.Value.Date < today
                && x.Task.Status != ComplianceTaskStatus.Completed);
        }

        if (spec.DueThisWeek == true)
        {
            var weekEnd = today.AddDays(7);
            q = q.Where(x =>
                x.Task.DueDate != null
                && x.Task.DueDate.Value.Date >= today
                && x.Task.DueDate.Value.Date <= weekEnd
                && x.Task.Status != ComplianceTaskStatus.Completed);
        }

        if (spec.FocusCriticalOverdue == true)
        {
            q = ApplyRiskLevelFilter(q, TaskRiskLevelApi.Critical, today);
            q = q.Where(x =>
                x.Task.DueDate != null
                && x.Task.DueDate.Value.Date < today
                && x.Task.Status != ComplianceTaskStatus.Completed);
        }

        if (spec.DeficiencyLinkedOnly == true)
        {
            q = q.Where(x => x.Task.SourceDeficiencyId != null);
        }

        if (spec.DeficiencySeverity is { } defSev)
        {
            q = q.Where(x =>
                x.Task.SourceDeficiencyId != null
                && _db.ComplianceDeficiencies.Any(d =>
                    d.Id == x.Task.SourceDeficiencyId
                    && d.Severity == defSev
                    && d.Status != DeficiencyStatus.Closed));
        }

        if (spec.DeficiencyType is { } defType)
        {
            q = q.Where(x =>
                x.Task.SourceDeficiencyId != null
                && _db.ComplianceDeficiencies.Any(d =>
                    d.Id == x.Task.SourceDeficiencyId
                    && d.DeficiencyType == defType
                    && d.Status != DeficiencyStatus.Closed));
        }

        if (spec.MaturityStage is { } matStage)
        {
            q = q.Where(x =>
                x.Task.MaturityStage == matStage
                || _db.ComplianceDeficiencies.Any(d =>
                    d.Id == x.Task.SourceDeficiencyId && d.MaturityLevel == matStage));
        }

        if (spec.ObligationEvidenceStatus is { } evStatus)
        {
            q = q.Where(x =>
                _db.OrganizationComplianceObligations.Any(o =>
                    o.HospitalId == x.Task.HospitalId
                    && o.ObjectiveElementId == x.Task.ObjectiveElementId
                    && o.EvidenceStatus == evStatus));
        }

        if (spec.ObligationVerificationStatus is { } verStatus)
        {
            q = q.Where(x =>
                _db.OrganizationComplianceObligations.Any(o =>
                    o.HospitalId == x.Task.HospitalId
                    && o.ObjectiveElementId == x.Task.ObjectiveElementId
                    && o.VerificationStatus == verStatus));
        }

        if (spec.Category is { } category)
        {
            q = q.Where(x => x.Task.Category == category);
        }

        if (spec.TaskKind is { } kind)
        {
            q = q.Where(x => x.Task.TaskKind == kind);
        }

        if (spec.TeamOnly == true)
        {
            var me = _userContext.UserId.ToString();
            q = q.Where(x => x.Task.AssignedToUserId != me);
        }

        if (spec.DueFrom is { } dueFrom)
        {
            var from = dueFrom.Date;
            q = q.Where(x => x.Task.DueDate != null && x.Task.DueDate.Value.Date >= from);
        }

        if (spec.DueTo is { } dueTo)
        {
            var to = dueTo.Date;
            q = q.Where(x => x.Task.DueDate != null && x.Task.DueDate.Value.Date <= to);
        }

        return q;
    }

    private static IQueryable<TaskJoinResult> ApplyRiskLevelFilter(
        IQueryable<TaskJoinResult> query,
        TaskRiskLevelApi? rl,
        DateTime today)
    {
        if (!rl.HasValue)
        {
            return query;
        }

        // Keep formula inline so EF Core translates to SQL (no local method calls).
        return rl.Value switch
        {
            TaskRiskLevelApi.Low => query.Where(x =>
                (x.Task.Criticality == TaskCriticalityLevel.Critical ? 4 :
                    x.Task.Criticality == TaskCriticalityLevel.High ? 3 :
                    x.Task.Criticality == TaskCriticalityLevel.Medium ? 2 : 1)
                + (x.Task.DueDate != null
                   && x.Task.DueDate.Value.Date < today
                   && x.Task.Status != ComplianceTaskStatus.Completed
                    ? 2
                    : 0)
                + (x.Task.Priority == ComplianceTaskPriority.Urgent ? 4 :
                    x.Task.Priority == ComplianceTaskPriority.High ? 3 :
                    x.Task.Priority == ComplianceTaskPriority.Medium ? 2 : 1)
                <= 2),
            TaskRiskLevelApi.Medium => query.Where(x =>
                (x.Task.Criticality == TaskCriticalityLevel.Critical ? 4 :
                    x.Task.Criticality == TaskCriticalityLevel.High ? 3 :
                    x.Task.Criticality == TaskCriticalityLevel.Medium ? 2 : 1)
                + (x.Task.DueDate != null
                   && x.Task.DueDate.Value.Date < today
                   && x.Task.Status != ComplianceTaskStatus.Completed
                    ? 2
                    : 0)
                + (x.Task.Priority == ComplianceTaskPriority.Urgent ? 4 :
                    x.Task.Priority == ComplianceTaskPriority.High ? 3 :
                    x.Task.Priority == ComplianceTaskPriority.Medium ? 2 : 1)
                >= 3
                && (x.Task.Criticality == TaskCriticalityLevel.Critical ? 4 :
                    x.Task.Criticality == TaskCriticalityLevel.High ? 3 :
                    x.Task.Criticality == TaskCriticalityLevel.Medium ? 2 : 1)
                + (x.Task.DueDate != null
                   && x.Task.DueDate.Value.Date < today
                   && x.Task.Status != ComplianceTaskStatus.Completed
                    ? 2
                    : 0)
                + (x.Task.Priority == ComplianceTaskPriority.Urgent ? 4 :
                    x.Task.Priority == ComplianceTaskPriority.High ? 3 :
                    x.Task.Priority == ComplianceTaskPriority.Medium ? 2 : 1)
                <= 4),
            TaskRiskLevelApi.High => query.Where(x =>
                (x.Task.Criticality == TaskCriticalityLevel.Critical ? 4 :
                    x.Task.Criticality == TaskCriticalityLevel.High ? 3 :
                    x.Task.Criticality == TaskCriticalityLevel.Medium ? 2 : 1)
                + (x.Task.DueDate != null
                   && x.Task.DueDate.Value.Date < today
                   && x.Task.Status != ComplianceTaskStatus.Completed
                    ? 2
                    : 0)
                + (x.Task.Priority == ComplianceTaskPriority.Urgent ? 4 :
                    x.Task.Priority == ComplianceTaskPriority.High ? 3 :
                    x.Task.Priority == ComplianceTaskPriority.Medium ? 2 : 1)
                >= 5
                && (x.Task.Criticality == TaskCriticalityLevel.Critical ? 4 :
                    x.Task.Criticality == TaskCriticalityLevel.High ? 3 :
                    x.Task.Criticality == TaskCriticalityLevel.Medium ? 2 : 1)
                + (x.Task.DueDate != null
                   && x.Task.DueDate.Value.Date < today
                   && x.Task.Status != ComplianceTaskStatus.Completed
                    ? 2
                    : 0)
                + (x.Task.Priority == ComplianceTaskPriority.Urgent ? 4 :
                    x.Task.Priority == ComplianceTaskPriority.High ? 3 :
                    x.Task.Priority == ComplianceTaskPriority.Medium ? 2 : 1)
                <= 6),
            TaskRiskLevelApi.Critical => query.Where(x =>
                (x.Task.Criticality == TaskCriticalityLevel.Critical ? 4 :
                    x.Task.Criticality == TaskCriticalityLevel.High ? 3 :
                    x.Task.Criticality == TaskCriticalityLevel.Medium ? 2 : 1)
                + (x.Task.DueDate != null
                   && x.Task.DueDate.Value.Date < today
                   && x.Task.Status != ComplianceTaskStatus.Completed
                    ? 2
                    : 0)
                + (x.Task.Priority == ComplianceTaskPriority.Urgent ? 4 :
                    x.Task.Priority == ComplianceTaskPriority.High ? 3 :
                    x.Task.Priority == ComplianceTaskPriority.Medium ? 2 : 1)
                >= 7),
            _ => query
        };
    }

    private async Task<IReadOnlyDictionary<Guid, (int Total, int Approved, int PendingReview, int RejectedOrNeedsMore)>> GetMergedEvidenceCountsByTaskAsync(
        IReadOnlyList<TaskJoinResult> rows,
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return new Dictionary<Guid, (int Total, int Approved, int PendingReview, int RejectedOrNeedsMore)>();
        }

        var taskIds = rows.Select(r => r.Task.Id).Distinct().ToList();
        var oeIds = rows.Select(r => r.Task.ObjectiveElementId).Distinct().ToList();

        var taskEvidenceRows = await _db.Evidences.AsNoTracking()
            .Where(e => e.HospitalId == hospitalId && e.FrameworkId == frameworkId && taskIds.Contains(e.TaskId))
            .Select(e => new { e.Id, e.TaskId })
            .ToListAsync(cancellationToken);

        var oeLinks = await (
                from link in _db.ObjectiveElementEvidences.AsNoTracking()
                join e in _db.Evidences.AsNoTracking() on link.EvidenceId equals e.Id
                join oe in _db.ObjectiveElements.AsNoTracking() on link.ObjectiveElementId equals oe.Id
                join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
                join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
                where oeIds.Contains(link.ObjectiveElementId)
                      && e.HospitalId == hospitalId
                      && e.FrameworkId == frameworkId
                      && ch.FrameworkId == frameworkId
                      && oe.IsApplicable
                select new { link.EvidenceId, link.ObjectiveElementId, link.VerificationStatus, link.LinkedAt })
            .ToListAsync(cancellationToken);

        var bestLinkByEvidenceAndOe = oeLinks
            .GroupBy(x => (x.EvidenceId, x.ObjectiveElementId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.LinkedAt).First());

        var oeEvidenceIdsByOe = oeLinks
            .GroupBy(x => x.ObjectiveElementId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.EvidenceId).Distinct().ToHashSet());

        var taskEvidenceIdsByTask = taskEvidenceRows
            .GroupBy(x => x.TaskId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToHashSet());

        var result = new Dictionary<Guid, (int Total, int Approved, int PendingReview, int RejectedOrNeedsMore)>();
        foreach (var joinRow in rows)
        {
            var tid = joinRow.Task.Id;
            var oeId = joinRow.Task.ObjectiveElementId;
            var merged = new HashSet<Guid>();
            if (taskEvidenceIdsByTask.TryGetValue(tid, out var te))
            {
                foreach (var id in te)
                {
                    merged.Add(id);
                }
            }

            if (oeEvidenceIdsByOe.TryGetValue(oeId, out var oeSet))
            {
                foreach (var id in oeSet)
                {
                    merged.Add(id);
                }
            }

            var approved = 0;
            var pendingReview = 0;
            var rejectedOrNeedsMore = 0;
            foreach (var eid in merged)
            {
                if (!bestLinkByEvidenceAndOe.TryGetValue((eid, oeId), out var linkRow))
                {
                    pendingReview++;
                    continue;
                }

                switch (linkRow.VerificationStatus)
                {
                    case EvidenceVerificationStatus.Verified:
                        approved++;
                        break;
                    case EvidenceVerificationStatus.Pending:
                    case EvidenceVerificationStatus.Submitted:
                    case EvidenceVerificationStatus.Draft:
                        pendingReview++;
                        break;
                    case EvidenceVerificationStatus.Rejected:
                    case EvidenceVerificationStatus.NeedsMoreEvidence:
                        rejectedOrNeedsMore++;
                        break;
                }
            }

            result[tid] = (merged.Count, approved, pendingReview, rejectedOrNeedsMore);
        }

        return result;
    }

    private TaskListItemResponseDto MapListItem(
        TaskJoinResult row,
        DateTime todayUtc,
        int evidenceTotalCount = 0,
        int evidenceApprovedCount = 0,
        int evidencePendingReviewCount = 0,
        TaskRemediationContext? remediation = null)
    {
        var t = row.Task;
        var apiStatus = TaskStatusMapper.ToApi(t.Status);
        var isOverdue = TaskRiskCalculator.IsOverdue(t.DueDate, t.Status, todayUtc);
        var score = TaskRiskCalculator.ComputeScore(t.DueDate, t.Status, t.Criticality, t.Priority, todayUtc);
        var riskLevel = TaskRiskCalculator.ToRiskLevel(score);

        return new TaskListItemResponseDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            ObjectiveElementId = t.ObjectiveElementId,
            ObjectiveElementCode = row.OeCode,
            ObjectiveElementTitle = row.OeTitle,
            ChapterId = row.ChapterId,
            AssignedToUserId = t.AssignedToUserId,
            AssignedToDisplayName = ResolveAssigneeDisplayName(t.AssignedToUserId),
            DepartmentId = t.DepartmentId,
            DepartmentName = t.DepartmentName,
            Priority = t.Priority,
            Status = apiStatus,
            RiskLevel = riskLevel,
            RiskScore = score,
            IsOverdue = isOverdue,
            DueDate = t.DueDate,
            CreatedAt = t.CreatedAt,
            RowVersion = RowVersionCodec.ToBase64(t.RowVersion),
            Criticality = t.Criticality,
            EvidenceTotalCount = evidenceTotalCount,
            EvidenceApprovedCount = evidenceApprovedCount,
            EvidencePendingReviewCount = evidencePendingReviewCount,
            SourceDeficiencyId = t.SourceDeficiencyId,
            RelatedDeficiency = remediation?.RelatedDeficiency,
            RelatedObligation = remediation?.RelatedObligation,
            MaturityStageLabel = remediation?.MaturityStageLabel
                ?? TaskRemediationMapper.MapMaturityStageLabel(t.MaturityStage),
            Category = t.Category,
            TaskKind = t.TaskKind,
            BlockedByTaskId = t.BlockedByTaskId,
            IsBlockedByDependency = t.BlockedByTaskId is { } bid && bid != Guid.Empty
        };
    }

    private async Task<Dictionary<Guid, TaskRemediationContext>> LoadRemediationContextAsync(
        IReadOnlyList<TaskJoinResult> rows,
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return new Dictionary<Guid, TaskRemediationContext>();
        }

        var deficiencyIds = rows
            .Where(r => r.Task.SourceDeficiencyId.HasValue)
            .Select(r => r.Task.SourceDeficiencyId!.Value)
            .Distinct()
            .ToList();

        var deficiencies = deficiencyIds.Count == 0
            ? new Dictionary<Guid, ComplianceDeficiency>()
            : await _db.ComplianceDeficiencies
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(d => deficiencyIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, cancellationToken);

        var oeIds = rows.Select(r => r.Task.ObjectiveElementId).Distinct().ToList();
        var obligations = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.HospitalId == hospitalId && oeIds.Contains(o.ObjectiveElementId))
            .ToListAsync(cancellationToken);

        var obligationByOe = obligations
            .GroupBy(o => o.ObjectiveElementId)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new Dictionary<Guid, TaskRemediationContext>();
        foreach (var row in rows)
        {
            ComplianceDeficiency? deficiency = null;
            if (row.Task.SourceDeficiencyId is { } defId)
            {
                deficiencies.TryGetValue(defId, out deficiency);
            }

            obligationByOe.TryGetValue(row.Task.ObjectiveElementId, out var obligation);
            string? nextAction = null;
            if (obligation != null)
            {
                var na = _nextActionService.GetNextAction(
                    obligation.AssessmentStatus,
                    obligation.ImplementationStatus,
                    obligation.JourneyStatus,
                    obligation.EvidenceStatus,
                    obligation.VerificationStatus);
                nextAction = na.Label;
            }

            result[row.Task.Id] = new TaskRemediationContext
            {
                RelatedDeficiency = TaskRemediationMapper.MapDeficiency(deficiency),
                RelatedObligation = TaskRemediationMapper.MapObligation(obligation, nextAction),
                MaturityStageLabel = TaskRemediationMapper.MapMaturityStageLabel(
                    row.Task.MaturityStage ?? deficiency?.MaturityLevel)
            };
        }

        return result;
    }

    private sealed class TaskRemediationContext
    {
        public TaskRelatedDeficiencyDto? RelatedDeficiency { get; init; }
        public TaskRelatedObligationDto? RelatedObligation { get; init; }
        public string? MaturityStageLabel { get; init; }
    }

    private string? ResolveAssigneeDisplayName(string assignedToUserId)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (string.IsNullOrEmpty(sub) || !string.Equals(sub, assignedToUserId, StringComparison.Ordinal))
        {
            return null;
        }

        return user.FindFirstValue("name")
               ?? user.FindFirstValue(ClaimTypes.Name)
               ?? user.FindFirstValue("preferred_username")
               ?? user.FindFirstValue(ClaimTypes.Email);
    }

    private static PagedResultDto<TaskEvidenceItemDto> EmptyEvidencePage(int page, int pageSize) =>
        new()
        {
            Items = Array.Empty<TaskEvidenceItemDto>(),
            TotalCount = 0,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 200)
        };

    private static string FileNameFromUrl(string? fileUrl, string? titleFallback)
    {
        if (!string.IsNullOrWhiteSpace(fileUrl))
        {
            try
            {
                return Path.GetFileName(fileUrl);
            }
            catch
            {
                // ignore
            }
        }

        return string.IsNullOrWhiteSpace(titleFallback) ? "file" : titleFallback.Trim();
    }

    private static string MapVerificationStatus(EvidenceVerificationStatus s) =>
        EvidenceStatusPresentationMapper.ToApiKey(s);
}
