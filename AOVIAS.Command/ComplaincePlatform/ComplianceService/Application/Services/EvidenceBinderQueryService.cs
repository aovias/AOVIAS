using System.IO;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Presentation;
using ComplianceService.Application.Journey;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Clients;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class EvidenceBinderQueryService : IEvidenceBinderQueryService
{
    private readonly ComplianceDbContext _db;
    private readonly IJourneyWorkspaceService _journeyWorkspace;
    private readonly IAiOrchestratorAssistClient? _aiAssist;

    public EvidenceBinderQueryService(
        ComplianceDbContext db,
        IJourneyWorkspaceService journeyWorkspace,
        IAiOrchestratorAssistClient? aiAssist = null)
    {
        _db = db;
        _journeyWorkspace = journeyWorkspace;
        _aiAssist = aiAssist;
    }

    public Task<Guid> ResolveFrameworkIdAsync(Guid hospitalId, Guid frameworkOrJourneyId, CancellationToken cancellationToken = default) =>
        ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(_db, hospitalId, frameworkOrJourneyId, cancellationToken);

    public async Task<bool> IsObjectiveInBinderScopeAsync(Guid resolvedFrameworkId, Guid objectiveElementId, CancellationToken cancellationToken = default)
    {
        return await (
            from oe in _db.ObjectiveElements.AsNoTracking()
            join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
            join fw in _db.ComplianceFrameworks.AsNoTracking() on ch.FrameworkId equals fw.Id
            where oe.Id == objectiveElementId
                  && ch.FrameworkId == resolvedFrameworkId
                  && oe.IsApplicable
                  && fw.IsActive
            select 1).AnyAsync(cancellationToken);
    }

    public async Task<EvidenceBinderSummaryDto> GetSummaryAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkIdAsync(hospitalId, frameworkId, cancellationToken);
        var scope = await LoadScopeRowsAsync(fwId, cancellationToken);
        var linkStats = await LoadLinkStatsAsync(hospitalId, fwId, cancellationToken);

        var total = scope.Count;
        if (total == 0)
        {
            return new EvidenceBinderSummaryDto
            {
                TotalObjectives = 0,
                ObjectivesWithEvidence = 0,
                ObjectivesMissingEvidence = 0,
                ObjectivesApproved = 0,
                ApprovedEvidenceCount = 0,
                ReadinessPercent = 0
            };
        }

        var withEvidence = 0;
        var approvedObj = 0;
        foreach (var row in scope)
        {
            if (!linkStats.TryGetValue(row.ObjectiveElementId, out var ls) || ls.EvidenceCount <= 0)
            {
                continue;
            }

            withEvidence++;
            if (ls.HasApproved)
            {
                approvedObj++;
            }
        }

        var missing = total - withEvidence;
        var readiness = total > 0 ? (int)Math.Round(approvedObj * 100.0 / total) : 0;

        var approvedEvidenceCount = await _db.ObjectiveElementEvidences.AsNoTracking()
            .CountAsync(
                l => l.VerificationStatus == EvidenceVerificationStatus.Verified
                     && _db.Evidences.Any(e => e.Id == l.EvidenceId && e.HospitalId == hospitalId && e.FrameworkId == fwId),
                cancellationToken);

        return new EvidenceBinderSummaryDto
        {
            TotalObjectives = total,
            ObjectivesWithEvidence = withEvidence,
            ObjectivesMissingEvidence = missing,
            ObjectivesApproved = approvedObj,
            ApprovedEvidenceCount = approvedEvidenceCount,
            ReadinessPercent = readiness
        };
    }

    public async Task<IReadOnlyList<EvidenceBinderChapterDto>> GetChaptersAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkIdAsync(hospitalId, frameworkId, cancellationToken);
        var scope = await LoadScopeRowsAsync(fwId, cancellationToken);
        var linkStats = await LoadLinkStatsAsync(hospitalId, fwId, cancellationToken);

        var chapters = await _db.ComplianceChapters.AsNoTracking()
            .Where(ch => ch.FrameworkId == fwId)
            .OrderBy(ch => ch.Sequence)
            .ToListAsync(cancellationToken);

        var byChapter = scope.GroupBy(s => s.ChapterId).ToDictionary(g => g.Key, g => g.ToList());
        var list = new List<EvidenceBinderChapterDto>(chapters.Count);

        foreach (var ch in chapters)
        {
            if (!byChapter.TryGetValue(ch.Id, out var rows))
            {
                list.Add(new EvidenceBinderChapterDto
                {
                    ChapterId = ch.Id,
                    Code = ch.Code ?? string.Empty,
                    Name = ch.Name ?? string.Empty,
                    ObjectiveCount = 0,
                    ObjectivesWithEvidence = 0,
                    CoveragePercent = 0,
                    RiskLevel = "low"
                });
                continue;
            }

            var objectiveCount = rows.Count;
            var withEvidence = 0;
            var missingCount = 0;
            var partialCount = 0;

            foreach (var row in rows)
            {
                var status = ClassifyStatus(row.ObjectiveElementId, linkStats);
                if (status == EvidenceBinderStatusRules.Missing)
                {
                    missingCount++;
                }
                else if (status == EvidenceBinderStatusRules.Partial)
                {
                    partialCount++;
                }

                if (linkStats.TryGetValue(row.ObjectiveElementId, out var ls) && ls.EvidenceCount > 0)
                {
                    withEvidence++;
                }
            }

            var coverage = objectiveCount > 0 ? (int)Math.Round(withEvidence * 100.0 / objectiveCount) : 0;
            var risk = EvidenceBinderStatusRules.ComputeChapterRiskLevel(missingCount, partialCount);

            list.Add(new EvidenceBinderChapterDto
            {
                ChapterId = ch.Id,
                Code = ch.Code ?? string.Empty,
                Name = ch.Name ?? string.Empty,
                ObjectiveCount = objectiveCount,
                ObjectivesWithEvidence = withEvidence,
                CoveragePercent = coverage,
                RiskLevel = risk
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<EvidenceBinderObjectiveElementDto>> GetObjectivesForChapterAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid chapterId,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkIdAsync(hospitalId, frameworkId, cancellationToken);
        var scope = await LoadScopeRowsAsync(fwId, cancellationToken);
        var linkStats = await LoadLinkStatsAsync(hospitalId, fwId, cancellationToken);
        var taskCounts = await LoadTaskCountsAsync(hospitalId, fwId, cancellationToken);

        return scope
            .Where(s => s.ChapterId == chapterId)
            .OrderBy(s => s.Code)
            .Select(row => MapObjective(row, linkStats, taskCounts))
            .ToList();
    }

    public async Task<EvidenceBinderOverviewDto?> GetObjectiveOverviewAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkIdAsync(hospitalId, frameworkId, cancellationToken);
        if (!await IsObjectiveInBinderScopeAsync(fwId, objectiveElementId, cancellationToken))
        {
            return null;
        }

        var scope = await LoadScopeRowsAsync(fwId, cancellationToken);
        var row = scope.FirstOrDefault(s => s.ObjectiveElementId == objectiveElementId);
        if (row is null)
        {
            return null;
        }

        var linkStats = await LoadLinkStatsAsync(hospitalId, fwId, cancellationToken);
        var taskCounts = await LoadTaskCountsAsync(hospitalId, fwId, cancellationToken);
        var (_, _, status) = Classify(row.ObjectiveElementId, linkStats);
        var evidenceCount = linkStats.TryGetValue(row.ObjectiveElementId, out var ls) ? ls.EvidenceCount : 0;
        var taskCount = taskCounts.GetValueOrDefault(row.ObjectiveElementId, 0);

        return new EvidenceBinderOverviewDto
        {
            ObjectiveElementId = row.ObjectiveElementId,
            ChapterId = row.ChapterId,
            ChapterCode = row.ChapterCode,
            ChapterName = row.ChapterName,
            Code = row.Code,
            Title = row.Title,
            Description = row.Description,
            EvidenceCount = evidenceCount,
            TaskCount = taskCount,
            Status = status
        };
    }

    public async Task<PagedResultDto<EvidenceBinderEvidenceItemDto>> GetEvidencesForObjectiveAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkIdAsync(hospitalId, frameworkId, cancellationToken);
        if (!await IsObjectiveInBinderScopeAsync(fwId, objectiveElementId, cancellationToken))
        {
            return new PagedResultDto<EvidenceBinderEvidenceItemDto>
            {
                Items = Array.Empty<EvidenceBinderEvidenceItemDto>(),
                TotalCount = 0,
                Page = Math.Max(1, page),
                PageSize = Math.Clamp(pageSize, 1, 200)
            };
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var baseQuery = from link in _db.ObjectiveElementEvidences.AsNoTracking()
            join e in _db.Evidences.AsNoTracking() on link.EvidenceId equals e.Id
            join oe in _db.ObjectiveElements.AsNoTracking() on link.ObjectiveElementId equals oe.Id
            join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
            join fw in _db.ComplianceFrameworks.AsNoTracking() on ch.FrameworkId equals fw.Id
            where link.ObjectiveElementId == objectiveElementId
                  && e.HospitalId == hospitalId
                  && e.FrameworkId == fwId
                  && ch.FrameworkId == fwId
                  && oe.IsApplicable
                  && fw.IsActive
            select new { link, e };

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var rows = await baseQuery
            .OrderByDescending(x => x.e.UploadedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.ConvertAll(x => new EvidenceBinderEvidenceItemDto
        {
            Id = x.link.Id,
            EvidenceId = x.e.Id,
            FileName = FileNameFromUrl(x.e.FileUrl, x.e.Title),
            FileUrl = x.e.FileUrl ?? string.Empty,
            FileType = NormalizeFileType(x.e.FileUrl),
            FileSizeBytes = 0,
            UploadedBy = x.e.UploadedBy ?? string.Empty,
            UploadedAt = x.e.UploadedOn,
            Status = MapVerificationStatus(x.link.VerificationStatus),
            IsApproved = ToIsApproved(x.link.VerificationStatus),
            ReviewComment = x.link.ReviewComment ?? x.link.Remarks,
            RowVersion = EncodeRowVersion(x.link.RowVersion),
            VerifiedBy = x.link.VerifiedBy,
            VerifiedAt = x.link.VerifiedAt,
            Version = x.e.CurrentVersion
        });

        return new PagedResultDto<EvidenceBinderEvidenceItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<IReadOnlyList<EvidenceBinderChecklistItemDto>> GetChecklistAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EvidenceBinderChecklistItemDto>>(Array.Empty<EvidenceBinderChecklistItemDto>());

    public async Task<IReadOnlyList<EvidenceBinderAiMappingItemDto>> GetAiMappingAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkIdAsync(hospitalId, frameworkId, cancellationToken);
        if (!await IsObjectiveInBinderScopeAsync(fwId, objectiveElementId, cancellationToken))
        {
            return Array.Empty<EvidenceBinderAiMappingItemDto>();
        }

        var oe = await (
            from o in _db.ObjectiveElements.AsNoTracking()
            where o.Id == objectiveElementId
            select new { o.Id, o.Code, o.Title, o.Description }).FirstOrDefaultAsync(cancellationToken);

        if (oe is null)
        {
            return Array.Empty<EvidenceBinderAiMappingItemDto>();
        }

        // Prefer AIOrchestrator SuggestMapping when the client is available.
        if (_aiAssist != null)
        {
            var prompt =
                $"Suggest evidence-to-OE mappings for {oe.Code} — {oe.Title}. " +
                $"Requirement: {oe.Description ?? "n/a"}. Return conservative suggestions only.";
            var ai = await _aiAssist.SuggestMappingAsync(hospitalId, fwId, objectiveElementId, prompt, cancellationToken);
            if (ai != null && (!string.IsNullOrWhiteSpace(ai.ComplianceHint) || ai.SuggestedMapping != null))
            {
                var confidence = ai.CompositeConfidenceScore > 0 ? ai.CompositeConfidenceScore : ai.ConfidenceScore;
                var reasoning = ai.Reasoning?.Select(r => r.Message).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
                var source = ai.SourceReferences?.FirstOrDefault();
                return new[]
                {
                    new EvidenceBinderAiMappingItemDto
                    {
                        Id = Guid.NewGuid(),
                        Title = $"AI mapping suggestion for {oe.Code}",
                        Detail = ai.ComplianceHint ?? ai.SuggestedMapping?.MappingReason ?? "Suggested mapping from AI assist.",
                        ConfidencePercent = (int)Math.Clamp(Math.Round(confidence * 100m), 0, 100),
                        Reasoning = reasoning ?? "Grounded assistive suggestion. Human approval required before linking.",
                        SourceReference = source == null
                            ? "AIOrchestrator/assist/suggest"
                            : $"{source.SourceType}/{source.SourceId}",
                        HumanApprovalRequired = true
                    }
                };
            }
        }

        // Local heuristic fallback — non-empty suggestions with HumanApprovalRequired semantics.
        var linkedCount = await _db.ObjectiveElementEvidences.AsNoTracking()
            .CountAsync(l => l.ObjectiveElementId == objectiveElementId, cancellationToken);

        var suggestions = new List<EvidenceBinderAiMappingItemDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = $"Map documented procedure to {oe.Code}",
                Detail =
                    $"Link the controlled SOP / policy that demonstrates '{oe.Title}'. " +
                    (string.IsNullOrWhiteSpace(oe.Description) ? string.Empty : Truncate(oe.Description!, 220)),
                ConfidencePercent = linkedCount == 0 ? 72 : 58,
                Reasoning = linkedCount == 0
                    ? "No linked evidence yet for this OE; prioritize a primary controlled document."
                    : $"OE already has {linkedCount} linked evidence item(s); suggest supplemental or version-aligned artifacts.",
                SourceReference = $"ObjectiveElement/{oe.Code}",
                HumanApprovalRequired = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = $"Attach implementation evidence for {oe.Code}",
                Detail =
                    "Propose records that show practice (training attendance, checklists, logs, audit samples) in addition to the policy document.",
                ConfidencePercent = 64,
                Reasoning = "Accreditation reviewers typically expect both documented intent and implementation proof.",
                SourceReference = $"ObjectiveElement/{oe.Id:N}",
                HumanApprovalRequired = true
            }
        };

        return suggestions;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 3)] + "...";

    public async Task<IReadOnlyList<OeHistoryEventDto>> GetHistoryAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var fwId = await ResolveFrameworkIdAsync(hospitalId, frameworkId, cancellationToken);
        if (!await IsObjectiveInBinderScopeAsync(fwId, objectiveElementId, cancellationToken))
        {
            return Array.Empty<OeHistoryEventDto>();
        }

        var journey = await ComplianceJourneySnapshotResolver.GetActiveJourneyForSnapshotAsync(_db, hospitalId, fwId, cancellationToken);
        if (journey is null)
        {
            return Array.Empty<OeHistoryEventDto>();
        }

        try
        {
            return await _journeyWorkspace.GetObjectiveElementHistoryAsync(journey.Id, objectiveElementId, tenantId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<OeHistoryEventDto>();
        }
    }

    private sealed record ScopeRow(
        Guid ObjectiveElementId,
        Guid ChapterId,
        string ChapterCode,
        string ChapterName,
        string Code,
        string Title,
        string Description);

    private async Task<List<ScopeRow>> LoadScopeRowsAsync(Guid fwId, CancellationToken cancellationToken)
    {
        return await (
            from oe in _db.ObjectiveElements.AsNoTracking()
            join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
            join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
            join fw in _db.ComplianceFrameworks.AsNoTracking() on ch.FrameworkId equals fw.Id
            where ch.FrameworkId == fwId && oe.IsApplicable && fw.IsActive
            select new ScopeRow(
                oe.Id,
                ch.Id,
                ch.Code ?? string.Empty,
                ch.Name ?? string.Empty,
                oe.Code ?? string.Empty,
                oe.Title ?? string.Empty,
                oe.Description ?? string.Empty)).ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, (int EvidenceCount, bool HasApproved)>> LoadLinkStatsAsync(
        Guid hospitalId,
        Guid fwId,
        CancellationToken cancellationToken)
    {
        return await (
                from l in _db.ObjectiveElementEvidences.AsNoTracking()
                join e in _db.Evidences.AsNoTracking() on l.EvidenceId equals e.Id
                where e.HospitalId == hospitalId && e.FrameworkId == fwId
                group l by l.ObjectiveElementId
                into g
                select new
                {
                    OeId = g.Key,
                    EvidenceCount = g.Count(),
                    HasApproved = g.Any(x => x.VerificationStatus == EvidenceVerificationStatus.Verified)
                })
            .ToDictionaryAsync(x => x.OeId, x => (x.EvidenceCount, x.HasApproved), cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> LoadTaskCountsAsync(Guid hospitalId, Guid fwId, CancellationToken cancellationToken)
    {
        return await (
                from t in _db.ComplianceTasks.AsNoTracking()
                join oe in _db.ObjectiveElements.AsNoTracking() on t.ObjectiveElementId equals oe.Id
                join st in _db.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals st.Id
                join ch in _db.ComplianceChapters.AsNoTracking() on st.ChapterId equals ch.Id
                join fw in _db.ComplianceFrameworks.AsNoTracking() on ch.FrameworkId equals fw.Id
                where t.HospitalId == hospitalId
                      && ch.FrameworkId == fwId
                      && oe.IsApplicable
                      && fw.IsActive
                group t by t.ObjectiveElementId
                into g
                select new { OeId = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.OeId, x => x.C, cancellationToken);
    }

    private static EvidenceBinderObjectiveElementDto MapObjective(
        ScopeRow row,
        IReadOnlyDictionary<Guid, (int EvidenceCount, bool HasApproved)> linkStats,
        IReadOnlyDictionary<Guid, int> taskCounts)
    {
        var (evidenceCount, _, status) = Classify(row.ObjectiveElementId, linkStats);
        var taskCount = taskCounts.GetValueOrDefault(row.ObjectiveElementId, 0);
        return new EvidenceBinderObjectiveElementDto
        {
            ObjectiveElementId = row.ObjectiveElementId,
            ChapterId = row.ChapterId,
            Code = row.Code,
            Title = row.Title,
            Description = row.Description,
            EvidenceCount = evidenceCount,
            TaskCount = taskCount,
            Status = status
        };
    }

    private static (int EvidenceCount, bool HasApproved, string Status) Classify(
        Guid objectiveElementId,
        IReadOnlyDictionary<Guid, (int EvidenceCount, bool HasApproved)> linkStats)
    {
        if (!linkStats.TryGetValue(objectiveElementId, out var ls) || ls.EvidenceCount <= 0)
        {
            return (0, false, EvidenceBinderStatusRules.Missing);
        }

        var status = EvidenceBinderStatusRules.ComputeObjectiveStatus(true, ls.HasApproved);
        return (ls.EvidenceCount, ls.HasApproved, status);
    }

    private static string ClassifyStatus(
        Guid objectiveElementId,
        IReadOnlyDictionary<Guid, (int EvidenceCount, bool HasApproved)> linkStats)
    {
        var (_, _, status) = Classify(objectiveElementId, linkStats);
        return status;
    }

    private static string MapVerificationStatus(EvidenceVerificationStatus status) =>
        EvidenceStatusPresentationMapper.ToApiKey(status);

    private static bool? ToIsApproved(EvidenceVerificationStatus status) =>
        status switch
        {
            EvidenceVerificationStatus.Verified => true,
            EvidenceVerificationStatus.Rejected => false,
            _ => null
        };

    private static string? EncodeRowVersion(byte[]? rowVersion) =>
        rowVersion is not { Length: > 0 } ? null : Convert.ToBase64String(rowVersion);

    private static string NormalizeFileType(string? fileUrl)
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

    private static string FileNameFromUrl(string? fileUrl, string? title)
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
