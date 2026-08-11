using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.ComplianceScoring;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Configs;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ComplianceService.Application.Services;

/// <summary>
/// Reporting facade that composes existing scoring, audit, and journey summary services
/// into report-friendly DTOs.
/// </summary>
public class ReportingService : IReportingService
{
    private readonly Application.Contracts.IComplianceScoringService _scoringService;
    private readonly IDeterministicNabhScoringService _deterministicScoring;
    private readonly IScoringParityService _parityService;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IShadowComparisonService _shadowComparisonService;
    private readonly ComplianceAuditService _auditService;
    private readonly ComplianceDbContext _dbContext;
    private readonly MigrationSafetyOptions _migrationSafetyOptions;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(
        Application.Contracts.IComplianceScoringService scoringService,
        IDeterministicNabhScoringService deterministicScoring,
        IScoringParityService parityService,
        IComplianceEventEmitter eventEmitter,
        IShadowComparisonService shadowComparisonService,
        ComplianceAuditService auditService,
        ComplianceDbContext dbContext,
        IOptions<MigrationSafetyOptions> migrationSafetyOptions,
        ILogger<ReportingService> logger)
    {
        _scoringService = scoringService;
        _deterministicScoring = deterministicScoring;
        _parityService = parityService;
        _eventEmitter = eventEmitter;
        _shadowComparisonService = shadowComparisonService;
        _auditService = auditService;
        _dbContext = dbContext;
        _migrationSafetyOptions = migrationSafetyOptions.Value;
        _logger = logger;
    }

    public async Task<ComplianceScoreReportDto> GetComplianceScoreReportAsync(
        Guid hospitalId,
        Guid? frameworkId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating compliance score report for HospitalId={HospitalId}, FrameworkId={FrameworkId}",
            hospitalId, frameworkId);

