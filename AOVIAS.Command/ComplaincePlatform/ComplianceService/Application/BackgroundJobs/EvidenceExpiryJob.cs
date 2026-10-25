using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Daily job: finds evidence past <see cref="Evidence.ExpiresAt"/> and marks lifecycle Rejected
/// (closest existing status — no Expired column). Logs a notification hook for downstream alerts.
/// </summary>
public sealed class EvidenceExpiryJob
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<EvidenceExpiryJob> _logger;

    public EvidenceExpiryJob(ComplianceDbContext db, ILogger<EvidenceExpiryJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessExpiredEvidenceAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting EvidenceExpiryJob");

        var now = DateTime.UtcNow;
        var candidates = await _db.Evidences
            .Where(e => e.ExpiresAt != null
                        && e.ExpiresAt <= now
                        && e.LifecycleStatus != Evidence.EvidenceLifecycleStatus.Rejected)
            .OrderBy(e => e.ExpiresAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} evidence records past ExpiresAt", candidates.Count);

        var processed = 0;
        foreach (var evidence in candidates)
        {
            try
            {
                var previous = evidence.LifecycleStatus;
                evidence.LifecycleStatus = Evidence.EvidenceLifecycleStatus.Rejected;

                // Notification hook (Outbox/alerts can subscribe later via structured log / event)
                _logger.LogWarning(
                    "EvidenceExpired NotificationHook EvidenceId={EvidenceId} HospitalId={HospitalId} FrameworkId={FrameworkId} ExpiresAt={ExpiresAt} PreviousLifecycle={Previous}",
                    evidence.Id, evidence.HospitalId, evidence.FrameworkId, evidence.ExpiresAt, previous);

                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire evidence {EvidenceId}", evidence.Id);
            }
        }

        if (processed > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("EvidenceExpiryJob completed — Processed={Processed}", processed);
    }
}
