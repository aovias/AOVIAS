using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Enriched read projections for the Findings Workbench (Sprint 7A / 7A.1).
/// Discovery-focused — no remediation or evidence verification logic.
/// </summary>
public sealed class FindingsWorkspaceQueryService : IFindingsWorkspaceQueryService
{
    private readonly ComplianceDbContext _db;

    public FindingsWorkspaceQueryService(ComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<FindingsWorkspaceDashboardDto> GetDashboardAsync(Guid organizationId, CancellationToken ct = default)
    {
        var rows = await _db.Findings
            .AsNoTracking()
            .Where(f => f.TenantId == organizationId)
            .Select(f => new DashboardRow
            {
                Status = f.Status,
                Severity = f.Severity,
                RaisedBy = f.RaisedBy,
                Department = f.Department,
                ObjectiveElementId = f.ObjectiveElementId,
                PromotedDeficiencyId = f.PromotedDeficiencyId,
                Classification = f.Classification,
                Type = f.Type,
                Title = f.Title,
                CreatedAt = f.CreatedAt,
            })
            .ToListAsync(ct);

        var dto = new FindingsWorkspaceDashboardDto { TotalFindings = rows.Count };

        foreach (var row in rows)
        {
            var readiness = ComputeReadiness(row);

            switch (row.Status)
            {
                case FindingStatus.Open:
                case FindingStatus.InProgress:
                    dto.OpenFindings++;
                    break;
                case FindingStatus.Accepted:
                    dto.AcceptedFindings++;
                    break;
                case FindingStatus.Closed:
                case FindingStatus.Verified:
                    dto.ClosedFindings++;
                    break;
            }

            if (row.Status is FindingStatus.Draft or FindingStatus.Open or FindingStatus.Reviewed or FindingStatus.InProgress)
            {
                dto.AwaitingReview++;
            }

            if (row.Status == FindingStatus.Draft) dto.DraftFindings++;
            if (row.Severity == FindingSeverity.Critical) dto.CriticalFindings++;

            if (readiness == FindingPromotionReadiness.Promoted)
            {
                dto.PromotedFindings++;
            }
            else if (readiness == FindingPromotionReadiness.Eligible)
            {
                dto.EligibleForPromotion++;
                dto.FindingsAwaitingPromotion++;
            }
        }

        var eligibleAccepted = rows.Count(r => ComputeReadiness(r) == FindingPromotionReadiness.Eligible
            || ComputeReadiness(r) == FindingPromotionReadiness.Promoted);
        dto.PromotionRate = eligibleAccepted > 0
            ? Math.Round((decimal)dto.PromotedFindings / Math.Max(1, dto.PromotedFindings + dto.EligibleForPromotion) * 100, 1)
            : 0;

        dto.BySource = rows
            .GroupBy(r => r.RaisedBy.ToString())
            .Select(g => new FindingsCountByLabelDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        dto.ByDepartment = rows
            .GroupBy(r => r.Department.ToString())
            .Select(g => new FindingsCountByLabelDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        dto.ByClassification = rows
            .GroupBy(r => (r.Classification ?? MapLegacyType(r.Type)).ToString())
            .Select(g => new FindingsCountByLabelDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var oeIds = rows.Where(r => r.ObjectiveElementId.HasValue).Select(r => r.ObjectiveElementId!.Value).Distinct().ToList();
        if (oeIds.Count > 0)
        {
            var chapters = await (
                from oe in _db.ObjectiveElements.AsNoTracking()
                where oeIds.Contains(oe.Id)
                join std in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals std.Id
                join ch in _db.ComplianceChapters.AsNoTracking() on std.ChapterId equals ch.Id
                select new { oe.Id, ch.Code }
            ).ToListAsync(ct);

            var chapterByOe = chapters.ToDictionary(x => x.Id, x => x.Code);
            dto.ByChapter = rows
                .Where(r => r.ObjectiveElementId.HasValue && chapterByOe.ContainsKey(r.ObjectiveElementId.Value))
                .GroupBy(r => chapterByOe[r.ObjectiveElementId!.Value])
                .Select(g => new FindingsCountByLabelDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        return dto;
    }

    public async Task<FindingsWorkspaceListDto> GetWorkspaceListAsync(
        Guid organizationId,
        string? status,
        string? severity,
        string? classification,
        string? source,
        string? department,
        string? promotionReadiness,
        string? chapterCode,
        string? standardCode,
        Guid? objectiveElementId,
        string? quickFilter,
        string? search,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = BuildQuery(organizationId, status, severity, classification, source, department, chapterCode, standardCode, objectiveElementId, search);
        var allRows = await query.ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(promotionReadiness))
        {
            allRows = allRows
                .Where(r => string.Equals(ComputeReadiness(r.Finding), promotionReadiness, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(quickFilter))
        {
            allRows = ApplyQuickFilter(allRows, quickFilter).ToList();
        }

        var total = allRows.Count;
        var sorted = ApplySortInMemory(allRows, sortBy, sortDesc);
        var pageRows = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var findingIds = pageRows.Select(x => x.Finding.Id).ToList();
        var attachmentCounts = await _db.FindingAttachments
            .AsNoTracking()
            .Where(a => findingIds.Contains(a.FindingId))
            .GroupBy(a => a.FindingId)
            .Select(g => new { FindingId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FindingId, x => x.Count, ct);

        var commentCounts = await _db.FindingComments
            .AsNoTracking()
            .Where(c => findingIds.Contains(c.FindingId))
            .GroupBy(c => c.FindingId)
            .Select(g => new { FindingId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FindingId, x => x.Count, ct);

        var items = pageRows.Select(row =>
        {
            var item = MapItem(row);
            item.AttachmentCount = attachmentCounts.GetValueOrDefault(row.Finding.Id);
            item.CommentCount = commentCounts.GetValueOrDefault(row.Finding.Id);
            return item;
        }).ToList();

        return new FindingsWorkspaceListDto
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<FindingsWorkspaceDetailDto?> GetWorkspaceDetailAsync(
        Guid organizationId,
        Guid id,
        CancellationToken ct = default)
    {
        var row = await BuildQuery(organizationId, null, null, null, null, null, null, null, null, null)
            .Where(x => x.Finding.Id == id)
            .FirstOrDefaultAsync(ct);

        if (row == null) return null;

        var detail = MapDetail(row);

        detail.Comments = await _db.FindingComments
            .AsNoTracking()
            .Where(c => c.FindingId == id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new FindingCommentDto
            {
                Id = c.Id,
                AuthorUserId = c.AuthorUserId,
                Body = c.Body,
                CreatedAt = c.CreatedAt,
            })
            .ToListAsync(ct);

        detail.Attachments = await _db.FindingAttachments
            .AsNoTracking()
            .Where(a => a.FindingId == id)
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new FindingAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileUrl = a.FileUrl,
                AttachmentType = a.AttachmentType.ToString(),
                FileSizeBytes = a.FileSizeBytes,
                UploadedAt = a.UploadedAt,
            })
            .ToListAsync(ct);

        var historyRows = await _db.FindingAuditTrails
            .AsNoTracking()
            .Where(h => h.FindingId == id)
            .OrderByDescending(h => h.OccurredAt)
            .ToListAsync(ct);

        detail.History = historyRows
            .Select(h => new FindingHistoryEntryDto
            {
                Action = h.Action,
                ActorUserId = h.ActorUserId,
                ActorType = h.ActorType,
                OccurredAt = h.OccurredAt,
                Detail = h.NewStateJson ?? h.PreviousStateJson,
            })
            .ToList();

        detail.LifecycleSteps = BuildLifecycleSteps(row.Finding, historyRows);
        detail.AttachmentCount = detail.Attachments.Count;
        detail.CommentCount = detail.Comments.Count;

        return detail;
    }

    public async Task<IReadOnlyList<FindingComplianceContextOptionDto>> SearchComplianceContextAsync(
        string? search,
        string? chapterCode,
        string? standardCode,
        int limit,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var query =
            from oe in _db.ObjectiveElements.AsNoTracking()
            join std in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals std.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on std.ChapterId equals ch.Id
            where oe.IsApplicable
            select new { oe, std, ch };

        if (!string.IsNullOrWhiteSpace(chapterCode))
        {
            var chTerm = chapterCode.Trim();
            query = query.Where(x => x.ch.Code.Contains(chTerm));
        }

        if (!string.IsNullOrWhiteSpace(standardCode))
        {
            var stdTerm = standardCode.Trim();
            query = query.Where(x => x.std.Code.Contains(stdTerm));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.oe.Code.ToLower().Contains(term)
                || x.oe.Title.ToLower().Contains(term)
                || x.std.Code.ToLower().Contains(term)
                || x.ch.Code.ToLower().Contains(term));
        }

        return await query
            .OrderBy(x => x.ch.Code)
            .ThenBy(x => x.std.Code)
            .ThenBy(x => x.oe.Code)
            .Take(limit)
            .Select(x => new FindingComplianceContextOptionDto
            {
                ObjectiveElementId = x.oe.Id,
                ObjectiveElementCode = x.oe.Code,
                ObjectiveElementName = x.oe.Title,
                StandardCode = x.std.Code,
                StandardName = x.std.Name,
                ChapterCode = x.ch.Code,
                ChapterName = x.ch.Name,
            })
            .ToListAsync(ct);
    }

    private IQueryable<WorkspaceRow> BuildQuery(
        Guid organizationId,
        string? status,
        string? severity,
        string? classification,
        string? source,
        string? department,
        string? chapterCode,
        string? standardCode,
        Guid? objectiveElementId,
        string? search)
    {
        var findings = _db.Findings.AsNoTracking().Where(f => f.TenantId == organizationId);

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<FindingStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            findings = findings.Where(f => f.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(severity)
            && Enum.TryParse<FindingSeverity>(severity, ignoreCase: true, out var parsedSeverity))
        {
            findings = findings.Where(f => f.Severity == parsedSeverity);
        }

        if (!string.IsNullOrWhiteSpace(source)
            && Enum.TryParse<FindingRaisedBy>(source, ignoreCase: true, out var parsedSource))
        {
            findings = findings.Where(f => f.RaisedBy == parsedSource);
        }

        if (!string.IsNullOrWhiteSpace(department)
            && Enum.TryParse<FindingDepartment>(department, ignoreCase: true, out var parsedDept))
        {
            findings = findings.Where(f => f.Department == parsedDept);
        }

        if (!string.IsNullOrWhiteSpace(classification)
            && Enum.TryParse<FindingClassification>(classification, ignoreCase: true, out var parsedClass))
        {
            findings = findings.Where(f =>
                f.Classification == parsedClass
                || (f.Classification == null && MapLegacyType(f.Type) == parsedClass));
        }

        if (objectiveElementId.HasValue && objectiveElementId.Value != Guid.Empty)
        {
            findings = findings.Where(f => f.ObjectiveElementId == objectiveElementId.Value);
        }

        var query =
            from f in findings
            join oe in _db.ObjectiveElements.AsNoTracking()
                on f.ObjectiveElementId equals oe.Id into oeJoin
            from oe in oeJoin.DefaultIfEmpty()
            join std in _db.ComplianceStandards.AsNoTracking()
                on oe.ComplianceStandardId equals std.Id into stdJoin
            from std in stdJoin.DefaultIfEmpty()
            join ch in _db.ComplianceChapters.AsNoTracking()
                on std.ChapterId equals ch.Id into chJoin
            from ch in chJoin.DefaultIfEmpty()
            select new WorkspaceRow
            {
                Finding = f,
                OeCode = oe != null ? oe.Code : string.Empty,
                OeName = oe != null ? oe.Title : string.Empty,
                StandardCode = std != null ? std.Code : string.Empty,
                StandardName = std != null ? std.Name : string.Empty,
                ChapterCode = ch != null ? ch.Code : string.Empty,
                ChapterName = ch != null ? ch.Name : string.Empty,
            };

        if (!string.IsNullOrWhiteSpace(chapterCode))
        {
            var term = chapterCode.Trim();
            query = query.Where(x => x.ChapterCode.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(standardCode))
        {
            var term = standardCode.Trim();
            query = query.Where(x => x.StandardCode.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Finding.Title.ToLower().Contains(term)
                || x.Finding.FindingNumber.ToLower().Contains(term)
                || x.Finding.ClauseReference.ToLower().Contains(term)
                || x.OeCode.ToLower().Contains(term)
                || x.ChapterCode.ToLower().Contains(term)
                || x.StandardCode.ToLower().Contains(term));
        }

        return query;
    }

    private static IEnumerable<WorkspaceRow> ApplyQuickFilter(IReadOnlyList<WorkspaceRow> rows, string quickFilter)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        return quickFilter.ToLowerInvariant() switch
        {
            "needsreview" => rows.Where(r => r.Finding.Status is FindingStatus.Draft or FindingStatus.Open or FindingStatus.Reviewed or FindingStatus.InProgress),
            "readyforpromotion" => rows.Where(r => ComputeReadiness(r.Finding) == FindingPromotionReadiness.Eligible),
            "recentlyadded" => rows.Where(r => r.Finding.CreatedAt >= cutoff),
            "critical" => rows.Where(r => r.Finding.Severity == FindingSeverity.Critical),
            _ => rows,
        };
    }

    private static List<WorkspaceRow> ApplySortInMemory(IReadOnlyList<WorkspaceRow> rows, string? sortBy, bool sortDesc)
    {
        IEnumerable<WorkspaceRow> ordered = (sortBy ?? "updated").ToLowerInvariant() switch
        {
            "severity" => sortDesc
                ? rows.OrderByDescending(x => x.Finding.Severity).ThenByDescending(x => x.Finding.LastUpdatedAt)
                : rows.OrderBy(x => x.Finding.Severity).ThenBy(x => x.Finding.LastUpdatedAt),
            "status" => sortDesc
                ? rows.OrderByDescending(x => x.Finding.Status)
                : rows.OrderBy(x => x.Finding.Status),
            "created" => sortDesc
                ? rows.OrderByDescending(x => x.Finding.CreatedAt)
                : rows.OrderBy(x => x.Finding.CreatedAt),
            "number" => sortDesc
                ? rows.OrderByDescending(x => x.Finding.FindingNumber)
                : rows.OrderBy(x => x.Finding.FindingNumber),
            "type" => sortDesc
                ? rows.OrderByDescending(x => x.Finding.Classification)
                : rows.OrderBy(x => x.Finding.Classification),
            _ => sortDesc
                ? rows.OrderByDescending(x => x.Finding.LastUpdatedAt)
                : rows.OrderBy(x => x.Finding.LastUpdatedAt),
        };
        return ordered.ToList();
    }

    private static FindingsWorkspaceItemDto MapItem(WorkspaceRow row) => MapCore(row, new FindingsWorkspaceItemDto());

    private static FindingsWorkspaceDetailDto MapDetail(WorkspaceRow row)
    {
        var dto = MapCore(row, new FindingsWorkspaceDetailDto());
        var f = row.Finding;
        dto.PromotedDeficiencyId = f.PromotedDeficiencyId;
        dto.AuditDate = f.AuditDate;
        dto.AuditorName = f.AuditorName;
        dto.AuditCycle = f.AuditCycle;
        dto.Observation = f.Observation;
        dto.Requirement = f.Requirement;
        dto.Impact = f.Impact;
        dto.Recommendation = f.Recommendation;
        dto.DateRaised = f.DateRaised;
        return dto;
    }

    private static T MapCore<T>(WorkspaceRow row, T dto) where T : FindingsWorkspaceItemDto
    {
        var f = row.Finding;
        var classification = f.Classification ?? MapLegacyType(f.Type);

        dto.Id = f.Id;
        dto.FindingNumber = string.IsNullOrWhiteSpace(f.FindingNumber)
            ? $"FND-{f.Id.ToString()[..8].ToUpperInvariant()}"
            : f.FindingNumber;
        dto.Title = string.IsNullOrWhiteSpace(f.Title) ? f.Description : f.Title;
        dto.Description = f.Description;
        dto.Classification = classification.ToString();
        dto.Source = f.RaisedBy.ToString();
        dto.Severity = f.Severity.ToString();
        dto.Status = f.Status.ToString();
        dto.Department = f.Department.ToString();
        dto.Owner = f.AssignedToUserId?.ToString();
        dto.ChapterCode = string.IsNullOrWhiteSpace(row.ChapterCode) ? f.ClauseReference : row.ChapterCode;
        dto.ChapterName = row.ChapterName;
        dto.StandardCode = row.StandardCode;
        dto.StandardName = row.StandardName;
        dto.ObjectiveElementCode = row.OeCode;
        dto.ObjectiveElementName = row.OeName;
        dto.ObjectiveElementId = f.ObjectiveElementId;
        dto.ClauseReference = f.ClauseReference;
        dto.CreatedDate = f.CreatedAt;
        dto.LastUpdated = f.LastUpdatedAt;
        dto.IsPromoted = f.PromotedDeficiencyId.HasValue;
        dto.PromotionReadiness = ComputeReadiness(f);
        return dto;
    }

    private static string ComputeReadiness(Finding finding) => FindingPromotionReadiness.Compute(finding);

    private static string ComputeReadiness(DashboardRow row)
    {
        var finding = new Finding
        {
            PromotedDeficiencyId = row.PromotedDeficiencyId,
            Classification = row.Classification,
            Type = row.Type,
            Status = row.Status,
            ObjectiveElementId = row.ObjectiveElementId,
            Title = row.Title,
        };
        return FindingPromotionReadiness.Compute(finding);
    }

    private static IReadOnlyList<FindingLifecycleStepDto> BuildLifecycleSteps(
        Finding finding,
        IReadOnlyList<FindingAuditTrail> history)
    {
        var steps = new[]
        {
            ("Draft", "Draft"),
            ("Open", "Open"),
            ("Reviewed", "Reviewed"),
            ("Accepted", "Accepted"),
            ("Promoted", "Promoted"),
            ("Closed", "Closed"),
        };

        var statusOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Draft"] = 0,
            ["Open"] = 1,
            ["InProgress"] = 1,
            ["Reviewed"] = 2,
            ["Accepted"] = 3,
            ["Closed"] = 5,
            ["Verified"] = 5,
        };

        var currentKey = finding.PromotedDeficiencyId.HasValue
            ? "Promoted"
            : finding.Status == FindingStatus.Verified ? "Closed" : finding.Status.ToString();

        if (!statusOrder.TryGetValue(currentKey, out var currentIndex))
        {
            currentIndex = 1;
        }

        if (finding.PromotedDeficiencyId.HasValue)
        {
            currentIndex = 4;
        }

        var statusChanges = history
            .Where(h => h.Action == "StatusChanged" && h.NewStateJson != null)
            .ToList();
        var promotedAt = history.FirstOrDefault(h => h.Action == "FindingPromoted")?.OccurredAt;

        return steps.Select((step, index) =>
        {
            var matchingHistory = statusChanges.LastOrDefault(h => h.NewStateJson!.Contains(step.Item1, StringComparison.OrdinalIgnoreCase));
            DateTime? occurredAt = step.Item1 switch
            {
                "Draft" when finding.Status == FindingStatus.Draft => finding.CreatedAt,
                "Promoted" => promotedAt,
                _ => matchingHistory?.OccurredAt,
            };
            Guid? actor = matchingHistory?.ActorUserId ?? (step.Item1 == "Draft" ? finding.CreatedByUserId : null);

            return new FindingLifecycleStepDto
            {
                Key = step.Item1,
                Label = step.Item2,
                IsComplete = index < currentIndex || (index == currentIndex && step.Item1 == "Closed" && finding.Status == FindingStatus.Closed),
                IsCurrent = index == currentIndex,
                OccurredAt = occurredAt,
                ActorUserId = actor,
            };
        }).ToList();
    }

    internal static FindingClassification MapLegacyType(NonConformanceType? type) => type switch
    {
        NonConformanceType.MajorNC or NonConformanceType.MinorNC => FindingClassification.NonConformity,
        NonConformanceType.Observation => FindingClassification.Observation,
        NonConformanceType.OFI => FindingClassification.OpportunityForImprovement,
        _ => FindingClassification.AuditFinding,
    };

    private sealed class WorkspaceRow
    {
        public Finding Finding { get; init; } = null!;
        public string OeCode { get; init; } = string.Empty;
        public string OeName { get; init; } = string.Empty;
        public string StandardCode { get; init; } = string.Empty;
        public string StandardName { get; init; } = string.Empty;
        public string ChapterCode { get; init; } = string.Empty;
        public string ChapterName { get; init; } = string.Empty;
    }

    private sealed class DashboardRow
    {
        public FindingStatus Status { get; init; }
        public FindingSeverity Severity { get; init; }
        public FindingRaisedBy RaisedBy { get; init; }
        public FindingDepartment Department { get; init; }
        public Guid? ObjectiveElementId { get; init; }
        public Guid? PromotedDeficiencyId { get; init; }
        public FindingClassification? Classification { get; init; }
        public NonConformanceType? Type { get; init; }
        public string Title { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
