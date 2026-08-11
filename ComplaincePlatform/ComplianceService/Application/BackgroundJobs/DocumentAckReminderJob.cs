using ComplianceService.Application.Contracts;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Daily QDMS job: increments <c>ReminderCount</c> for pending acknowledgements on Effective documents
/// and emits acknowledgement reminder notifications.
/// </summary>
public sealed class DocumentAckReminderJob
{
    private readonly ComplianceDbContext _db;
    private readonly IComplianceNotificationPublisher _notifications;
    private readonly ILogger<DocumentAckReminderJob> _logger;

    /// <summary>Minimum hours between reminders for the same acknowledgement.</summary>
    private const int ReminderCooldownHours = 24;

    public DocumentAckReminderJob(
        ComplianceDbContext db,
        IComplianceNotificationPublisher notifications,
        ILogger<DocumentAckReminderJob> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessAckRemindersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting DocumentAckReminderJob");

        var now = DateTime.UtcNow;
        var cooldown = now.AddHours(-ReminderCooldownHours);

        var pending = await _db.DocumentAcknowledgements
            .Include(a => a.ControlledDocument)
            .Where(a => a.AcknowledgedAt == null
                        && !a.ControlledDocument.IsArchived
                        && a.ControlledDocument.Status == ControlledDocumentStatus.Effective
                        && (a.LastReminderAt == null || a.LastReminderAt <= cooldown))
            .OrderBy(a => a.LastReminderAt ?? a.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} pending acknowledgements for reminder", pending.Count);

        var processed = 0;
        foreach (var ack in pending)
        {
            try
            {
                ack.ReminderCount += 1;
                ack.LastReminderAt = now;

                await _notifications.PublishAsync(new ComplianceNotificationMessage
                {
                    EventType = "DocumentAckReminder",
                    Title = "Document acknowledgement reminder",
                    Body = $"Please acknowledge {ack.ControlledDocument.DocumentNumber} '{ack.ControlledDocument.Title}'.",
                    HospitalId = ack.ControlledDocument.HospitalId,
                    OrganizationId = ack.ControlledDocument.TenantId,
                    RecipientUserId = ack.AssigneeUserId?.ToString() ?? ack.AssigneeKey,
                    Data = new Dictionary<string, string>
                    {
                        ["acknowledgementId"] = ack.Id.ToString(),
                        ["documentId"] = ack.ControlledDocumentId.ToString(),
                        ["documentNumber"] = ack.ControlledDocument.DocumentNumber,
                        ["reminderCount"] = ack.ReminderCount.ToString(),
                    },
                }, cancellationToken);

                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed ack reminder for acknowledgement {AckId}", ack.Id);
            }
        }

        if (processed > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DocumentAckReminderJob completed — Processed={Processed}", processed);
    }
}
