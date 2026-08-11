using System.IO;
using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Presentation;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Journey;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class EvidenceLibraryService : IEvidenceLibraryService
{
    private readonly ComplianceDbContext _db;
    private readonly IEvidenceService _evidenceService;
    private readonly IFileStorageService _fileStorage;

    public EvidenceLibraryService(ComplianceDbContext db, IEvidenceService evidenceService, IFileStorageService fileStorage)
    {
        _db = db;
        _evidenceService = evidenceService;
        _fileStorage = fileStorage;
    }

    public async Task<EvidenceSummaryDto> GetSummaryAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);
        return await GetSummaryCoreAsync(hospitalId, fwId, cancellationToken);
    }

    private async Task<EvidenceSummaryDto> GetSummaryCoreAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var total = await _db.Evidences.AsNoTracking()
            .CountAsync(e => e.HospitalId == hospitalId && e.FrameworkId == frameworkId, cancellationToken);

        var linkQuery = from l in _db.ObjectiveElementEvidences.AsNoTracking()
            join e in _db.Evidences.AsNoTracking() on l.EvidenceId equals e.Id
            where e.HospitalId == hospitalId && e.FrameworkId == frameworkId
            select l;

        var approved = await linkQuery.CountAsync(l => l.VerificationStatus == EvidenceVerificationStatus.Verified, cancellationToken);
        var pendingReview = await linkQuery.CountAsync(l =>
            l.VerificationStatus == EvidenceVerificationStatus.Pending
            || l.VerificationStatus == EvidenceVerificationStatus.Submitted
            || l.VerificationStatus == EvidenceVerificationStatus.Draft
            || l.VerificationStatus == EvidenceVerificationStatus.NeedsMoreEvidence, cancellationToken);
        var rejected = await linkQuery.CountAsync(l => l.VerificationStatus == EvidenceVerificationStatus.Rejected, cancellationToken);

        var missing = await CountMissingObjectiveElementsAsync(hospitalId, frameworkId, cancellationToken);

        return new EvidenceSummaryDto
        {
            Total = total,
            Approved = approved,
            PendingReview = pendingReview,
            Rejected = rejected,
            Missing = missing
        };
    }

    public async Task<PagedResultDto<EvidenceListItemDto>> GetListAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid? objectiveElementId,
        Guid? chapterId,
        Guid? standardId,
        string? status,
        Guid? departmentId,
        string? fileType,
        string? search,
        int page,
        int pageSize,
        string? sortBy,
        string? sortOrder,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var baseQuery = BuildEvidenceListScalarQuery(hospitalId, fwId);
        baseQuery = ApplyListFilters(baseQuery, objectiveElementId, chapterId, standardId, status, departmentId, fileType, search);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var ascending = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        var key = sortBy?.Trim().ToLowerInvariant() ?? "uploaddate";

        var ordered = ApplyListSort(baseQuery, key, ascending);

        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.ConvertAll(row => new EvidenceListItemDto
        {
            Id = row.LinkId,
            EvidenceId = row.EvidenceId,
            ObjectiveElementId = row.ObjectiveElementId,
            FileName = EvidenceLibraryQueryHelper.FileNameFromUrl(row.FileUrl, row.Title),
            FileSizeBytes = 0,
            FileType = EvidenceLibraryQueryHelper.NormalizeFileType(row.FileUrl),
            ObjectiveElementCode = row.ObjectiveElementCode ?? string.Empty,
            ObjectiveElementName = row.ObjectiveElementName ?? string.Empty,
            ChapterCode = row.ChapterCode ?? string.Empty,
            ChapterName = row.ChapterName ?? string.Empty,
            DepartmentId = row.TaskDepartmentId,
            Department = row.TaskId != null
                ? (row.TaskDeptName ?? row.EvidenceDepartment ?? string.Empty)
                : (row.EvidenceDepartment ?? string.Empty),
            UploadedBy = row.UploadedBy,
            UploadDate = row.UploadedOn,
            Status = EvidenceLibraryQueryHelper.MapVerificationStatus(row.VerificationStatus),
            IsApproved = EvidenceLibraryQueryHelper.ToIsApproved(row.VerificationStatus),
            ReviewComment = row.ReviewComment,
            RowVersion = EvidenceLibraryQueryHelper.EncodeRowVersion(row.RowVersion),
            VerifiedBy = row.VerifiedBy?.ToString(),
            VerifiedAt = row.VerifiedAt,
            LinkedTask = row.TaskTitle,
            Description = row.Description,
            Version = row.CurrentVersion
        });

        return new PagedResultDto<EvidenceListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EvidenceAnalyticsDto> GetAnalyticsAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);
        return await GetAnalyticsCoreAsync(hospitalId, fwId, cancellationToken);
    }

    private async Task<EvidenceAnalyticsDto> GetAnalyticsCoreAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var linkQuery = from l in _db.ObjectiveElementEvidences.AsNoTracking()
            join e in _db.Evidences.AsNoTracking() on l.EvidenceId equals e.Id
            join oe in _db.ObjectiveElements.AsNoTracking() on l.ObjectiveElementId equals oe.Id
            join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
            join t in _db.ComplianceTasks.AsNoTracking() on e.TaskId equals t.Id into tj
            from task in tj.DefaultIfEmpty()
            where e.HospitalId == hospitalId && e.FrameworkId == frameworkId && ch.FrameworkId == frameworkId
            select new { l, e, ch, st, task };

        var byChapter = await linkQuery
            .GroupBy(x => new { x.ch.Id, x.ch.Code, x.ch.Name })
            .Select(g => new EvidenceChapterAggregateDto
            {
                ChapterId = g.Key.Id,
                Name = string.IsNullOrWhiteSpace(g.Key.Code) ? g.Key.Name : $"{g.Key.Code} - {g.Key.Name}",
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        // Project raw group keys and sort server-side on Code/Name; format the display
        // name client-side. Ordering the projected `Name` directly forces EF to translate
        // the string interpolation into SQL, which fails (string.Format is not mapped).
        var byStandardRaw = await linkQuery
            .GroupBy(x => new { x.st.Id, x.st.Code, x.st.Name, ChapterId = x.ch.Id })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Code,
                g.Key.Name,
                g.Key.ChapterId,
                Count = g.Count()
            })
            .OrderBy(x => x.Code).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var byStandard = byStandardRaw.ConvertAll(g => new EvidenceStandardAggregateDto
        {
            StandardId = g.Id,
            ChapterId = g.ChapterId,
            Name = string.IsNullOrWhiteSpace(g.Code) ? g.Name : $"{g.Code} - {g.Name}",
            Count = g.Count
        });

        var byDepartment = await (
            from x in linkQuery
            group x by new
            {
                DepartmentId = x.task != null ? x.task.DepartmentId : null,
                Name = x.task != null && !string.IsNullOrEmpty(x.task.DepartmentName)
                    ? x.task.DepartmentName!
                    : (x.e.Department ?? "Unassigned")
            }
            into g
            orderby g.Count() descending
            select new EvidenceDepartmentAggregateDto
            {
                DepartmentId = g.Key.DepartmentId,
                Name = g.Key.Name,
                Count = g.Count()
            }).ToListAsync(cancellationToken);

        var approvalStatus = await linkQuery
            .GroupBy(x => x.l.VerificationStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var approval = new EvidenceApprovalStatusDto
        {
            Approved = approvalStatus.FirstOrDefault(x => x.Status == EvidenceVerificationStatus.Verified)?.Count ?? 0,
            PendingReview = approvalStatus.FirstOrDefault(x => x.Status == EvidenceVerificationStatus.Pending)?.Count ?? 0,
            Rejected = approvalStatus.FirstOrDefault(x => x.Status == EvidenceVerificationStatus.Rejected)?.Count ?? 0
        };

        var missing = await GetMissingObjectiveElementsAsync(hospitalId, frameworkId, cancellationToken);

        return new EvidenceAnalyticsDto
        {
            ByChapter = byChapter,
            ByStandard = byStandard,
            ByDepartment = byDepartment,
            ApprovalStatus = approval,
            MissingEvidence = missing
        };
    }

    public async Task<IReadOnlyList<EvidenceActivityItemDto>> GetRecentActivityAsync(
        Guid hospitalId,
        Guid frameworkId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);
        return await GetRecentActivityCoreAsync(hospitalId, fwId, take, cancellationToken);
    }

    private async Task<IReadOnlyList<EvidenceActivityItemDto>> GetRecentActivityCoreAsync(
        Guid hospitalId,
        Guid frameworkId,
        int take,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 20);

        var uploadRows = await _db.Evidences.AsNoTracking()
            .Where(e => e.HospitalId == hospitalId && e.FrameworkId == frameworkId)
            .OrderByDescending(e => e.UploadedOn)
            .Take(80)
            .Select(e => new { e.UploadedBy, e.FileUrl, e.Title, e.UploadedOn })
            .ToListAsync(cancellationToken);

        var uploads = uploadRows.ConvertAll(e => new EvidenceActivityItemDto
        {
            User = e.UploadedBy,
            Action = "Uploaded",
            Entity = EvidenceLibraryQueryHelper.FileNameFromUrl(e.FileUrl, e.Title),
            Timestamp = e.UploadedOn
        });

        var reviewRows = await (
            from l in _db.ObjectiveElementEvidences.AsNoTracking()
            join e in _db.Evidences.AsNoTracking() on l.EvidenceId equals e.Id
            where e.HospitalId == hospitalId && e.FrameworkId == frameworkId && l.VerifiedAt != null
            orderby l.VerifiedAt descending
            select new
            {
                l.VerifiedBy,
                l.VerificationStatus,
                e.FileUrl,
                e.Title,
                l.VerifiedAt
            })
            .Take(80)
            .ToListAsync(cancellationToken);

        var reviews = reviewRows.ConvertAll(r => new EvidenceActivityItemDto
        {
            User = r.VerifiedBy.HasValue ? r.VerifiedBy.Value.ToString() : string.Empty,
            Action = r.VerificationStatus == EvidenceVerificationStatus.Verified ? "Approved" : "Rejected",
            Entity = EvidenceLibraryQueryHelper.FileNameFromUrl(r.FileUrl, r.Title),
            Timestamp = r.VerifiedAt!.Value
        });

        var merged = uploads.Concat(reviews)
            .OrderByDescending(x => x.Timestamp)
            .Take(take)
            .ToList();

        return merged;
    }

    public async Task<IReadOnlyList<EvidenceTimelineItemDto>> GetRecentTimelineAsync(
        Guid hospitalId,
        Guid frameworkId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);
        return await GetRecentTimelineCoreAsync(hospitalId, fwId, take, cancellationToken);
    }

    /// <summary>
    /// Performance-first timeline query:
    /// 1) Base join over link/evidence/OE/standard/chapter.
    /// 2) Correlated scalar subquery for latest EvidenceVersion timestamp
    ///    (single SQL round-trip; translated to a scalar sub-select).
    /// 3) MostRecentAt computed server-side via SQL CASE over non-nullable locals
    ///    (COALESCE fills in defaults), so ORDER BY + TAKE stay in SQL.
    /// 4) UpdatedBy resolved for the returned rows only via one targeted follow-up
    ///    query keyed on (EvidenceId, CreatedAt) - no N+1.
    /// </summary>
    private async Task<IReadOnlyList<EvidenceTimelineItemDto>> GetRecentTimelineCoreAsync(
        Guid hospitalId,
        Guid frameworkId,
        int take,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 20);

        // Correlated subquery: returns MAX(CreatedAt) per evidence, or NULL when no versions.
        // This form is translated reliably by EF Core SQL Server provider.
        var projected =
            from link in _db.ObjectiveElementEvidences.AsNoTracking()
            join e in _db.Evidences.AsNoTracking() on link.EvidenceId equals e.Id
            join oe in _db.ObjectiveElements.AsNoTracking() on link.ObjectiveElementId equals oe.Id
            join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
            where e.HospitalId == hospitalId
                && e.FrameworkId == frameworkId
                && ch.FrameworkId == frameworkId
            let latestVersionAt =
                _db.EvidenceVersions
                    .Where(v => v.EvidenceId == e.Id)
                    .Max(v => (DateTime?)v.CreatedAt)
            let effectiveUpdatedAt = latestVersionAt ?? e.UploadedOn
            let effectiveVerifiedAt = link.VerifiedAt ?? DateTime.MinValue
            select new EvidenceTimelineScalarRow
            {
                EvidenceId = e.Id,
                ObjectiveElementId = oe.Id,
                ObjectiveElementCode = oe.Code,
                ObjectiveElementName = oe.Title,
                ChapterCode = ch.Code,
                FileUrl = e.FileUrl,
                Title = e.Title,
                UploadedBy = e.UploadedBy,
                UploadedOn = e.UploadedOn,
                LatestVersionAt = latestVersionAt,
                VerificationStatus = link.VerificationStatus,
                VerifiedBy = link.VerifiedBy,
                VerifiedAt = link.VerifiedAt,
                ReviewComment = link.ReviewComment ?? link.Remarks,
                MostRecentAt =
                    (effectiveVerifiedAt >= effectiveUpdatedAt && effectiveVerifiedAt >= e.UploadedOn)
                        ? effectiveVerifiedAt
                        : (effectiveUpdatedAt >= e.UploadedOn ? effectiveUpdatedAt : e.UploadedOn)
            };

        var rows = await projected
            .OrderByDescending(x => x.MostRecentAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        // One targeted query to resolve UpdatedBy for the rows that have a latest version.
        var updatedByByEvidence = new Dictionary<Guid, Guid>();
        var rowsWithVersion = rows.Where(r => r.LatestVersionAt.HasValue).ToList();
        if (rowsWithVersion.Count > 0)
        {
            var evidenceIds = rowsWithVersion.Select(r => r.EvidenceId).Distinct().ToArray();
            var versionRows = await _db.EvidenceVersions.AsNoTracking()
                .Where(v => evidenceIds.Contains(v.EvidenceId))
                .Select(v => new { v.EvidenceId, v.CreatedAt, v.CreatedBy })
                .ToListAsync(cancellationToken);

            var versionsByEvidence = versionRows.ToLookup(v => v.EvidenceId);
            foreach (var row in rowsWithVersion)
            {
                var match = versionsByEvidence[row.EvidenceId]
                    .FirstOrDefault(v => v.CreatedAt == row.LatestVersionAt!.Value);
                if (match != null && match.CreatedBy != Guid.Empty)
                {
                    updatedByByEvidence[row.EvidenceId] = match.CreatedBy;
                }
            }
        }

        return rows.ConvertAll(row =>
        {
            var updatedAt = row.LatestVersionAt ?? row.UploadedOn;
            var updatedBy = updatedByByEvidence.TryGetValue(row.EvidenceId, out var latestAuthor)
                ? latestAuthor.ToString()
                : row.UploadedBy;
            return new EvidenceTimelineItemDto
            {
                EvidenceId = row.EvidenceId,
                ObjectiveElementId = row.ObjectiveElementId,
                ObjectiveElementCode = row.ObjectiveElementCode ?? string.Empty,
                ObjectiveElementName = row.ObjectiveElementName ?? string.Empty,
                ChapterCode = row.ChapterCode ?? string.Empty,
                FileName = EvidenceLibraryQueryHelper.FileNameFromUrl(row.FileUrl, row.Title),
                FileType = EvidenceLibraryQueryHelper.NormalizeFileType(row.FileUrl),
                FileUrl = row.FileUrl,
                UploadedBy = row.UploadedBy,
                UploadedAt = row.UploadedOn,
                UpdatedBy = updatedBy,
                UpdatedAt = updatedAt,
                Status = EvidenceLibraryQueryHelper.MapVerificationStatus(row.VerificationStatus),
                VerifiedBy = row.VerifiedBy?.ToString(),
                VerifiedAt = row.VerifiedAt,
                ReviewComment = row.ReviewComment,
                HasUpdate = updatedAt > row.UploadedOn,
                MostRecentAt = row.MostRecentAt
            };
        });
    }

    public async Task<EvidenceDashboardDto> GetDashboardAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);
        var summary = await GetSummaryCoreAsync(hospitalId, fwId, cancellationToken);
        var analytics = await GetAnalyticsCoreAsync(hospitalId, fwId, cancellationToken);
        var recent = await GetRecentActivityCoreAsync(hospitalId, fwId, 20, cancellationToken);
        var timeline = await GetRecentTimelineCoreAsync(hospitalId, fwId, 10, cancellationToken);

        return new EvidenceDashboardDto
        {
            Summary = summary,
            Analytics = analytics,
            RecentActivity = recent,
            RecentTimeline = timeline
        };
    }

    public async Task<EvidenceUploadContextDto> GetUploadContextAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);
        var journey = await ComplianceJourneySnapshotResolver.GetActiveJourneyForSnapshotAsync(_db, hospitalId, fwId, cancellationToken);
        return new EvidenceUploadContextDto { JourneyId = journey?.Id };
    }

    public async Task<EvidenceDetailDto?> GetLibraryEvidenceDetailAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid evidenceId,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);
        var oeId = await ResolveObjectiveElementIdForLibraryEvidenceAsync(hospitalId, fwId, evidenceId, cancellationToken);
        if (!oeId.HasValue)
        {
            return null;
        }

        return await _evidenceService.GetEvidenceDetailAsync(oeId.Value, evidenceId, cancellationToken);
    }

    public async Task<(Stream Stream, string ContentType, string FileDownloadName)?> DownloadLibraryEvidenceAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid evidenceId,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkOrJourneyIdAsync(hospitalId, frameworkId, cancellationToken);
        var oeId = await ResolveObjectiveElementIdForLibraryEvidenceAsync(hospitalId, fwId, evidenceId, cancellationToken);
        if (!oeId.HasValue)
        {
            return null;
        }

        var ev = await _db.Evidences.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evidenceId && e.HospitalId == hospitalId && e.FrameworkId == fwId, cancellationToken);
        if (ev == null || string.IsNullOrWhiteSpace(ev.FileUrl))
        {
            return null;
        }

        var stream = await _fileStorage.OpenReadAsync(ev.FileUrl, cancellationToken);
        TryGetDownloadHeaders(ev.Metadata, ev.Title, evidenceId, out var contentType, out var fileName);
        return (stream, contentType, fileName);
    }

    private async Task<Guid?> ResolveObjectiveElementIdForLibraryEvidenceAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid evidenceId,
        CancellationToken cancellationToken)
    {
        return await (
                from l in _db.ObjectiveElementEvidences.AsNoTracking()
                join e in _db.Evidences.AsNoTracking() on l.EvidenceId equals e.Id
                join oe in _db.ObjectiveElements.AsNoTracking() on l.ObjectiveElementId equals oe.Id
                join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
                join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
                where l.EvidenceId == evidenceId
                      && e.HospitalId == hospitalId
                      && e.FrameworkId == frameworkId
                      && ch.FrameworkId == frameworkId
                orderby l.ObjectiveElementId
                select (Guid?)l.ObjectiveElementId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void TryGetDownloadHeaders(
        string? metadataJson,
        string? title,
        Guid evidenceId,
        out string contentType,
        out string fileDownloadName)
    {
        contentType = "application/octet-stream";
        fileDownloadName = $"{evidenceId:N}";
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                fileDownloadName = title!;
            }

            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("contentType", out var ct) && ct.ValueKind == JsonValueKind.String)
            {
                var s = ct.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    contentType = s!;
                }
            }

            if (root.TryGetProperty("originalFileName", out var on) && on.ValueKind == JsonValueKind.String)
            {
                var s = on.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    fileDownloadName = s!;
                    return;
                }
            }
        }
        catch
        {
            // fall through
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            fileDownloadName = title!;
        }
    }

    private Task<Guid> ResolveFrameworkOrJourneyIdAsync(
        Guid hospitalId,
        Guid frameworkOrJourneyId,
        CancellationToken cancellationToken) =>
        ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(_db, hospitalId, frameworkOrJourneyId, cancellationToken);

    private async Task<int> CountMissingObjectiveElementsAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken)
    {
        return await _db.ObjectiveElements.AsNoTracking()
            .Where(oe =>
                _db.ComplianceStandards.Any(st => st.Id == oe.ComplianceStandardId &&
                    _db.ComplianceChapters.Any(ch => ch.Id == st.ChapterId && ch.FrameworkId == frameworkId)) &&
                !_db.ObjectiveElementEvidences.Any(l =>
                    l.ObjectiveElementId == oe.Id &&
                    _db.Evidences.Any(e => e.Id == l.EvidenceId && e.HospitalId == hospitalId && e.FrameworkId == frameworkId)))
            .CountAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<EvidenceMissingItemDto>> GetMissingObjectiveElementsAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        return await (
            from oe in _db.ObjectiveElements.AsNoTracking()
            join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
            where ch.FrameworkId == frameworkId && oe.IsApplicable
            where !_db.ObjectiveElementEvidences.Any(l =>
                l.ObjectiveElementId == oe.Id &&
                _db.Evidences.Any(e => e.Id == l.EvidenceId && e.HospitalId == hospitalId && e.FrameworkId == frameworkId))
            orderby oe.Code
            select new EvidenceMissingItemDto
            {
                ObjectiveElementId = oe.Id,
                Code = oe.Code,
                Name = oe.Title,
                Chapter = ch.Code + " - " + ch.Name
            })
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Flat scalar projection only — EF cannot reliably translate Count/OrderBy/Skip when the shape holds full entity instances.
    /// </summary>
    private IQueryable<EvidenceListScalarRow> BuildEvidenceListScalarQuery(Guid hospitalId, Guid frameworkId)
    {
        return from link in _db.ObjectiveElementEvidences.AsNoTracking()
            join e in _db.Evidences.AsNoTracking() on link.EvidenceId equals e.Id
            join oe in _db.ObjectiveElements.AsNoTracking() on link.ObjectiveElementId equals oe.Id
            join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
            join t in _db.ComplianceTasks.AsNoTracking() on e.TaskId equals t.Id into tj
            from task in tj.DefaultIfEmpty()
            where e.HospitalId == hospitalId && e.FrameworkId == frameworkId && ch.FrameworkId == frameworkId
            select new EvidenceListScalarRow
            {
                LinkId = link.Id,
                ObjectiveElementId = link.ObjectiveElementId,
                VerificationStatus = link.VerificationStatus,
                ReviewComment = link.ReviewComment ?? link.Remarks,
                RowVersion = link.RowVersion,
                VerifiedBy = link.VerifiedBy,
                VerifiedAt = link.VerifiedAt,
                EvidenceId = e.Id,
                FileUrl = e.FileUrl,
                Title = e.Title,
                ObjectiveElementCode = oe.Code,
                ObjectiveElementName = oe.Title,
                StandardId = st.Id,
                ChapterId = ch.Id,
                ChapterCode = ch.Code,
                ChapterName = ch.Name,
                TaskId = task != null ? task.Id : null,
                TaskDepartmentId = task != null ? task.DepartmentId : null,
                TaskDeptName = task != null ? task.DepartmentName : null,
                EvidenceDepartment = e.Department,
                UploadedBy = e.UploadedBy,
                UploadedOn = e.UploadedOn,
                TaskTitle = task != null ? task.Title : null,
                Description = e.Description,
                CurrentVersion = e.CurrentVersion
            };
    }

    private static IQueryable<EvidenceListScalarRow> ApplyListFilters(
        IQueryable<EvidenceListScalarRow> query,
        Guid? objectiveElementId,
        Guid? chapterId,
        Guid? standardId,
        string? status,
        Guid? departmentId,
        string? fileType,
        string? search)
    {
        if (objectiveElementId.HasValue && objectiveElementId.Value != Guid.Empty)
        {
            query = query.Where(x => x.ObjectiveElementId == objectiveElementId.Value);
        }

        if (chapterId.HasValue && chapterId.Value != Guid.Empty)
        {
            query = query.Where(x => x.ChapterId == chapterId.Value);
        }

        if (standardId.HasValue && standardId.Value != Guid.Empty)
        {
            query = query.Where(x => x.StandardId == standardId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            if (s == "approved")
            {
                query = query.Where(x => x.VerificationStatus == EvidenceVerificationStatus.Verified);
            }
            else if (s == "pendingreview")
            {
                query = query.Where(x =>
                    x.VerificationStatus == EvidenceVerificationStatus.Pending
                    || x.VerificationStatus == EvidenceVerificationStatus.Submitted
                    || x.VerificationStatus == EvidenceVerificationStatus.Draft);
            }
            else if (s == "needsmoreevidence" || s == "needs-more")
            {
                query = query.Where(x => x.VerificationStatus == EvidenceVerificationStatus.NeedsMoreEvidence);
            }
            else if (s == "rejected")
            {
                query = query.Where(x => x.VerificationStatus == EvidenceVerificationStatus.Rejected);
            }
        }

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            query = query.Where(x =>
                x.TaskId != null && x.TaskDepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(fileType))
        {
            var ft = fileType.Trim().ToLowerInvariant();
            if (ft == "jpg")
            {
                query = query.Where(x =>
                    (x.FileUrl ?? "").EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    (x.FileUrl ?? "").EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                    (x.FileUrl ?? "").Contains(".jpg?", StringComparison.OrdinalIgnoreCase) ||
                    (x.FileUrl ?? "").Contains(".jpeg?", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var suffix = "." + ft;
                query = query.Where(x =>
                    (x.FileUrl ?? "").EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ||
                    (x.FileUrl ?? "").Contains(suffix + "?", StringComparison.OrdinalIgnoreCase));
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = "%" + term.Replace("%", "[%]").Replace("_", "[_]") + "%";
            query = query.Where(x =>
                EF.Functions.Like(x.Title ?? "", pattern) ||
                EF.Functions.Like(x.FileUrl ?? "", pattern) ||
                EF.Functions.Like(x.ObjectiveElementCode ?? "", pattern) ||
                EF.Functions.Like(x.ObjectiveElementName ?? "", pattern) ||
                EF.Functions.Like(x.ChapterName ?? "", pattern) ||
                EF.Functions.Like(x.ChapterCode ?? "", pattern) ||
                (x.EvidenceDepartment != null && EF.Functions.Like(x.EvidenceDepartment, pattern)) ||
                (x.TaskDeptName != null && EF.Functions.Like(x.TaskDeptName, pattern)));
        }

        return query;
    }

    private static IQueryable<EvidenceListScalarRow> ApplyListSort(IQueryable<EvidenceListScalarRow> query, string sortKey, bool ascending)
    {
        return sortKey switch
        {
            "filename" => ascending
                ? query.OrderBy(x => x.Title ?? x.FileUrl)
                : query.OrderByDescending(x => x.Title ?? x.FileUrl),
            "status" => ascending ? query.OrderBy(x => x.VerificationStatus) : query.OrderByDescending(x => x.VerificationStatus),
            "chapter" => ascending ? query.OrderBy(x => x.ChapterCode) : query.OrderByDescending(x => x.ChapterCode),
            "department" => ascending
                ? query.OrderBy(x => x.TaskId != null ? x.TaskDeptName ?? x.EvidenceDepartment : x.EvidenceDepartment)
                : query.OrderByDescending(x => x.TaskId != null ? x.TaskDeptName ?? x.EvidenceDepartment : x.EvidenceDepartment),
            "objectiveelementcode" => ascending ? query.OrderBy(x => x.ObjectiveElementCode) : query.OrderByDescending(x => x.ObjectiveElementCode),
            "uploaddate" or _ => ascending ? query.OrderBy(x => x.UploadedOn) : query.OrderByDescending(x => x.UploadedOn)
        };
    }

    private sealed class EvidenceTimelineScalarRow
    {
        public Guid EvidenceId { get; init; }
        public Guid ObjectiveElementId { get; init; }
        public string? ObjectiveElementCode { get; init; }
        public string? ObjectiveElementName { get; init; }
        public string? ChapterCode { get; init; }
        public string? FileUrl { get; init; }
        public string? Title { get; init; }
        public string UploadedBy { get; init; } = string.Empty;
        public DateTime UploadedOn { get; init; }
        public DateTime? LatestVersionAt { get; init; }
        public EvidenceVerificationStatus VerificationStatus { get; init; }
        public Guid? VerifiedBy { get; init; }
        public DateTime? VerifiedAt { get; init; }
        public string? ReviewComment { get; init; }
        public DateTime MostRecentAt { get; init; }
    }

    private sealed class EvidenceListScalarRow
    {
        public Guid LinkId { get; init; }
        public Guid ObjectiveElementId { get; init; }
        public EvidenceVerificationStatus VerificationStatus { get; init; }
        public string? ReviewComment { get; init; }
        public byte[]? RowVersion { get; init; }
        public Guid? VerifiedBy { get; init; }
        public DateTime? VerifiedAt { get; init; }
        public Guid EvidenceId { get; init; }
        public string? FileUrl { get; init; }
        public string? Title { get; init; }
        public string? ObjectiveElementCode { get; init; }
        public string? ObjectiveElementName { get; init; }
        public Guid StandardId { get; init; }
        public Guid ChapterId { get; init; }
        public string? ChapterCode { get; init; }
        public string? ChapterName { get; init; }
        public Guid? TaskId { get; init; }
        public Guid? TaskDepartmentId { get; init; }
        public string? TaskDeptName { get; init; }
        public string? EvidenceDepartment { get; init; }
        public string UploadedBy { get; init; } = "";
        public DateTime UploadedOn { get; init; }
        public string? TaskTitle { get; init; }
        public string? Description { get; init; }
        public int CurrentVersion { get; init; }
    }
}

internal static class EvidenceLibraryQueryHelper
{
    public static string MapVerificationStatus(EvidenceVerificationStatus status) =>
        EvidenceStatusPresentationMapper.ToApiKey(status);

    public static bool? ToIsApproved(EvidenceVerificationStatus status) =>
        status switch
        {
            EvidenceVerificationStatus.Verified => true,
            EvidenceVerificationStatus.Rejected => false,
            _ => null
        };

    public static string? EncodeRowVersion(byte[]? rowVersion) =>
        rowVersion == null || rowVersion.Length == 0
            ? null
            : Convert.ToBase64String(rowVersion);

    public static string NormalizeFileType(string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return "unknown";
        }

        var ext = Path.GetExtension(fileUrl).TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "jpeg" => "jpg",
            "" => "unknown",
            _ => ext
        };
    }

    public static string FileNameFromUrl(string? fileUrl, string? title)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title!;
        }

        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return "Evidence";
        }

        try
        {
            var name = Path.GetFileName(fileUrl.Split('?', '#')[0]);
            return string.IsNullOrWhiteSpace(name) ? "Evidence" : name;
        }
        catch
        {
            return "Evidence";
        }
    }
}