        Application.ComplianceScoring.ComplianceScoreResult score;
        if (frameworkId.HasValue)
        {
            score = await _scoringService.CalculateFrameworkScoreAsync(hospitalId, frameworkId.Value);

            if (_migrationSafetyOptions.EnableShadowMode && _migrationSafetyOptions.EnableScoringParity)
            {
                var parity = await _parityService.CompareAsync(hospitalId, frameworkId.Value, null, cancellationToken);
                _logger.LogInformation(
                    "Scoring parity check ({Category}) hospital={HospitalId} framework={FrameworkId} delta={Delta} mismatches={MismatchCount}",
                    parity.Category,
                    hospitalId,
                    frameworkId.Value,
                    parity.Delta,
                    parity.Mismatches.Count);
            }

            var oldScoreSnapshot = new
            {
                score.OverallScore,
                score.AuditConfidenceScore,
                score.ChapterScores
            };

            if (_migrationSafetyOptions.UseUnifiedFinding)
            {
                var deterministic = await _deterministicScoring.EvaluateAsync(hospitalId, frameworkId.Value, null, cancellationToken);
                await _eventEmitter.EmitAsync(
                    ComplianceEventType.ComplianceEvaluated,
                    deterministic.JourneyId ?? frameworkId.Value,
                    "ComplianceScoring",
                    Guid.Empty,
                    null,
                    new
                    {
                        hospitalId,
                        frameworkId = frameworkId.Value,
                        deterministic.CompliancePercent,
                        deterministic.Pass
                    },
                    cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                score = new Application.ComplianceScoring.ComplianceScoreResult
                {
                    HospitalId = hospitalId,
                    OverallScore = deterministic.CompliancePercent,
                    AuditConfidenceScore = deterministic.CompliancePercent,
                    MajorCount = 0,
                    MinorCount = 0,
                    CriticalCount = 0,
                    ChapterScores = deterministic.Chapters
                        .Select(c => new ChapterScoreDto
                        {
                            ChapterId = c.ChapterId,
                            WeightedScore = c.CompliancePercent,
                            CompletionPercentage = c.CompliancePercent,
                            RiskPenalty = 0
                        })
                        .ToList()
                };
            }

            if (_migrationSafetyOptions.EnableShadowMode)
            {
                await _shadowComparisonService.CompareAndLogAsync(
                    "ComplianceScoring",
                    frameworkId.Value,
                    "ComplianceScoreReport",
                    oldScoreSnapshot,
                    new
                    {
                        score.OverallScore,
                        score.AuditConfidenceScore,
                        score.ChapterScores
                    },
                    Guid.Empty,
                    cancellationToken);
            }
        }
        else
        {
            score = await _scoringService.CalculateHospitalScoreAsync(hospitalId);
        }

        return new ComplianceScoreReportDto
        {
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            OverallScore = score.OverallScore,
            AuditConfidenceScore = score.AuditConfidenceScore,
            MajorCount = score.MajorCount,
            MinorCount = score.MinorCount,
            CriticalCount = score.CriticalCount,
            Chapters = score.ChapterScores,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<AuditFindingsReportDto> GetAuditFindingsReportAsync(
        Guid hospitalId,
        Guid? frameworkId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating audit findings report for HospitalId={HospitalId}, FrameworkId={FrameworkId}",
            hospitalId, frameworkId);

        // Find the active journey for this hospital (+framework if provided)
        var journeysQuery = _dbContext.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId);

        if (frameworkId.HasValue)
        {
            journeysQuery = journeysQuery.Where(j => j.FrameworkId == frameworkId.Value);
        }

        var journey = await journeysQuery
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (journey == null)
        {
            return new AuditFindingsReportDto
            {
                HospitalId = hospitalId,
                FrameworkId = frameworkId,
                TotalFindings = 0,
                MajorCount = 0,
                MinorCount = 0,
                ObservationCount = 0,
                OFICount = 0,
                UnresolvedCount = 0,
                GeneratedAt = DateTime.UtcNow
            };
        }

        // Reuse ComplianceJourneyService non-conformance summary for this journey
        // (ComplianceJourneyService is already used by JourneysController for this DTO)
        // For now, compute counts directly from findings table for the report:
        var findings = await _dbContext.Findings
            .AsNoTracking()
            .Where(f => f.Scope == FindingScope.Audit && f.ComplianceJourneyId == journey.Id)
            .ToListAsync(cancellationToken);

        var summary = new NonConformanceSummaryDto
        {
            TotalCount = findings.Count,
            MajorCount = findings.Count(f => f.Type == Shared.Infrastructure.Domain.Enums.NonConformanceType.MajorNC),
            MinorCount = findings.Count(f => f.Type == Shared.Infrastructure.Domain.Enums.NonConformanceType.MinorNC),
            ObservationCount = findings.Count(f => f.Type == Shared.Infrastructure.Domain.Enums.NonConformanceType.Observation),
            OFICount = findings.Count(f => f.Type == Shared.Infrastructure.Domain.Enums.NonConformanceType.OFI),
            UnresolvedCount = findings.Count(f => !f.IsResolved)
        };

        return new AuditFindingsReportDto
        {
            HospitalId = hospitalId,
            FrameworkId = frameworkId ?? journey.FrameworkId,
            TotalFindings = summary.TotalCount,
            MajorCount = summary.MajorCount,
            MinorCount = summary.MinorCount,
            ObservationCount = summary.ObservationCount,
            OFICount = summary.OFICount,
            UnresolvedCount = summary.UnresolvedCount,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<NabhReadinessReportDto> GetNabhReadinessReportAsync(
        Guid hospitalId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating NABH readiness report for HospitalId={HospitalId}",
            hospitalId);

        // For readiness we combine framework-level score (for NABH frameworks) and journey summary.

        // Try to locate NABH framework(s) for this hospital
        var nabhFrameworkIds = await _dbContext.ComplianceFrameworks
            .AsNoTracking()
            .Where(f => f.Name.StartsWith("NABH", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        Application.ComplianceScoring.ComplianceScoreResult? nabhScore = null;
        if (nabhFrameworkIds.Any())
        {
            // Use first NABH framework for readiness; can be extended to multi-framework later
            var fwId = nabhFrameworkIds.First();
            nabhScore = await _scoringService.CalculateFrameworkScoreAsync(hospitalId, fwId);
        }
        else
        {
            _logger.LogWarning("No NABH frameworks configured when generating readiness report for hospital {HospitalId}", hospitalId);
        }

        // Journey summary (all frameworks) gives risk/riskIndex and status counts
        // TenantId is not available directly here; we project by hospital only and treat organization as tenant for summary.
        // For now, use hospital-level summary without tenant filter (consistent with existing dashboard behavior).
        var tenantId = Guid.Empty; // placeholder; dashboard readiness is primarily score-based here
        var summary = await _auditService.GetJourneySummaryAsync(hospitalId, tenantId, null, cancellationToken);

        return new NabhReadinessReportDto
        {
            HospitalId = hospitalId,
            OverallScore = nabhScore?.OverallScore ?? 0,
            RiskIndex = summary.RiskIndex,
            ApprovedJourneys = summary.Approved,
            ExpiringIn90Days = summary.ExpiringIn90Days,
            SuspendedJourneys = summary.Suspended,
            GeneratedAt = DateTime.UtcNow
        };
    }
}

