using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Service for retrieving compliance snapshots.
/// </summary>
public class SnapshotService : ISnapshotService
{
    private readonly ComplianceDbContext _context;
    private readonly ILogger<SnapshotService> _logger;

    public SnapshotService(
        ComplianceDbContext context,
        ILogger<SnapshotService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ComplianceSnapshot?> GetSnapshotByHospitalIdAsync(Guid hospitalId, CancellationToken cancellationToken = default)
    {
        return await GetSnapshotByHospitalIdAsync(hospitalId, null, cancellationToken);
    }

    /// <summary>
    /// Optional filter <paramref name="frameworkId"/> may be a <see cref="ComplianceSnapshot.FrameworkId"/>
    /// or a <see cref="ComplianceSnapshot.ComplianceJourneyId"/> (clients often pass journey id as ?frameworkId=).
    /// </summary>
    public async Task<ComplianceSnapshot?> GetSnapshotByHospitalIdAsync(Guid hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving compliance snapshot for hospital {HospitalId}, frameworkOrJourney {FrameworkId}",
            hospitalId, frameworkId);

        var baseQuery = _context.ComplianceSnapshots
            .AsNoTracking()
            .Where(s => s.HospitalId == hospitalId);

        ComplianceSnapshot? snapshot;

        if (!frameworkId.HasValue)
        {
            snapshot = await baseQuery
                .OrderByDescending(s => s.CalculatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            var fid = frameworkId.Value;

            snapshot = await baseQuery
                .Where(s => s.FrameworkId == fid)
                .OrderByDescending(s => s.CalculatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (snapshot == null)
            {
                snapshot = await baseQuery
                    .Where(s => s.ComplianceJourneyId == fid)
                    .OrderByDescending(s => s.CalculatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        if (snapshot == null)
        {
            _logger.LogInformation("No snapshot found for hospital {HospitalId}, frameworkOrJourney {FrameworkId}",
                hospitalId, frameworkId);
        }
        else
        {
            _logger.LogInformation(
                "Retrieved snapshot for hospital {HospitalId}: OverallScore={OverallScore}, CalculatedAt={CalculatedAt}",
                hospitalId, snapshot.OverallScore, snapshot.CalculatedAt);
        }

        return snapshot;
    }
}
