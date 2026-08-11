using ComplianceService.Application.ComplianceScoring;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Journey;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ComplianceService.Application.Services;

/// <summary>
/// Program overview: prefer persisted ComplianceSnapshots; if missing, compute from active journey + OE aggregation
/// so dashboards align with executed objective-element work. Queues snapshot materialization for subsequent reads.
/// </summary>
public sealed class ProgramOverviewSnapshotReadService : IProgramOverviewSnapshotReadService
{
    private readonly ISnapshotService _snapshotService;
    private readonly ComplianceService.Application.Contracts.IComplianceScoringService _scoringService;
    private readonly IComplianceSnapshotRecalculationQueue _recalculationQueue;
    private readonly ComplianceDbContext _db;
    private readonly ILogger<ProgramOverviewSnapshotReadService> _logger;

    public ProgramOverviewSnapshotReadService(
        ISnapshotService snapshotService,
        ComplianceService.Application.Contracts.IComplianceScoringService scoringService,
        IComplianceSnapshotRecalculationQueue recalculationQueue,
        ComplianceDbContext db,
        ILogger<ProgramOverviewSnapshotReadService> logger)
    {
        _snapshotService = snapshotService;
        _scoringService = scoringService;
        _recalculationQueue = recalculationQueue;
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProgramOverviewSnapshotDto> GetProgramOverviewAsync(
        Guid hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken = default)
    {
        var persisted = await _snapshotService.GetSnapshotByHospitalIdAsync(hospitalId, frameworkId, cancellationToken);
        if (persisted != null)
        {
            return MapFromPersisted(persisted);
        }

        Guid? effectiveFrameworkId = frameworkId;
        ComplianceJourney? journey;

        if (effectiveFrameworkId.HasValue && effectiveFrameworkId.Value != Guid.Empty)
        {
            journey = await ComplianceJourneySnapshotResolver.GetActiveJourneyForSnapshotAsync(
                _db,
                hospitalId,
                effectiveFrameworkId.Value,
                cancellationToken);

            // Query param is often ComplianceJourney.Id (mislabeled as frameworkId in clients), not FrameworkId
            if (journey == null)
            {
                journey = await _db.ComplianceJourneys
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(j => j.Id == effectiveFrameworkId.Value && j.HospitalId == hospitalId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (journey != null)
                {
                    effectiveFrameworkId = journey.FrameworkId;
                    _logger.LogInformation(
                        "Resolved program overview query id as ComplianceJourney.Id; using FrameworkId={FrameworkId} JourneyId={JourneyId}",
                        journey.FrameworkId,
                        journey.Id);
                }
            }
        }
        else
        {
            journey = await _db.ComplianceJourneys
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(j => j.HospitalId == hospitalId)
                .OrderByDescending(j => j.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            effectiveFrameworkId = journey?.FrameworkId;
        }

        if (journey == null || !effectiveFrameworkId.HasValue || effectiveFrameworkId.Value == Guid.Empty)
        {
            _logger.LogInformation(
                "No program overview: no snapshot row and no active journey for HospitalId={HospitalId} FrameworkId={FrameworkId}",
                hospitalId,
                frameworkId);

            return new ProgramOverviewSnapshotDto
            {
                IsSnapshotAvailable = false,
                SnapshotMaterialized = false,
                ComplianceJourneyId = null,
                FrameworkId = frameworkId,
                CalculatedAt = null
            };
        }

        var fwId = effectiveFrameworkId.Value;
        var score = await _scoringService.CalculateFrameworkScoreAsync(hospitalId, fwId);

        var chapterJson = JsonSerializer.Serialize(score.ChapterScores, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogInformation(
            "Program overview fallback from OE scoring for HospitalId={HospitalId} FrameworkId={FrameworkId} JourneyId={JourneyId} score={Score:F2}",
            hospitalId,
            fwId,
            journey.Id,
            score.OverallScore);

        _recalculationQueue.EnqueueHospitalFramework(hospitalId, fwId);

        return new ProgramOverviewSnapshotDto
        {
            IsSnapshotAvailable = true,
            SnapshotMaterialized = false,
            ComplianceJourneyId = journey.Id,
            FrameworkId = fwId,
            OverallScore = score.OverallScore,
            AuditConfidenceScore = score.AuditConfidenceScore,
            CriticalCount = score.CriticalCount,
            MajorCount = score.MajorCount,
            MinorCount = score.MinorCount,
            ObservationCount = score.ObservationsCount,
            OFICount = score.OFICount,
            HighRiskChaptersJson = "[]",
            ChapterBreakdownJson = chapterJson,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private static ProgramOverviewSnapshotDto MapFromPersisted(ComplianceSnapshot snapshot)
    {
        return new ProgramOverviewSnapshotDto
        {
            IsSnapshotAvailable = true,
            SnapshotMaterialized = true,
            ComplianceJourneyId = snapshot.ComplianceJourneyId,
            FrameworkId = snapshot.FrameworkId,
            OverallScore = snapshot.OverallScore,
            AuditConfidenceScore = snapshot.AuditConfidenceScore,
            CriticalCount = snapshot.CriticalCount,
            MajorCount = snapshot.MajorCount,
            MinorCount = snapshot.MinorCount,
            ObservationCount = snapshot.ObservationCount,
            OFICount = snapshot.OFICount,
            HighRiskChaptersJson = snapshot.HighRiskChaptersJson ?? string.Empty,
            ChapterBreakdownJson = snapshot.ChapterBreakdownJson ?? string.Empty,
            CalculatedAt = snapshot.CalculatedAt
        };
    }
}
