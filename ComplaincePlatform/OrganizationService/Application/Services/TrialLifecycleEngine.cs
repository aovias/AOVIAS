using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrganizationService.Configuration;
using OrganizationService.Domain;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Email;

namespace OrganizationService.Application.Services;

public sealed class TrialLifecycleRunResult
{
    public Guid RunId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int TransitionCount { get; set; }
    public int ReminderCount { get; set; }
    public int FailureCount { get; set; }
    public IReadOnlyList<Guid> FailedLicenseKeyIds { get; set; } = Array.Empty<Guid>();
}

/// <summary>CRP Phase 5 — centralized trial lifecycle evaluation (idempotent, auditable).</summary>
public sealed class TrialLifecycleEngine
{
    private readonly OrganizationDbContext _db;
    private readonly LicenseAppService _licenses;
    private readonly CommercialAdminAuditAppService _audit;
    private readonly IEmailService _email;
    private readonly TrialLifecycleOptions _options;
    private readonly ILogger<TrialLifecycleEngine> _logger;

    public TrialLifecycleEngine(
        OrganizationDbContext db,
        LicenseAppService licenses,
        CommercialAdminAuditAppService audit,
        IEmailService email,
        IOptions<TrialLifecycleOptions> options,
        ILogger<TrialLifecycleEngine> logger)
    {
        _db = db;
        _licenses = licenses;
        _audit = audit;
        _email = email;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TrialLifecycleRunResult> EvaluateAllAsync(
        string trigger,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var corr = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId.Trim();
        var run = new TrialLifecycleRun
        {
            Id = Guid.NewGuid(),
            Trigger = trigger,
            CorrelationId = corr,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.TrialLifecycleRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        var failed = new List<Guid>();
        var processed = 0;
        var transitions = 0;
        var reminders = 0;
        Guid? cursor = null;
        var batchSize = Math.Clamp(_options.BatchSize, 10, 500);

        while (true)
        {
            var q = _db.LicenseKeys.AsQueryable()
                .Where(k => k.LicenseType == LicenseType.Trial && k.Status != LicenseStatus.Revoked);
            if (cursor is { } c)
            {
                q = q.Where(k => k.Id.CompareTo(c) > 0);
            }

            var batch = await q.OrderBy(k => k.Id).Take(batchSize).ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            foreach (var key in batch)
            {
                try
                {
                    var outcome = await EvaluateKeyAsync(key, corr, cancellationToken);
                    processed++;
                    transitions += outcome.Transitions;
                    reminders += outcome.Reminders;
                }
                catch (Exception ex)
                {
                    failed.Add(key.Id);
                    _logger.LogError(ex, "Trial lifecycle failed for key {KeyId} org {Org}", key.Id, key.OrganizationId);
                }
            }

            cursor = batch[^1].Id;
            if (batch.Count < batchSize)
            {
                break;
            }
        }

        run.ProcessedCount = processed;
        run.TransitionCount = transitions;
        run.ReminderCount = reminders;
        run.FailureCount = failed.Count;
        run.FailedLicenseKeyIdsJson = failed.Count == 0 ? null : JsonSerializer.Serialize(failed);
        run.FinishedAtUtc = DateTime.UtcNow;
        if (failed.Count > 0)
        {
            run.ErrorSummary = $"{failed.Count} key(s) failed; see FailedLicenseKeyIdsJson";
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new TrialLifecycleRunResult
        {
            RunId = run.Id,
            CorrelationId = corr,
            ProcessedCount = processed,
            TransitionCount = transitions,
            ReminderCount = reminders,
            FailureCount = failed.Count,
            FailedLicenseKeyIds = failed
        };
    }

    public async Task<TrialLifecycleRun?> GetLatestRunAsync(CancellationToken cancellationToken = default)
        => await _db.TrialLifecycleRuns.AsNoTracking()
            .OrderByDescending(r => r.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<object> GetHealthSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var trials = _db.LicenseKeys.AsNoTracking()
            .Where(k => k.LicenseType == LicenseType.Trial && k.Status != LicenseStatus.Revoked);

        var active = await trials.CountAsync(k => k.Status == LicenseStatus.Active || k.Status == LicenseStatus.Issued, cancellationToken);
        var grace = await trials.CountAsync(k => k.Status == LicenseStatus.Grace, cancellationToken);
        var expired = await trials.CountAsync(k => k.Status == LicenseStatus.Expired, cancellationToken);
        var expiring = await trials.CountAsync(
            k => (k.Status == LicenseStatus.Active || k.Status == LicenseStatus.Issued)
                 && k.ExpiresAtUtc != null
                 && k.ExpiresAtUtc <= now.AddDays(7)
                 && k.ExpiresAtUtc > now,
            cancellationToken);

        var last = await GetLatestRunAsync(cancellationToken);
        return new
        {
            activeTrials = active,
            expiringWithin7Days = expiring,
            graceTrials = grace,
            expiredTrials = expired,
            lastRun = last == null ? null : new
            {
                last.Id,
                last.Trigger,
                last.StartedAtUtc,
                last.FinishedAtUtc,
                last.ProcessedCount,
                last.TransitionCount,
                last.ReminderCount,
                last.FailureCount,
                last.CorrelationId
            },
            checkedAtUtc = now
        };
    }

    private sealed record KeyOutcome(int Transitions, int Reminders);

    private async Task<KeyOutcome> EvaluateKeyAsync(LicenseKey key, string correlationId, CancellationToken ct)
    {
        var previousStatus = key.Status;
        // Persist RefreshStatus via ValidateAsync path semantics
        await _licenses.ValidateAsync(key.OrganizationId, ct);
        var refreshed = await _db.LicenseKeys.FirstAsync(k => k.Id == key.Id, ct);

        var transitions = 0;
        var reminders = 0;

        if (refreshed.Status != previousStatus)
        {
            transitions++;
            await _audit.RecordAsync(
                refreshed.OrganizationId,
                null,
                "system:trial-lifecycle",
                "Trial",
                refreshed.Id.ToString(),
                $"Status:{previousStatus}->{refreshed.Status}",
                previousStatus.ToString(),
                refreshed.Status.ToString(),
                "Automated trial lifecycle evaluation",
                correlationId,
                ct);

            if (refreshed.Status == LicenseStatus.Grace && previousStatus != LicenseStatus.Grace)
            {
                if (await TryNotifyAsync(refreshed, TrialLifecycleMilestones.GraceStarted, correlationId, ct))
                {
                    reminders++;
                }

                await EmitOutboxAsync(refreshed, "GraceStarted", correlationId, ct);
                await TryInternalAlertAsync(refreshed, "Trial entered grace", ct);
            }

            if (refreshed.Status == LicenseStatus.Expired && previousStatus != LicenseStatus.Expired)
            {
                if (await TryNotifyAsync(refreshed, TrialLifecycleMilestones.TrialExpired, correlationId, ct))
                {
                    reminders++;
                }

                if (await TryNotifyAsync(refreshed, TrialLifecycleMilestones.ReadOnlyEnabled, correlationId, ct))
                {
                    reminders++;
                }

                await EmitOutboxAsync(refreshed, "TrialExpired", correlationId, ct);
                await EmitOutboxAsync(refreshed, "ReadOnlyEnabled", correlationId, ct);
                await TryInternalAlertAsync(refreshed, "Trial expired — read-only", ct);
                await MaybeAutoArchiveAsync(refreshed, correlationId, ct);
            }
        }

        // Reminder milestones while still Active/Issued (before expiry)
        if (refreshed.Status is LicenseStatus.Active or LicenseStatus.Issued
            && refreshed.ExpiresAtUtc is { } expires)
        {
            var daysLeft = (int)Math.Ceiling((expires - DateTime.UtcNow).TotalDays);
            foreach (var day in _options.ReminderDays.Distinct().OrderByDescending(d => d))
            {
                if (daysLeft == day)
                {
                    var milestone = TrialLifecycleMilestones.ReminderForDays(day);
                    if (await TryNotifyAsync(refreshed, milestone, correlationId, ct))
                    {
                        reminders++;
                        await EmitOutboxAsync(refreshed, milestone, correlationId, ct);
                    }
                }
            }
        }

        // Grace ending soon
        if (refreshed.Status == LicenseStatus.Grace && refreshed.GraceEndsAtUtc is { } graceEnd)
        {
            var hoursLeft = (graceEnd - DateTime.UtcNow).TotalHours;
            if (hoursLeft > 0 && hoursLeft <= Math.Max(1, _options.GraceEndingReminderHours))
            {
                if (await TryNotifyAsync(refreshed, TrialLifecycleMilestones.GraceEnding, correlationId, ct))
                {
                    reminders++;
                    await EmitOutboxAsync(refreshed, "GraceEnding", correlationId, ct);
                }
            }
        }

        // High seat usage alert
        if (refreshed.MaxSeats > 0
            && refreshed.UsedSeats >= refreshed.MaxSeats * _options.HighSeatUsageThreshold)
        {
            if (await TryNotifyAsync(refreshed, TrialLifecycleMilestones.HighSeatUsage, correlationId, ct, internalOnly: true))
            {
                reminders++;
                await TryInternalAlertAsync(refreshed, "High seat usage on trial", ct);
            }
        }

        return new KeyOutcome(transitions, reminders);
    }

    private async Task MaybeAutoArchiveAsync(LicenseKey key, string correlationId, CancellationToken ct)
    {
        if (_options.AutoArchiveAfterExpiredDays is not { } days || days <= 0)
        {
            return;
        }

        if (key.ExpiresAtUtc is null || key.ExpiresAtUtc.Value.AddDays(key.GracePeriodDays + days) > DateTime.UtcNow)
        {
            return;
        }

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == key.OrganizationId, ct);
        if (org == null || string.Equals(org.Status, OrganizationStatuses.Archived, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_options.AutoSuspendOrganizationOnExpired && !_options.AutoArchiveAfterExpiredDays.HasValue)
        {
            return;
        }

        // Only archive when AutoArchiveAfterExpiredDays is set (already checked)
        org.Status = OrganizationStatuses.Archived;
        await _db.SaveChangesAsync(ct);
        await _audit.RecordAsync(
            org.Id, null, "system:trial-lifecycle", "Organization", org.Id.ToString(),
            "AutoArchive", null, OrganizationStatuses.Archived,
            "Auto-archive after expired trial policy", correlationId, ct);
    }

    private async Task<bool> TryNotifyAsync(
        LicenseKey key,
        string milestone,
        string correlationId,
        CancellationToken ct,
        bool internalOnly = false)
    {
        var already = await _db.TrialLifecycleNotificationLogs.AsNoTracking()
            .AnyAsync(x => x.LicenseKeyId == key.Id && x.Milestone == milestone, ct);
        if (already)
        {
            return false;
        }

        var org = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == key.OrganizationId, ct);
        var admin = await _db.Members.AsNoTracking()
            .Where(m => m.OrganizationId == key.OrganizationId && !m.IsDeleted && m.Email != null)
            .OrderByDescending(m => m.Role)
            .Select(m => new { m.Email, m.FirstName })
            .FirstOrDefaultAsync(ct);

        var to = admin?.Email;
        if (internalOnly)
        {
            // Record milestone for idempotency even when only internal emails fire
            if (_options.InternalAlertEmails.Count == 0)
            {
                return false;
            }

            await MarkSentAsync(key, milestone, correlationId, string.Join(",", _options.InternalAlertEmails), ct);
            return true;
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("No admin email for org {Org} milestone {Milestone}", key.OrganizationId, milestone);
            return false;
        }

        var daysLeft = key.ExpiresAtUtc.HasValue
            ? Math.Max(0, (int)Math.Ceiling((key.ExpiresAtUtc.Value - DateTime.UtcNow).TotalDays))
            : 0;

        var (subject, html) = BuildCustomerEmail(
            milestone,
            admin?.FirstName,
            org?.Name ?? "your organization",
            daysLeft,
            key.Status.ToString());

        try
        {
            await _email.SendAsync(to, subject, html);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trial lifecycle email failed for {Milestone} org {Org}", milestone, key.OrganizationId);
            return false;
        }

        await MarkSentAsync(key, milestone, correlationId, to, ct);
        return true;
    }

    private async Task MarkSentAsync(LicenseKey key, string milestone, string correlationId, string? recipient, CancellationToken ct)
    {
        _db.TrialLifecycleNotificationLogs.Add(new TrialLifecycleNotificationLog
        {
            Id = Guid.NewGuid(),
            LicenseKeyId = key.Id,
            OrganizationId = key.OrganizationId,
            Milestone = milestone,
            SentAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId,
            Recipient = recipient
        });
        await _db.SaveChangesAsync(ct);
    }

    private (string Subject, string Html) BuildCustomerEmail(
        string milestone,
        string? displayName,
        string orgName,
        int daysLeft,
        string licenseStatus)
        => milestone switch
        {
            TrialLifecycleMilestones.GraceStarted => IdentityEmailTemplates.TrialGraceStarted(
                displayName, orgName, licenseStatus, _options.UpgradeInfoUrl, _options.SupportEmail, _options.DocsBaseUrl),
            TrialLifecycleMilestones.GraceEnding => IdentityEmailTemplates.TrialGraceEnding(
                displayName, orgName, _options.UpgradeInfoUrl, _options.SupportEmail),
            TrialLifecycleMilestones.TrialExpired or TrialLifecycleMilestones.ReadOnlyEnabled =>
                IdentityEmailTemplates.TrialExpiredReadOnly(
                    displayName, orgName, _options.UpgradeInfoUrl, _options.SupportEmail, _options.DocsBaseUrl),
            _ => IdentityEmailTemplates.TrialReminder(
                displayName, orgName, daysLeft, licenseStatus, _options.UpgradeInfoUrl, _options.SupportEmail, _options.DocsBaseUrl)
        };

    private async Task EmitOutboxAsync(LicenseKey key, string eventType, string correlationId, CancellationToken ct)
    {
        var daysLeft = key.ExpiresAtUtc.HasValue
            ? Math.Max(0, (int)Math.Ceiling((key.ExpiresAtUtc.Value - DateTime.UtcNow).TotalDays))
            : 0;
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = key.Id,
            OccurredOn = DateTime.UtcNow,
            Type = eventType.StartsWith("Trial", StringComparison.Ordinal) || eventType.StartsWith("Grace", StringComparison.Ordinal) || eventType.StartsWith("ReadOnly", StringComparison.Ordinal)
                ? eventType
                : eventType,
            Payload = JsonSerializer.Serialize(new
            {
                organizationId = key.OrganizationId,
                licenseKeyId = key.Id,
                daysLeft,
                status = key.Status.ToString(),
                correlationId
            })
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task TryInternalAlertAsync(LicenseKey key, string title, CancellationToken ct)
    {
        if (_options.InternalAlertEmails.Count == 0)
        {
            return;
        }

        var link = $"{_options.CommercialAdminBaseUrl?.TrimEnd('/')}/support/{key.OrganizationId}";
        var (subject, html) = IdentityEmailTemplates.TrialInternalAlert(
            title,
            key.OrganizationId.ToString(),
            key.Status.ToString(),
            link);

        foreach (var addr in _options.InternalAlertEmails.Where(a => !string.IsNullOrWhiteSpace(a)))
        {
            try
            {
                await _email.SendAsync(addr.Trim(), subject, html);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Internal trial alert failed to {Addr}", addr);
            }
        }
    }
}
