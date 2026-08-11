using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Journey;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class AuditSimulationAppService : IAuditSimulationAppService
{
    private readonly ComplianceDbContext _db;
    private readonly AuditSimulationService _simulator;

    public AuditSimulationAppService(ComplianceDbContext db, AuditSimulationService simulator)
    {
        _db = db;
        _simulator = simulator;
    }

    public async Task<AuditSimulationResultDto> SimulateAsync(
        Guid organizationId, Guid hospitalId, Guid? frameworkId, CancellationToken ct = default)
    {
        var journey = await ComplianceJourneySnapshotResolver.GetActiveJourneyForSnapshotAsync(
            _db, hospitalId, frameworkId ?? Guid.Empty, ct);

        if (journey == null)
        {
            journey = await _db.ComplianceJourneys.AsNoTracking()
                .Where(j => j.HospitalId == hospitalId || j.OrganizationId == organizationId)
                .OrderByDescending(j => j.UpdatedAt)
                .FirstOrDefaultAsync(ct);
        }

        var fwId = frameworkId is { } f && f != Guid.Empty
            ? f
            : journey?.FrameworkId ?? Guid.Empty;

        var chapters = fwId == Guid.Empty
            ? new List<ComplianceChapter>()
            : await _db.ComplianceChapters.AsNoTracking()
                .Where(c => c.FrameworkId == fwId)
                .ToListAsync(ct);

        var chapterIds = chapters.Select(c => c.Id).ToList();
        var standards = chapterIds.Count == 0
            ? new List<ComplianceStandard>()
            : await _db.ComplianceStandards.AsNoTracking()
                .Where(s => chapterIds.Contains(s.ChapterId))
                .ToListAsync(ct);

        // Attach standards so simulation chapter completion can resolve ChapterId via OE.ComplianceStandard
        var standardsById = standards.ToDictionary(s => s.Id);
        foreach (var s in standards)
        {
            s.Chapter = chapters.FirstOrDefault(c => c.Id == s.ChapterId);
        }

        var standardIds = standards.Select(s => s.Id).ToList();
        var oes = standardIds.Count == 0
            ? new List<ObjectiveElement>()
            : await _db.ObjectiveElements.AsNoTracking()
                .Where(oe => standardIds.Contains(oe.ComplianceStandardId))
                .ToListAsync(ct);

        foreach (var oe in oes)
        {
            if (standardsById.TryGetValue(oe.ComplianceStandardId, out var st))
                oe.ComplianceStandard = st;
        }

        var oeIds = oes.Select(o => o.Id).ToList();
        var tasks = oeIds.Count == 0
            ? new List<ComplianceTask>()
            : await _db.ComplianceTasks.AsNoTracking()
                .Where(t => oeIds.Contains(t.ObjectiveElementId) && t.HospitalId == hospitalId)
                .ToListAsync(ct);

        var risks = await _db.Risks.AsNoTracking()
            .Where(r => r.HospitalId == hospitalId && r.Status != RiskStatus.Closed)
            .ToListAsync(ct);

        var result = _simulator.SimulateAudit(new AuditSimulationContext
        {
            Chapters = chapters,
            ObjectiveElements = oes,
            Tasks = tasks,
            Risks = risks,
        });

        var totalClauses = oes.Count;
        var completedOe = 0;
        foreach (var oe in oes)
        {
            var oeTasks = tasks.Where(t => t.ObjectiveElementId == oe.Id).ToList();
            if (oeTasks.Count > 0 && oeTasks.All(t =>
                    ComplianceTaskStatusExtensions.IsCompletedForScoring(t.Status.ToScoringStatusString())))
            {
                completedOe++;
            }
        }

        var openTasks = tasks.Count(t =>
            !ComplianceTaskStatusExtensions.IsCompletedForScoring(t.Status.ToScoringStatusString()));

        var score = totalClauses == 0
            ? 0
            : (int)Math.Round((decimal)completedOe / totalClauses * 100m);

        return new AuditSimulationResultDto
        {
            MajorNCCount = result.MajorNCCount,
            MinorNCCount = result.MinorNCCount,
            ObservationCount = result.ObservationCount,
            OFICount = result.OFICount,
            TotalNonConformances = result.TotalNonConformances,
            Score = score,
            TotalClauses = totalClauses,
            CompliantClauses = completedOe,
            OpenTasks = openTasks,
            HighRiskChapters = result.HighRiskChapters.Select(c => new AuditSimulationHighRiskChapterDto
            {
                ChapterId = c.ChapterId,
                ChapterCode = c.ChapterCode,
                ChapterName = c.ChapterName,
                CompletionPercentage = c.CompletionPercentage,
                TotalObjectiveElements = c.TotalObjectiveElements,
                CompletedObjectiveElements = c.CompletedObjectiveElements,
            }).ToList(),
        };
    }
}
