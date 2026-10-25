using ComplianceService.Application.Contracts;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Daily QDMS job: Effective documents past <see cref="ControlledDocument.NextReviewDate"/>
/// transition to ReviewDue and open a <see cref="DocumentReview"/> record when none exists.
/// </summary>
public sealed class DocumentReviewDueJob
{
    private readonly ComplianceDbContext _db;
    private readonly IComplianceNotificationPublisher _notifications;
    private readonly ILogger<DocumentReviewDueJob> _logger;

    public DocumentReviewDueJob(
        ComplianceDbContext db,
        IComplianceNotificationPublisher notifications,
        ILogger<DocumentReviewDueJob> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessReviewDueAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting DocumentReviewDueJob");

        var now = DateTime.UtcNow;
        var candidates = await _db.ControlledDocuments
            .Where(d => !d.IsArchived
                        && d.Status == ControlledDocumentStatus.Effective
                        && d.NextReviewDate != null
                        && d.NextReviewDate <= now)
            .OrderBy(d => d.NextReviewDate)
            .Take(500)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} Effective documents past NextReviewDate", candidates.Count);

        var processed = 0;
        foreach (var doc in candidates)
        {
            try
            {
                if (doc.IsPastExpiry(now))
                {
                    // Expiry job owns archival; skip review-due for already-expired policies.
                    continue;
                }

                var from = doc.Status;
                doc.TransitionTo(ControlledDocumentStatus.ReviewDue, userId: null, utcNow: now);

                var hasOpenReview = await _db.DocumentReviews.AnyAsync(
                    r => r.ControlledDocumentId == doc.Id && r.CompletedDate == null,
                    cancellationToken);

                if (!hasOpenReview)
                {
                    _db.DocumentReviews.Add(new DocumentReview
                    {
                        Id = Guid.NewGuid(),
                        ControlledDocumentId = doc.Id,
                        DocumentVersionId = doc.CurrentVersionId,
                        ScheduledDate = doc.NextReviewDate,
                        ReviewerUserId = doc.ReviewOwnerUserId ?? doc.ReviewerUserId,
                        ReviewerName = doc.ReviewOwnerName ?? doc.ReviewerName,
                        CreatedAt = now,
                        RowVersion = new byte[8],
                    });
                }

                _db.DocumentLifecycleAuditEntries.Add(new DocumentLifecycleAuditEntry
                {
                    Id = Guid.NewGuid(),
                    ControlledDocumentId = doc.Id,
                    DocumentVersionId = doc.CurrentVersionId,
                    EventType = "ReviewDue",
                    FromStatus = from,
                    ToStatus = ControlledDocumentStatus.ReviewDue,
                    Details = "Automated review-due transition",
                    CreatedAt = now,
                    RowVersion = new byte[8],
                });

                await _notifications.PublishAsync(new ComplianceNotificationMessage
                {
                    EventType = "DocumentReviewDue",
                    Title = "Document review due",
                    Body = $"{doc.DocumentNumber} '{doc.Title}' is due for review.",
                    HospitalId = doc.HospitalId,
                    OrganizationId = doc.TenantId,
                    RecipientUserId = (doc.ReviewOwnerUserId ?? doc.OwnerUserId)?.ToString(),
                    Data = new Dictionary<string, string>
                    {
                        ["documentId"] = doc.Id.ToString(),
                        ["documentNumber"] = doc.DocumentNumber,
                        ["nextReviewDate"] = doc.NextReviewDate?.ToString("O") ?? string.Empty,
                    },
                }, cancellationToken);

                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed review-due transition for document {DocumentId}", doc.Id);
            }
        }

        if (processed > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DocumentReviewDueJob completed — Processed={Processed}", processed);
    }
}
