using ComplianceService.Application.Commands;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Executes a full Assessment → Obligation → Deficiency → Snapshot rebuild for every hospital
/// that has a saved wizard assessment.
///
/// Safe to run multiple times (idempotent).  Multi-tenant safe (processes all tenants or a scoped
/// subset).  Failures for one hospital are isolated and logged; processing continues for the rest.
/// </summary>
public sealed class RebuildObligationAssessmentCommandHandler
{
    private readonly ComplianceDbContext                        _db;
    private readonly IOnboardingObligationAssessmentService    _applyService;
    private readonly IComplianceSnapshotRecalculationQueue     _snapshotQueue;
    private readonly ILogger<RebuildObligationAssessmentCommandHandler> _logger;

    public RebuildObligationAssessmentCommandHandler(
        ComplianceDbContext db,
        IOnboardingObligationAssessmentService applyService,
        IComplianceSnapshotRecalculationQueue snapshotQueue,
        ILogger<RebuildObligationAssessmentCommandHandler> logger)
    {
        _db            = db;
        _applyService  = applyService;
        _snapshotQueue = snapshotQueue;
        _logger        = logger;
    }

    public async Task<RebuildObligationAssessmentReport> HandleAsync(
        RebuildObligationAssessmentCommand command,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RebuildObligations: Starting. OrgFilter={Org} HospitalFilter={Hospital} FrameworkFilter={Fw} DryRun={Dry}",
            command.OrganizationId, command.HospitalId, command.FrameworkId, command.DryRun);

        // ── 1. Find all wizard assessments (most-recent per hospital) ───────────────
        var assessmentQuery = _db.WizardOnboardingAssessments
            .IgnoreQueryFilters()
            .AsNoTracking();

        if (command.OrganizationId.HasValue)
            assessmentQuery = assessmentQuery.Where(a => a.OrganizationId == command.OrganizationId.Value);

        if (command.HospitalId.HasValue)
            assessmentQuery = assessmentQuery.Where(a => a.HospitalId == command.HospitalId.Value);

        // Keep only the latest assessment per hospital.
        var latestPerHospital = await assessmentQuery
            .GroupBy(a => new { a.HospitalId, a.OrganizationId })
            .Select(g => new
            {
                g.Key.HospitalId,
                g.Key.OrganizationId,
                LatestAt = g.Max(a => a.CreatedAt)
            })
            .ToListAsync(ct);

        if (latestPerHospital.Count == 0)
        {
            _logger.LogWarning("RebuildObligations: No wizard assessments found for the given filters. Exiting.");
            return new RebuildObligationAssessmentReport
            {
                DryRun       = command.DryRun,
                TotalHospitals = 0,
                RebuiltAt    = DateTime.UtcNow,
            };
        }

        // ── 2. Resolve (hospitalId, frameworkId) pairs from ComplianceJourneys ─────
        var hospitalIds = latestPerHospital.Select(h => h.HospitalId).Distinct().ToList();

        var journeyQuery = _db.ComplianceJourneys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(j => hospitalIds.Contains(j.HospitalId));

        if (command.FrameworkId.HasValue)
            journeyQuery = journeyQuery.Where(j => j.FrameworkId == command.FrameworkId.Value);

        var journeys = await journeyQuery
            .Select(j => new { j.HospitalId, j.FrameworkId, j.OrganizationId })
            .Distinct()
            .ToListAsync(ct);

        // Build a lookup: hospitalId → list of frameworkIds
        var frameworksByHospital = journeys
            .GroupBy(j => j.HospitalId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(j => new { j.FrameworkId, j.OrganizationId }).Distinct().ToList());

        // ── 3. Process each hospital × framework ─────────────────────────────────────
        var results = new List<HospitalFrameworkRebuildResult>();
        int totalObligationsUpdated = 0;
        int totalAnswersProcessed   = 0;

        foreach (var row in latestPerHospital)
        {
            if (!frameworksByHospital.TryGetValue(row.HospitalId, out var frameworks) || frameworks.Count == 0)
            {
                _logger.LogWarning(
                    "RebuildObligations: No compliance journey for HospitalId={HospitalId}. Skipping.",
                    row.HospitalId);
                continue;
            }

            foreach (var fw in frameworks)
            {
                if (command.DryRun)
                {
                    _logger.LogInformation(
                        "RebuildObligations [DryRun]: Would process HospitalId={Hospital} FrameworkId={Fw}.",
                        row.HospitalId, fw.FrameworkId);

                    results.Add(new HospitalFrameworkRebuildResult
                    {
                        HospitalId     = row.HospitalId,
                        OrganizationId = row.OrganizationId,
                        FrameworkId    = fw.FrameworkId,
                        Succeeded      = true,
                    });
                    continue;
                }

                try
                {
                    _logger.LogInformation(
                        "RebuildObligations: Processing HospitalId={Hospital} OrgId={Org} FrameworkId={Fw}.",
                        row.HospitalId, row.OrganizationId, fw.FrameworkId);

                    // ApplyAsync covers: sync obligations → update from answers → evaluate deficiencies → flush outbox
                    var updated = await _applyService.ApplyAsync(
                        organizationId: row.OrganizationId,
                        hospitalId:     row.HospitalId,
                        frameworkId:    fw.FrameworkId,
                        answers:        null, // loads from WizardOnboardingAssessments table
                        ct:             ct);

                    // Phase 7 — queue snapshot recalculation (fire-and-forget, non-blocking)
                    _snapshotQueue.EnqueueHospitalFramework(row.HospitalId, fw.FrameworkId);
                    _snapshotQueue.EnqueueMaturityRecalc(row.HospitalId, row.OrganizationId);

                    totalObligationsUpdated += updated;

                    results.Add(new HospitalFrameworkRebuildResult
                    {
                        HospitalId          = row.HospitalId,
                        OrganizationId      = row.OrganizationId,
                        FrameworkId         = fw.FrameworkId,
                        Succeeded           = true,
                        ObligationsUpdated  = updated,
                        SnapshotQueued      = true,
                    });

                    _logger.LogInformation(
                        "RebuildObligations: Completed HospitalId={Hospital} FrameworkId={Fw} — {N} obligations updated, snapshot queued.",
                        row.HospitalId, fw.FrameworkId, updated);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "RebuildObligations: Failed for HospitalId={Hospital} FrameworkId={Fw}.",
                        row.HospitalId, fw.FrameworkId);

                    results.Add(new HospitalFrameworkRebuildResult
                    {
                        HospitalId     = row.HospitalId,
                        OrganizationId = row.OrganizationId,
                        FrameworkId    = fw.FrameworkId,
                        Succeeded      = false,
                        ErrorMessage   = ex.Message,
                    });
                }
            }
        }

        var report = new RebuildObligationAssessmentReport
        {
            DryRun                   = command.DryRun,
            TotalHospitals           = latestPerHospital.Count,
            ProcessedHospitals       = results.Count,
            SucceededHospitals       = results.Count(r => r.Succeeded),
            FailedHospitals          = results.Count(r => !r.Succeeded),
            TotalObligationsUpdated  = totalObligationsUpdated,
            TotalAnswersProcessed    = totalAnswersProcessed,
            RebuiltAt                = DateTime.UtcNow,
            Results                  = results,
        };

        _logger.LogInformation(
            "RebuildObligations: Finished. Total={Total} Succeeded={Ok} Failed={Fail} ObligationsUpdated={N} DryRun={Dry}",
            report.TotalHospitals, report.SucceededHospitals, report.FailedHospitals,
            report.TotalObligationsUpdated, report.DryRun);

        return report;
    }
}
