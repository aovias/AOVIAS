using ComplianceService.Application.Contracts;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Background job for recalculating compliance snapshots.
/// Handles both single hospital and batch recalculation.
/// </summary>
public class ComplianceSnapshotJob
{
    private readonly IComplianceFrameworkSnapshotRecalculationService _recalculationService;
    private readonly ComplianceDbContext _context;
    private readonly ILogger<ComplianceSnapshotJob> _logger;

    public ComplianceSnapshotJob(
        IComplianceFrameworkSnapshotRecalculationService recalculationService,
        ComplianceDbContext context,
        ILogger<ComplianceSnapshotJob> logger)
    {
        _recalculationService = recalculationService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Recalculates and persists the compliance snapshot for a single hospital (all active journeys / frameworks).
    /// </summary>
    /// <param name="hospitalId">The unique identifier of the hospital.</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 120, 300 })]
    public async Task RecalculateHospitalSnapshot(Guid hospitalId)
    {
        _logger.LogInformation("Starting compliance snapshot recalculation for hospital {HospitalId}", hospitalId);

        try
        {
            var frameworkIds = await _context.ComplianceJourneys
                .IgnoreQueryFilters()
                .Where(j => j.HospitalId == hospitalId)
                .Select(j => j.FrameworkId)
                .Distinct()
                .ToListAsync();

            if (!frameworkIds.Any())
            {
                _logger.LogWarning(
                    "No compliance journeys (frameworks) found for hospital {HospitalId}. Skipping snapshot recalculation.",
                    hospitalId);
                return;
            }

            foreach (var frameworkId in frameworkIds)
            {
                await _recalculationService.RecalculateAsync(hospitalId, frameworkId, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating compliance snapshot for hospital {HospitalId}", hospitalId);
            throw;
        }
    }

    /// <summary>
    /// Recalculates compliance snapshots for all hospitals that have compliance programs.
    /// Processes hospitals sequentially to avoid database connection exhaustion.
    /// </summary>
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 600 })]
    public async Task RecalculateAllHospitals()
    {
        _logger.LogInformation("Starting batch recalculation of compliance snapshots for all hospitals");

        try
        {
            var hospitalIds = await _context.CompliancePrograms
                .Select(p => p.HospitalId)
                .Distinct()
                .ToListAsync();

            if (!hospitalIds.Any())
            {
                _logger.LogWarning("No hospitals found with compliance programs. Skipping batch recalculation.");
                return;
            }

            _logger.LogInformation(
                "Found {Count} hospitals with compliance programs. Starting sequential recalculation.",
                hospitalIds.Count);

            int successCount = 0;
            int failureCount = 0;

            foreach (var hospitalId in hospitalIds)
            {
                try
                {
                    await RecalculateHospitalSnapshot(hospitalId);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(
                        ex,
                        "Failed to recalculate snapshot for hospital {HospitalId}. Continuing with next hospital.",
                        hospitalId);
                }
            }

            _logger.LogInformation(
                "Batch recalculation completed. Total={Total}, Success={Success}, Failed={Failed}",
                hospitalIds.Count,
                successCount,
                failureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during batch recalculation of compliance snapshots");
            throw;
        }
    }
}
