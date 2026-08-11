using ComplianceService.Application.Analytics;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Generates append-only daily analytics rollups for historical trend analysis (Sprint 12B-1).
/// </summary>
public sealed class AnalyticsDailySnapshotJob
{
    private readonly IAnalyticsSnapshotService _snapshotService;
    private readonly ComplianceDbContext _db;
    private readonly ILogger<AnalyticsDailySnapshotJob> _logger;

    public AnalyticsDailySnapshotJob(
        IAnalyticsSnapshotService snapshotService,
        ComplianceDbContext db,
        ILogger<AnalyticsDailySnapshotJob> logger)
    {
        _snapshotService = snapshotService;
        _db = db;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 120, 300 })]
    public Task GenerateAllDailySnapshotsAsync(CancellationToken cancellationToken = default) =>
        _snapshotService.GenerateAllOrganizationsDailyAsync(ct: cancellationToken);

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 180 })]
    public async Task GenerateHospitalDailySnapshotAsync(Guid organizationId, Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var scope = AnalyticsScope.ForHospital(organizationId, hospitalId, frameworkId);
        await _snapshotService.GenerateDailySnapshotAsync(scope, ct: cancellationToken);
    }
}
