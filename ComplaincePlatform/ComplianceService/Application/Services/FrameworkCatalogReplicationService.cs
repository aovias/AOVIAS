using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Clones chapters, standards, and objective elements between compliance frameworks.
/// Used to populate CP00000001 (Full 6E) from CP00000002 (Entry Level) after Entry Level seeding.
/// </summary>
public sealed class FrameworkCatalogReplicationService : IFrameworkCatalogReplicationService
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<FrameworkCatalogReplicationService> _logger;

    public FrameworkCatalogReplicationService(
        ComplianceDbContext db,
        ILogger<FrameworkCatalogReplicationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<FrameworkCatalogReplicationResult> ReplicateIfTargetEmptyAsync(
        string sourcePlatformId,
        string targetPlatformId,
        CancellationToken cancellationToken = default)
    {
        var sourceCode = sourcePlatformId.Trim();
        var targetCode = targetPlatformId.Trim();

        var source = await _db.ComplianceFrameworks.AsNoTracking()
            .FirstOrDefaultAsync(f => f.PlatformId == sourceCode, cancellationToken);
        if (source is null)
        {
            return new FrameworkCatalogReplicationResult(
                Skipped: true,
                Message: $"Source framework {sourceCode} not found.");
        }

        var target = await _db.ComplianceFrameworks.AsNoTracking()
            .FirstOrDefaultAsync(f => f.PlatformId == targetCode, cancellationToken);
        if (target is null)
        {
            return new FrameworkCatalogReplicationResult(
                Skipped: true,
                Message: $"Target framework {targetCode} not found.");
        }

        var sourceChapters = await _db.ComplianceChapters.AsNoTracking()
            .Where(c => c.FrameworkId == source.Id)
            .OrderBy(c => c.Sequence)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);

        if (sourceChapters.Count == 0)
        {
            return new FrameworkCatalogReplicationResult(
                Skipped: true,
                Message: $"Source framework {sourceCode} has no chapters to replicate.");
        }

        var sourceChapterIds = sourceChapters.Select(c => c.Id).ToList();
        var sourceStandards = await _db.ComplianceStandards.AsNoTracking()
            .Where(s => sourceChapterIds.Contains(s.ChapterId))
            .OrderBy(s => s.Sequence)
            .ThenBy(s => s.Code)
            .ToListAsync(cancellationToken);

        var sourceStandardIds = sourceStandards.Select(s => s.Id).ToList();
        var sourceOes = await _db.ObjectiveElements.AsNoTracking()
            .Where(oe => sourceStandardIds.Contains(oe.ComplianceStandardId))
            .OrderBy(oe => oe.Code)
            .ToListAsync(cancellationToken);

        var targetChapterCount = await _db.ComplianceChapters
            .CountAsync(c => c.FrameworkId == target.Id, cancellationToken);
        var targetStandardCount = await _db.ComplianceStandards
            .CountAsync(s => s.Chapter.FrameworkId == target.Id, cancellationToken);
        var targetOeCount = await _db.ObjectiveElements
            .CountAsync(
                oe => oe.ComplianceStandard.Chapter.FrameworkId == target.Id,
                cancellationToken);

        if (targetChapterCount > 0 || targetStandardCount > 0 || targetOeCount > 0)
        {
            _logger.LogInformation(
                "Skip catalog replication {Source} → {Target}: target already has {Chapters} chapters, {Standards} standards, {Oes} OEs.",
                sourceCode,
                targetCode,
                targetChapterCount,
                targetStandardCount,
                targetOeCount);

            return new FrameworkCatalogReplicationResult(
                Skipped: true,
                Message: $"Target framework {targetCode} already has catalog data.",
                SourceChapters: sourceChapters.Count,
                SourceStandards: sourceStandards.Count,
                SourceObjectiveElements: sourceOes.Count);
        }

        var nextPlatformNumber = await GetNextCpSequenceStartAsync(cancellationToken);
        var chapterMap = new Dictionary<Guid, Guid>();
        var chapterPlatformSeq = 0;
        var standardPlatformSeq = 0;
        var oePlatformSeq = 0;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var src in sourceChapters)
        {
            var newId = Guid.NewGuid();
            chapterMap[src.Id] = newId;
            chapterPlatformSeq++;

            _db.ComplianceChapters.Add(new ComplianceChapter
            {
                Id = newId,
                PlatformId = FormatCpPlatformId(nextPlatformNumber + chapterPlatformSeq),
                FrameworkId = target.Id,
                Code = src.Code,
                Name = src.Name,
                Sequence = src.Sequence,
                Weight = src.Weight,
            });
        }

        var standardMap = new Dictionary<Guid, Guid>();
        foreach (var src in sourceStandards)
        {
            if (!chapterMap.TryGetValue(src.ChapterId, out var newChapterId))
            {
                continue;
            }

            var newId = Guid.NewGuid();
            standardMap[src.Id] = newId;
            standardPlatformSeq++;

            _db.ComplianceStandards.Add(new ComplianceStandard
            {
                Id = newId,
                PlatformId = FormatCpPlatformId(nextPlatformNumber + sourceChapters.Count + standardPlatformSeq),
                ChapterId = newChapterId,
                Code = src.Code,
                Name = src.Name,
                Sequence = src.Sequence,
                Weight = src.Weight,
            });
        }

        foreach (var src in sourceOes)
        {
            if (!standardMap.TryGetValue(src.ComplianceStandardId, out var newStandardId))
            {
                continue;
            }

            oePlatformSeq++;

            _db.ObjectiveElements.Add(new ObjectiveElement
            {
                Id = Guid.NewGuid(),
                PlatformId = FormatCpPlatformId(
                    nextPlatformNumber + sourceChapters.Count + sourceStandards.Count + oePlatformSeq),
                ComplianceStandardId = newStandardId,
                Code = src.Code,
                Title = src.Title,
                Description = src.Description,
                Classification = src.Classification,
                IsMandatory = src.IsMandatory,
                IsScorable = src.IsScorable,
                IsApplicable = src.IsApplicable,
                IsDocumentationRequired = src.IsDocumentationRequired,
                Tags = src.Tags,
                Department = src.Department,
                ResponsibleRole = src.ResponsibleRole,
                Frequency = src.Frequency,
                EvidenceCategory = src.EvidenceCategory,
                Weight = src.Weight,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Replicated catalog {Source} → {Target}: {Chapters} chapters, {Standards} standards, {Oes} OEs.",
            sourceCode,
            targetCode,
            chapterPlatformSeq,
            standardPlatformSeq,
            oePlatformSeq);

        return new FrameworkCatalogReplicationResult(
            Skipped: false,
            Message: $"Replicated catalog from {sourceCode} to {targetCode}.",
            SourceChapters: sourceChapters.Count,
            SourceStandards: sourceStandards.Count,
            SourceObjectiveElements: sourceOes.Count,
            InsertedChapters: chapterPlatformSeq,
            InsertedStandards: standardPlatformSeq,
            InsertedObjectiveElements: oePlatformSeq);
    }

    private async Task<int> GetNextCpSequenceStartAsync(CancellationToken cancellationToken)
    {
        var chapterIds = await _db.ComplianceChapters.AsNoTracking()
            .Where(c => c.PlatformId.StartsWith("CP"))
            .Select(c => c.PlatformId)
            .ToListAsync(cancellationToken);
        var standardIds = await _db.ComplianceStandards.AsNoTracking()
            .Where(s => s.PlatformId.StartsWith("CP"))
            .Select(s => s.PlatformId)
            .ToListAsync(cancellationToken);
        var oeIds = await _db.ObjectiveElements.AsNoTracking()
            .Where(oe => oe.PlatformId.StartsWith("CP"))
            .Select(oe => oe.PlatformId)
            .ToListAsync(cancellationToken);

        var max = 0;
        foreach (var id in chapterIds.Concat(standardIds).Concat(oeIds))
        {
            if (id.Length == 10 && int.TryParse(id.AsSpan(2), out var n) && n > max)
            {
                max = n;
            }
        }

        return max;
    }

    private static string FormatCpPlatformId(int number) => $"CP{number:D8}";
}
