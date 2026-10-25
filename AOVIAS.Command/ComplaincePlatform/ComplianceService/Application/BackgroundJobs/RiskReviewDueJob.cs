using ComplianceService.Application.Contracts;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Daily job: active risks past <see cref="Domain.Risk.ReviewDueDate"/> move to UnderReview and notify owners.
/// </summary>
public sealed class RiskReviewDueJob
{
    private readonly ComplianceDbContext _db;
    private readonly IComplianceNotificationPublisher _notifications;
    private readonly ILogger<RiskReviewDueJob> _logger;

    public RiskReviewDueJob(
        ComplianceDbContext db,
        IComplianceNotificationPublisher notifications,
        ILogger<RiskReviewDueJob> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessReviewDueAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting RiskReviewDueJob");

        var now = DateTime.UtcNow;
        var candidates = await _db.Risks
            .Where(r => r.Status != RiskStatus.Closed
                        && r.Status != RiskStatus.UnderReview
                        && r.ReviewDueDate != null
                        && r.ReviewDueDate <= now)
            .OrderBy(r => r.ReviewDueDate)
            .Take(500)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} risks past ReviewDueDate", candidates.Count);

        var processed = 0;
        foreach (var risk in candidates)
        {
            try
            {
                if (!risk.Status.CanTransitionTo(RiskStatus.UnderReview))
                    continue;

                risk.TransitionTo(RiskStatus.UnderReview, now);

                await _notifications.PublishAsync(new ComplianceNotificationMessage
                {
                    EventType = "RiskReviewDue",
                    Title = "Risk review due",
                    Body = $"Risk '{risk.Title}' is due for review (score {risk.RiskScore}).",
                    HospitalId = risk.HospitalId,
                    OrganizationId = risk.TenantId,
                    RecipientUserId = risk.OwnerUserId?.ToString(),
                    Data = new Dictionary<string, string>
                    {
                        ["riskId"] = risk.Id.ToString(),
                        ["title"] = risk.Title,
                        ["reviewDueDate"] = risk.ReviewDueDate?.ToString("O") ?? string.Empty,
                        ["riskScore"] = risk.RiskScore.ToString(),
                    },
                }, cancellationToken);

                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed review-due transition for risk {RiskId}", risk.Id);
            }
        }

        if (processed > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("RiskReviewDueJob completed — Processed={Processed}", processed);
    }
}
