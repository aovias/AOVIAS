using ComplianceService.Application.Dashboard.Projection;
using ComplianceService.Application.Aggregation;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Dashboard.Summary;
using ComplianceService.Application.Decision;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Journey;
using ComplianceService.Domain;
using ComplianceService.Domain.Policies;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;
using System.Text.Json;

namespace ComplianceService.Application.Services;

/// <summary>
/// Appends a time-series <see cref="ComplianceSnapshot"/> for (hospital, framework) using the active journey.
/// </summary>
public sealed class ComplianceFrameworkSnapshotRecalculationService : IComplianceFrameworkSnapshotRecalculationService
{
    private readonly IAggregationService _aggregationService;
    private readonly DecisionService _decisionService;
    private readonly ComplianceDbContext _context;
    private readonly ILogger<ComplianceFrameworkSnapshotRecalculationService> _logger;
    private readonly IOperationalModeClassificationPolicy _operationalModeClassification;
    private readonly IObligationQueryService _obligationQueryService;
    private readonly IDeficiencyQueryService _deficiencyQueryService;
    private readonly IDashboardSummaryCache _dashboardSummaryCache;
    private readonly IDashboardProjectionEngine _dashboardProjectionEngine;

    public ComplianceFrameworkSnapshotRecalculationService(
        IAggregationService aggregationService,
        DecisionService decisionService,
        ComplianceDbContext context,
        ILogger<ComplianceFrameworkSnapshotRecalculationService> logger,
        IOperationalModeClassificationPolicy operationalModeClassification,
        IObligationQueryService obligationQueryService,
        IDeficiencyQueryService deficiencyQueryService,
        IDashboardSummaryCache dashboardSummaryCache,
        IDashboardProjectionEngine dashboardProjectionEngine)
    {
        _aggregationService    = aggregationService;
        _decisionService       = decisionService;
        _context               = context;
        _logger                = logger;
        _operationalModeClassification = operationalModeClassification;
        _obligationQueryService  = obligationQueryService;
        _deficiencyQueryService  = deficiencyQueryService;
        _dashboardSummaryCache = dashboardSummaryCache;
        _dashboardProjectionEngine = dashboardProjectionEngine;
    }

    /// <inheritdoc />
    public async Task<bool> RecalculateAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty || frameworkId == Guid.Empty)
        {
            _logger.LogWarning(
                "Snapshot recalculation skipped: invalid ids HospitalId={HospitalId} FrameworkId={FrameworkId}",
                hospitalId,
                frameworkId);
            return false;
        }

        var journey = await ComplianceJourneySnapshotResolver.GetActiveJourneyForSnapshotAsync(
            _context,
            hospitalId,
            frameworkId,
            cancellationToken);

        if (journey == null)
        {
            _logger.LogWarning(
                "Snapshot recalculation skipped: no ComplianceJourney for HospitalId={HospitalId} FrameworkId={FrameworkId}",
                hospitalId,
                frameworkId);
            return false;
        }

        var journeyId = journey.Id;

        try
        {
            var agg = await _aggregationService.ComputeFrameworkAsync(
                frameworkId,
                journeyId,
                _operationalModeClassification.GetClassificationsForDecisionRules(OperationalMode.FinalAudit),
                cancellationToken);

            var decision = await _decisionService.EvaluateAsync(
                frameworkId,
                journeyId,
                OperationalMode.FinalAudit,
                cancellationToken);

            ObligationMaturityBreakdownDto? obligationBreakdown = null;
            try
            {
                obligationBreakdown = await _obligationQueryService.GetMaturityBreakdownAsync(
                    journey.OrganizationId,
                    frameworkId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to load obligation breakdown for snapshot HospitalId={HospitalId} FrameworkId={FrameworkId} — continuing without it",
                    hospitalId, frameworkId);
            }

            DeficiencySummaryDto? deficiencySummary = null;
            try
            {
                deficiencySummary = await _deficiencyQueryService.GetSummaryAsync(
                    hospitalId, frameworkId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to load deficiency summary for snapshot HospitalId={HospitalId} FrameworkId={FrameworkId} — continuing without it",
                    hospitalId, frameworkId);
            }

            await AppendSnapshotAsync(
                hospitalId,
                frameworkId,
                journeyId,
                journey.TenantId,
                agg,
                decision,
                obligationBreakdown,
                deficiencySummary,
                cancellationToken);

            _logger.LogInformation(
                "Appended compliance snapshot for HospitalId={HospitalId} FrameworkId={FrameworkId} JourneyId={JourneyId} score={Score:F2}",
                hospitalId,
                frameworkId,
                journeyId,
                agg.CompliancePercent);

            _dashboardSummaryCache.Invalidate(journey.TenantId, hospitalId, frameworkId);

            await _dashboardProjectionEngine.RebuildAsync(
                DashboardProjectionScope.ForHospital(journey.TenantId, hospitalId, frameworkId),
                "ComplianceFrameworkSnapshotRecalculated",
                cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Snapshot recalculation failed for HospitalId={HospitalId} FrameworkId={FrameworkId}",
                hospitalId,
                frameworkId);
            throw;
        }
    }

    private async Task AppendSnapshotAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid complianceJourneyId,
        Guid tenantId,
        ComplianceService.Application.Aggregation.Models.FrameworkAggregateResult agg,
        ComplianceService.Application.Decision.Models.DecisionResult decision,
        ObligationMaturityBreakdownDto? obligationBreakdown,
        DeficiencySummaryDto? deficiencySummary,
        CancellationToken ct)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var breakdownPayload = new
                {
                    Chapters           = agg.Chapters,
                    Decision           = decision,
                    ObligationBreakdown = obligationBreakdown,
                    DeficiencySummary  = deficiencySummary,
                };
                var chapterBreakdownJson = JsonSerializer.Serialize(breakdownPayload);

                var newSnapshot = new ComplianceSnapshot
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    HospitalId = hospitalId,
                    FrameworkId = frameworkId,
                    ComplianceJourneyId = complianceJourneyId,
                    OverallScore = agg.CompliancePercent,
                    AuditConfidenceScore = agg.CompliancePercent,
                    ChapterBreakdownJson = chapterBreakdownJson,
                    CriticalCount = 0,
                    MajorCount = 0,
                    MinorCount = 0,
                    MajorNCCount = 0,
                    MinorNCCount = 0,
                    ObservationCount = 0,
                    OFICount = 0,
                    HighRiskChaptersJson = "[]",
                    CalculatedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ComplianceSnapshots.AddAsync(newSnapshot, ct);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(
                    ex,
                    "Error inserting compliance snapshot for hospital {HospitalId}, framework {FrameworkId}",
                    hospitalId,
                    frameworkId);
                throw;
            }
        });
    }
}
