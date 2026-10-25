using ComplianceService.Application.Contracts;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Daily QDMS job: documents past <see cref="ControlledDocument.ExpiryDate"/> are archived
/// (expired policies cannot stay Effective) and a notification is emitted.
/// </summary>
public sealed class DocumentExpiryJob
{
    private readonly ComplianceDbContext _db;
    private readonly IComplianceNotificationPublisher _notifications;
    private readonly ILogger<DocumentExpiryJob> _logger;

    public DocumentExpiryJob(
        ComplianceDbContext db,
        IComplianceNotificationPublisher notifications,
        ILogger<DocumentExpiryJob> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessExpiredDocumentsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting DocumentExpiryJob");

        var now = DateTime.UtcNow;
        var candidates = await _db.ControlledDocuments
            .Where(d => d.ExpiryDate != null
                        && d.ExpiryDate <= now
                        && d.Status != ControlledDocumentStatus.Archived
                        && !d.IsArchived)
            .OrderBy(d => d.ExpiryDate)
            .Take(500)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} documents past ExpiryDate", candidates.Count);

        var processed = 0;
        foreach (var doc in candidates)
        {
            try
            {
                var from = doc.Status;
                // Expired policies cannot stay Effective — archive (allowed from any non-archived state).
                doc.TransitionTo(ControlledDocumentStatus.Archived, userId: null, utcNow: now);

                _db.DocumentLifecycleAuditEntries.Add(new DocumentLifecycleAuditEntry
                {
                    Id = Guid.NewGuid(),
                    ControlledDocumentId = doc.Id,
                    DocumentVersionId = doc.CurrentVersionId,
                    EventType = "ExpiredArchived",
                    FromStatus = from,
                    ToStatus = ControlledDocumentStatus.Archived,
                    Details = $"Auto-archived past ExpiryDate {doc.ExpiryDate:O}",
                    CreatedAt = now,
                    RowVersion = new byte[8],
                });

                await _notifications.PublishAsync(new ComplianceNotificationMessage
                {
                    EventType = "DocumentExpired",
                    Title = "Controlled document expired",
                    Body = $"{doc.DocumentNumber} '{doc.Title}' expired and was archived.",
                    HospitalId = doc.HospitalId,
                    OrganizationId = doc.TenantId,
                    RecipientUserId = doc.OwnerUserId?.ToString(),
                    Data = new Dictionary<string, string>
                    {
                        ["documentId"] = doc.Id.ToString(),
                        ["documentNumber"] = doc.DocumentNumber,
                        ["expiryDate"] = doc.ExpiryDate?.ToString("O") ?? string.Empty,
                        ["previousStatus"] = from.ToString(),
                    },
                }, cancellationToken);

                _logger.LogWarning(
                    "DocumentExpired NotificationHook DocumentId={DocumentId} DocumentNumber={DocumentNumber} ExpiryDate={ExpiryDate} PreviousStatus={Previous}",
                    doc.Id, doc.DocumentNumber, doc.ExpiryDate, from);

                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive expired document {DocumentId}", doc.Id);
            }
        }

        if (processed > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DocumentExpiryJob completed — Processed={Processed}", processed);
    }
}
