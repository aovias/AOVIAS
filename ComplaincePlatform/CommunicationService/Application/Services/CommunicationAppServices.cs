using CommunicationService.Application.Contracts;
using CommunicationService.Domain.Entities;
using CommunicationService.Domain.Enums;
using CommunicationService.Infrastructure.Clients;
using CommunicationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Email;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CommunicationService.Application.Services;

public sealed class EmailChannelProvider(IEmailService email) : ICommunicationChannelProvider
{
    public CommunicationChannelType Channel => CommunicationChannelType.Email;
    public async Task<DeliveryAttemptResult> SendAsync(DeliveryContext context, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(context.Email))
            return new DeliveryAttemptResult { Success = false, ProviderResponse = "No email address", LatencyMs = 0 };
        var sw = Stopwatch.StartNew();
        await email.SendAsync(context.Email, context.Subject, context.Body);
        sw.Stop();
        return new DeliveryAttemptResult { Success = true, ProviderResponse = "Sent", LatencyMs = (int)sw.ElapsedMilliseconds };
    }
}

public sealed class InAppChannelProvider(CommunicationDbContext db, IGatewayRealtimeClient realtime) : ICommunicationChannelProvider
{
    public CommunicationChannelType Channel => CommunicationChannelType.InApp;
    public async Task<DeliveryAttemptResult> SendAsync(DeliveryContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var notificationId = Guid.NewGuid();
        db.InAppNotifications.Add(new InAppNotification
        {
            Id = notificationId,
            PersonId = context.PersonId,
            OutboundMessageId = context.OutboundMessageId,
            Title = context.Subject,
            Body = context.Body,
            ModuleCode = context.ModuleCode,
            CreatedAt = DateTime.UtcNow
        });
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = notificationId,
            OccurredOn = DateTime.UtcNow,
            Type = "InAppNotificationCreated",
            Payload = JsonSerializer.Serialize(new
            {
                notificationId,
                context.PersonId,
                title = context.Subject,
                body = context.Body,
                moduleCode = context.ModuleCode
            })
        });
        await db.SaveChangesAsync(ct);
        await realtime.PublishInAppCreatedAsync(
            context.PersonId, notificationId, context.Subject, context.Body, context.ModuleCode, ct);
        sw.Stop();
        return new DeliveryAttemptResult { Success = true, ProviderResponse = "InAppCreated", LatencyMs = (int)sw.ElapsedMilliseconds };
    }
}

public sealed class StubChannelProvider(CommunicationChannelType channel) : ICommunicationChannelProvider
{
    public CommunicationChannelType Channel => channel;
    public Task<DeliveryAttemptResult> SendAsync(DeliveryContext context, CancellationToken ct = default)
        => Task.FromResult(new DeliveryAttemptResult { Success = true, ProviderResponse = $"{channel} stub", LatencyMs = 0 });
}

public sealed class AvailabilityEngine(CommunicationDbContext db)
{
    public async Task<bool> CanDeliverNowAsync(Guid personId, bool critical, CancellationToken ct = default)
    {
        if (critical) return true;
        var profile = await db.AvailabilityProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.PersonId == personId, ct);
        if (profile == null) return true;
        if (profile.Status is AvailabilityStatus.Vacation or AvailabilityStatus.DoNotDisturb) return false;
        var now = DateTime.UtcNow;
        var vacations = await db.VacationEntries.AsNoTracking().Where(v => v.AvailabilityProfileId == profile.Id).ToListAsync(ct);
        if (vacations.Any(v => now.Date >= v.StartDate.Date && now.Date <= v.EndDate.Date)) return false;
        return true;
    }

    public async Task<bool> IsQuietHoursAsync(Guid personId, CancellationToken ct = default)
    {
        var pref = await db.NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId && p.QuietHoursStart != null, ct);
        if (pref?.QuietHoursStart == null || pref.QuietHoursEnd == null) return false;
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        if (pref.QuietHoursStart <= pref.QuietHoursEnd)
            return now >= pref.QuietHoursStart && now <= pref.QuietHoursEnd;
        return now >= pref.QuietHoursStart || now <= pref.QuietHoursEnd;
    }
}

public sealed class TemplateRenderer(CommunicationDbContext db)
{
    public async Task<(string subject, string body)?> RenderAsync(string templateCode, Dictionary<string, string>? variables, string locale = "en-IN", CancellationToken ct = default)
    {
        var template = await db.CommunicationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Code == templateCode && !t.IsDeleted, ct);
        if (template == null) return null;
        var version = await db.TemplateVersions.AsNoTracking()
            .Where(v => v.TemplateId == template.Id && v.Locale == locale)
            .OrderByDescending(v => v.Version).FirstOrDefaultAsync(ct)
            ?? await db.TemplateVersions.AsNoTracking().Where(v => v.TemplateId == template.Id).OrderByDescending(v => v.Version).FirstOrDefaultAsync(ct);
        if (version == null) return null;
        var subject = ApplyVariables(version.Subject, variables);
        var body = ApplyVariables(version.Body, variables);
        return (subject, body);
    }

    private static string ApplyVariables(string input, Dictionary<string, string>? variables)
    {
        if (variables == null) return input;
        foreach (var (key, value) in variables)
            input = input.Replace("{{" + key + "}}", value, StringComparison.OrdinalIgnoreCase);
        return Regex.Replace(input, @"\{\{[^}]+\}\}", string.Empty);
    }
}

public sealed class DeliveryOrchestrator(
    CommunicationDbContext db,
    IEnumerable<ICommunicationChannelProvider> providers,
    IPersonContactClient contacts,
    AvailabilityEngine availability,
    TemplateRenderer renderer)
{
    public async Task DeliverAsync(OutboundMessage message, IReadOnlyList<CommunicationChannelType> channels, string? overrideEmail, string? overridePhone, CancellationToken ct = default)
    {
        var contact = message.PersonId != Guid.Empty ? await contacts.GetPrimaryContactAsync(message.PersonId, ct) : null;
        var (subject, body) = message.Subject.Length > 0
            ? (message.Subject, message.Body)
            : message.TemplateCode != null
                ? await renderer.RenderAsync(message.TemplateCode, JsonSerializer.Deserialize<Dictionary<string, string>>(message.PayloadJson), ct: ct) ?? (message.EventType, message.Body)
                : (message.EventType, message.Body);

        var ctx = new DeliveryContext
        {
            OutboundMessageId = message.Id,
            PersonId = message.PersonId,
            Subject = subject,
            Body = body,
            ModuleCode = message.ModuleCode,
            Email = overrideEmail ?? contact?.Email,
            Phone = overridePhone ?? contact?.Phone
        };

        var providerMap = providers.ToDictionary(p => p.Channel);
        foreach (var channel in channels)
        {
            if (!providerMap.TryGetValue(channel, out var provider)) continue;
            var record = new DeliveryRecord
            {
                Id = Guid.NewGuid(),
                OutboundMessageId = message.Id,
                Channel = channel,
                Status = DeliveryStatus.Queued,
                CreatedAt = DateTime.UtcNow
            };
            db.DeliveryRecords.Add(record);
            var result = await provider.SendAsync(ctx, ct);
            record.Status = result.Success ? DeliveryStatus.Delivered : DeliveryStatus.Failed;
            if (!result.Success)
                record.RetryCount += 1;
            record.ProviderResponse = result.ProviderResponse;
            record.LatencyMs = result.LatencyMs;
            record.CompletedAt = DateTime.UtcNow;
            if (result.Success) break;
        }

        message.Status = OutboundMessageStatus.Completed;
        await AppendTimelineAsync(message.PersonId, "Delivery", "NotificationDelivered", $"Delivered {message.EventType}", ct);
        await WriteOutboxAsync("NotificationDelivered", message.Id, new { message.Id, message.PersonId }, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task AppendTimelineAsync(Guid? personId, string source, string eventType, string summary, CancellationToken ct)
    {
        db.CommunicationTimeline.Add(new CommunicationTimelineEntry
        {
            Id = Guid.NewGuid(), PersonId = personId, Source = source, EventType = eventType, Summary = summary, OccurredAt = DateTime.UtcNow
        });
        await Task.CompletedTask;
    }

    private Task WriteOutboxAsync(string type, Guid aggregateId, object payload, CancellationToken ct)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(), AggregateId = aggregateId, OccurredOn = DateTime.UtcNow, Type = type, Payload = JsonSerializer.Serialize(payload)
        });
        return Task.CompletedTask;
    }
}

public sealed class CommunicationRequestService(
    CommunicationDbContext db,
    DeliveryOrchestrator orchestrator,
    AvailabilityEngine availability,
    TemplateRenderer renderer) : ICommunicationRequestService
{
    public async Task<SendCommunicationResult> SendAsync(SendCommunicationRequest request, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.DedupeKey))
        {
            var existing = await db.OutboundMessages.AsNoTracking().FirstOrDefaultAsync(m => m.DedupeKey == request.DedupeKey, ct);
            if (existing != null)
                return new SendCommunicationResult { OutboundMessageId = existing.Id, Status = existing.Status.ToString() };
        }

        var personId = request.PersonId ?? Guid.Empty;
        if (personId == Guid.Empty && string.IsNullOrWhiteSpace(request.RecipientEmail))
            throw new InvalidOperationException("PersonId or RecipientEmail is required.");
        var email = request.RecipientEmail;
        var phone = request.RecipientPhone;

        var canDeliver = personId == Guid.Empty || await availability.CanDeliverNowAsync(personId, request.Critical, ct);
        var quiet = !request.Critical && personId != Guid.Empty && await availability.IsQuietHoursAsync(personId, ct);
        var digest = !request.Critical && personId != Guid.Empty && await IsDigestPreferredAsync(personId, request.ModuleCode, ct);

        string subject = request.Subject ?? request.EventType;
        string body = request.Body ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(request.TemplateCode))
        {
            var rendered = await renderer.RenderAsync(request.TemplateCode, request.Variables, ct: ct);
            if (rendered != null) { subject = rendered.Value.subject; body = rendered.Value.body; }
        }

        var defer = !canDeliver || quiet || digest;
        var message = new OutboundMessage
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ModuleCode = request.ModuleCode,
            EventType = request.EventType,
            TemplateCode = request.TemplateCode,
            Subject = subject,
            Body = body,
            PayloadJson = JsonSerializer.Serialize(request.Variables ?? new Dictionary<string, string>()),
            Critical = request.Critical,
            DedupeKey = request.DedupeKey,
            Status = defer ? OutboundMessageStatus.Deferred : OutboundMessageStatus.Pending,
            ScheduledFor = defer
                ? (digest ? DateTime.UtcNow.Date.AddDays(1).AddHours(8) : DateTime.UtcNow.AddHours(1))
                : null,
            CreatedAt = DateTime.UtcNow
        };
        db.OutboundMessages.Add(message);
        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = message.Id, OccurredOn = DateTime.UtcNow, Type = "NotificationQueued", Payload = JsonSerializer.Serialize(new { message.Id }) });
        await db.SaveChangesAsync(ct);

        if (message.Status == OutboundMessageStatus.Pending)
        {
            var channels = await ResolveChannelsAsync(request, personId, ct);
            if (channels.Count > 0)
                await orchestrator.DeliverAsync(message, channels, email, phone, ct);
            else
            {
                message.Status = OutboundMessageStatus.Completed;
                await db.SaveChangesAsync(ct);
            }
        }

        return new SendCommunicationResult { OutboundMessageId = message.Id, Status = message.Status.ToString() };
    }

    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        var pending = await db.OutboundMessages
            .Where(m => m.Status == OutboundMessageStatus.Pending
                || (m.Status == OutboundMessageStatus.Deferred
                    && m.ScheduledFor <= DateTime.UtcNow
                    && !m.Critical))
            .OrderBy(m => m.CreatedAt).Take(50).ToListAsync(ct);
        foreach (var msg in pending)
        {
            if (await IsDigestPreferredAsync(msg.PersonId, msg.ModuleCode, ct) && !msg.Critical)
                continue; // DigestNotificationHostedService owns these.
            if (!await availability.CanDeliverNowAsync(msg.PersonId, msg.Critical, ct) && !msg.Critical) continue;
            if (!msg.Critical && await availability.IsQuietHoursAsync(msg.PersonId, ct)) continue;
            msg.Status = OutboundMessageStatus.Pending;
            var channels = await ResolveChannelsAsync(
                new SendCommunicationRequest { ModuleCode = msg.ModuleCode, EventType = msg.EventType },
                msg.PersonId,
                ct);
            if (channels.Count == 0)
            {
                msg.Status = OutboundMessageStatus.Completed;
                await db.SaveChangesAsync(ct);
                continue;
            }
            await orchestrator.DeliverAsync(msg, channels, null, null, ct);
        }
    }

    public async Task ProcessDigestAsync(CancellationToken ct = default)
    {
        var deferred = await db.OutboundMessages
            .Where(m => m.Status == OutboundMessageStatus.Deferred && !m.Critical && m.ScheduledFor <= DateTime.UtcNow)
            .OrderBy(m => m.CreatedAt)
            .Take(200)
            .ToListAsync(ct);

        foreach (var group in deferred.GroupBy(m => m.PersonId))
        {
            if (!await IsDigestPreferredAsync(group.Key, null, ct))
                continue;
            if (!await availability.CanDeliverNowAsync(group.Key, critical: false, ct))
                continue;
            if (await availability.IsQuietHoursAsync(group.Key, ct))
                continue;

            var items = group.Take(20).ToList();
            var summary = string.Join("<br/>", items.Select(m => $"• {m.Subject}"));
            var digestMessage = new OutboundMessage
            {
                Id = Guid.NewGuid(),
                PersonId = group.Key,
                ModuleCode = "Communication",
                EventType = "DigestSummary",
                Subject = $"You have {items.Count} notification(s)",
                Body = summary,
                PayloadJson = "{}",
                Status = OutboundMessageStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.OutboundMessages.Add(digestMessage);
            await db.SaveChangesAsync(ct);

            var channels = await ResolveChannelsAsync(
                new SendCommunicationRequest { ModuleCode = "Communication", EventType = "DigestSummary", PreferredChannels = ["InApp", "Email"] },
                group.Key,
                ct);
            if (channels.Count > 0)
                await orchestrator.DeliverAsync(digestMessage, channels, null, null, ct);

            foreach (var msg in items)
                msg.Status = OutboundMessageStatus.Completed;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<bool> IsDigestPreferredAsync(Guid personId, string? moduleCode, CancellationToken ct)
    {
        var prefs = await db.NotificationPreferences.AsNoTracking()
            .Where(p => p.PersonId == personId && p.Enabled)
            .ToListAsync(ct);
        if (prefs.Count == 0) return false;
        var match = prefs.FirstOrDefault(p => moduleCode != null && p.ModuleCode == moduleCode)
            ?? prefs.FirstOrDefault(p => p.ModuleCode == null)
            ?? prefs[0];
        return match.Frequency is NotificationFrequency.Digest
            or NotificationFrequency.DailySummary
            or NotificationFrequency.WeeklySummary;
    }

    private async Task<IReadOnlyList<CommunicationChannelType>> ResolveChannelsAsync(
        SendCommunicationRequest request,
        Guid personId,
        CancellationToken ct)
    {
        IReadOnlyList<CommunicationChannelType> preferred = request.PreferredChannels?.Count > 0
            ? request.PreferredChannels.Select(c => Enum.Parse<CommunicationChannelType>(c, true)).ToList()
            : [CommunicationChannelType.InApp, CommunicationChannelType.Email];

        if (personId == Guid.Empty)
            return preferred;

        var prefs = await db.NotificationPreferences.AsNoTracking()
            .Where(p => p.PersonId == personId)
            .ToListAsync(ct);
        if (prefs.Count == 0)
            return preferred;

        var modulePrefs = prefs.Where(p => p.ModuleCode == request.ModuleCode || p.ModuleCode == null).ToList();
        var allowed = new List<CommunicationChannelType>();
        foreach (var channel in preferred)
        {
            var pref = modulePrefs.FirstOrDefault(p => p.Channel == channel)
                ?? prefs.FirstOrDefault(p => p.Channel == channel);
            if (pref == null)
            {
                allowed.Add(channel);
                continue;
            }

            if (!pref.Enabled && !(request.Critical && pref.EscalationOverride))
                continue;
            if (pref.Frequency == NotificationFrequency.CriticalOnly && !request.Critical)
                continue;
            if (pref.Frequency == NotificationFrequency.NoMarketing && !request.Critical)
                continue;
            allowed.Add(channel);
        }

        return allowed;
    }
}

public sealed class NotificationInboxService(CommunicationDbContext db) : INotificationInboxService
{
    public async Task<NotificationListResponse> ListAsync(Guid personId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.InAppNotifications.AsNoTracking().Where(n => n.PersonId == personId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(n => n.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationDto { Id = n.Id, Title = n.Title, Body = n.Body, ModuleCode = n.ModuleCode, IsRead = n.IsRead, CreatedAt = n.CreatedAt })
            .ToListAsync(ct);
        return new NotificationListResponse { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<int> UnreadCountAsync(Guid personId, CancellationToken ct = default)
        => db.InAppNotifications.CountAsync(n => n.PersonId == personId && !n.IsRead, ct);

    public async Task MarkReadAsync(Guid personId, Guid notificationId, CancellationToken ct = default)
    {
        var n = await db.InAppNotifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.PersonId == personId, ct);
        if (n == null) return;
        n.IsRead = true;
        n.ReadAt = DateTime.UtcNow;
        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = n.Id, OccurredOn = DateTime.UtcNow, Type = "NotificationRead", Payload = "{}" });
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(Guid personId, CancellationToken ct = default)
    {
        var unread = await db.InAppNotifications.Where(n => n.PersonId == personId && !n.IsRead).ToListAsync(ct);
        if (unread.Count == 0) return;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = personId, OccurredOn = DateTime.UtcNow, Type = "NotificationsAllRead", Payload = "{}" });
        await db.SaveChangesAsync(ct);
    }
}

public sealed class PreferenceService(CommunicationDbContext db) : IPreferenceService
{
    public async Task<IReadOnlyList<PreferenceDto>> GetAsync(Guid personId, CancellationToken ct = default)
        => await db.NotificationPreferences.AsNoTracking().Where(p => p.PersonId == personId)
            .Select(p => new PreferenceDto
            {
                Id = p.Id, ModuleCode = p.ModuleCode, Channel = p.Channel.ToString(), Frequency = p.Frequency.ToString(),
                Enabled = p.Enabled, EscalationOverride = p.EscalationOverride, NoMarketing = p.NoMarketing,
                QuietHoursStart = p.QuietHoursStart.HasValue ? p.QuietHoursStart.Value.ToString("HH:mm") : null,
                QuietHoursEnd = p.QuietHoursEnd.HasValue ? p.QuietHoursEnd.Value.ToString("HH:mm") : null
            }).ToListAsync(ct);

    public async Task<PreferenceDto> UpsertAsync(Guid personId, UpdatePreferenceRequest request, CancellationToken ct = default)
    {
        var channel = Enum.Parse<CommunicationChannelType>(request.Channel, true);
        var existing = await db.NotificationPreferences.FirstOrDefaultAsync(p => p.PersonId == personId && p.ModuleCode == request.ModuleCode && p.Channel == channel, ct);
        if (existing == null)
        {
            existing = new NotificationPreference { Id = Guid.NewGuid(), PersonId = personId, CreatedAt = DateTime.UtcNow };
            db.NotificationPreferences.Add(existing);
        }
        existing.ModuleCode = request.ModuleCode;
        existing.Channel = channel;
        existing.Frequency = Enum.Parse<NotificationFrequency>(request.Frequency, true);
        existing.Enabled = request.Enabled;
        existing.EscalationOverride = request.EscalationOverride;
        existing.NoMarketing = request.NoMarketing;
        existing.QuietHoursStart = TimeOnly.TryParse(request.QuietHoursStart, out var qs) ? qs : null;
        existing.QuietHoursEnd = TimeOnly.TryParse(request.QuietHoursEnd, out var qe) ? qe : null;
        existing.UpdatedAt = DateTime.UtcNow;
        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = existing.Id, OccurredOn = DateTime.UtcNow, Type = "PreferenceUpdated", Payload = "{}" });
        await db.SaveChangesAsync(ct);
        return (await GetAsync(personId, ct)).First(p => p.Id == existing.Id);
    }
}

public sealed class TemplateService(CommunicationDbContext db) : ITemplateService
{
    public async Task<IReadOnlyList<TemplateDto>> ListAsync(CancellationToken ct = default)
        => await db.CommunicationTemplates.AsNoTracking().Where(t => !t.IsDeleted)
            .Select(t => new TemplateDto { Id = t.Id, Code = t.Code, Name = t.Name, Channel = t.Channel.ToString(), ModuleCode = t.ModuleCode, Status = t.Status.ToString(), CurrentVersion = t.CurrentVersion })
            .ToListAsync(ct);

    public async Task<TemplateDto> UpsertAsync(UpsertTemplateRequest request, CancellationToken ct = default)
    {
        var template = await db.CommunicationTemplates.FirstOrDefaultAsync(t => t.Code == request.Code, ct);
        if (template == null)
        {
            template = new CommunicationTemplate { Id = Guid.NewGuid(), Code = request.Code, CreatedAt = DateTime.UtcNow };
            db.CommunicationTemplates.Add(template);
        }
        template.Name = request.Name;
        template.Channel = Enum.Parse<CommunicationChannelType>(request.Channel, true);
        template.ModuleCode = request.ModuleCode;
        template.UpdatedAt = DateTime.UtcNow;
        if (request.Publish)
        {
            template.CurrentVersion++;
            template.Status = TemplateStatus.Published;
            db.TemplateVersions.Add(new TemplateVersion
            {
                Id = Guid.NewGuid(), TemplateId = template.Id, Version = template.CurrentVersion,
                Locale = request.Locale, Subject = request.Subject, Body = request.Body, PublishedAt = DateTime.UtcNow
            });
            db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = template.Id, OccurredOn = DateTime.UtcNow, Type = "TemplatePublished", Payload = "{}" });
        }
        await db.SaveChangesAsync(ct);
        return (await ListAsync(ct)).First(t => t.Id == template.Id);
    }
}

public sealed class CommunicationTimelineService(CommunicationDbContext db) : ICommunicationTimelineService
{
    public async Task<IReadOnlyList<TimelineDto>> GetAsync(Guid? personId, int take, CancellationToken ct = default)
    {
        var q = db.CommunicationTimeline.AsNoTracking();
        if (personId.HasValue) q = q.Where(t => t.PersonId == personId);
        return await q.OrderByDescending(t => t.OccurredAt).Take(take)
            .Select(t => new TimelineDto { Id = t.Id, Source = t.Source, EventType = t.EventType, Summary = t.Summary, OccurredAt = t.OccurredAt })
            .ToListAsync(ct);
    }
}

public sealed class DeliveryQueryService(CommunicationDbContext db) : IDeliveryQueryService
{
    public async Task<DeliveryStatusDto?> GetAsync(Guid deliveryId, CancellationToken ct = default)
    {
        var r = await db.DeliveryRecords.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deliveryId, ct);
        return r == null ? null : new DeliveryStatusDto { Id = r.Id, OutboundMessageId = r.OutboundMessageId, Channel = r.Channel.ToString(), Status = r.Status.ToString(), RetryCount = r.RetryCount, LatencyMs = r.LatencyMs, CreatedAt = r.CreatedAt };
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return new DashboardDto
        {
            PendingMessages = await db.OutboundMessages.CountAsync(m => m.Status == OutboundMessageStatus.Pending || m.Status == OutboundMessageStatus.Deferred, ct),
            DeliveredToday = await db.DeliveryRecords.CountAsync(d => d.Status == DeliveryStatus.Delivered && d.CompletedAt >= today, ct),
            FailedToday = await db.DeliveryRecords.CountAsync(d => d.Status == DeliveryStatus.Failed && d.CreatedAt >= today, ct),
            UnreadInApp = await db.InAppNotifications.CountAsync(n => !n.IsRead, ct)
        };
    }
}

public sealed class ReminderRuleService(CommunicationDbContext db) : IReminderRuleService
{
    public async Task<IReadOnlyList<ReminderRuleDto>> ListAsync(CancellationToken ct = default)
        => await db.ReminderRules.AsNoTracking().Where(r => !r.IsDeleted)
            .Select(r => new ReminderRuleDto { Id = r.Id, Name = r.Name, RuleType = r.RuleType.ToString(), ModuleCode = r.ModuleCode, TemplateCode = r.TemplateCode, Enabled = r.Enabled })
            .ToListAsync(ct);

    public async Task<ReminderRuleDto> CreateAsync(CreateReminderRuleRequest request, CancellationToken ct = default)
    {
        var rule = new ReminderRule
        {
            Id = Guid.NewGuid(), Name = request.Name, RuleType = Enum.Parse<ReminderRuleType>(request.RuleType, true),
            ModuleCode = request.ModuleCode, TemplateCode = request.TemplateCode, CronExpression = request.CronExpression,
            EventType = request.EventType, CreatedAt = DateTime.UtcNow
        };
        db.ReminderRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return (await ListAsync(ct)).First(r => r.Id == rule.Id);
    }
}

public sealed class CampaignService(CommunicationDbContext db, ICommunicationRequestService sender) : ICampaignService
{
    public async Task<IReadOnlyList<CampaignDto>> ListAsync(CancellationToken ct = default)
        => await db.CommunicationCampaigns.AsNoTracking()
            .Select(c => new CampaignDto { Id = c.Id, Name = c.Name, Status = c.Status, LaunchedAt = c.LaunchedAt })
            .ToListAsync(ct);

    public async Task<CampaignDto> CreateAsync(CreateCampaignRequest request, CancellationToken ct = default)
    {
        var campaign = new CommunicationCampaign { Id = Guid.NewGuid(), Name = request.Name, TemplateId = request.TemplateId, AudienceFilterJson = request.AudienceFilterJson, CreatedAt = DateTime.UtcNow };
        db.CommunicationCampaigns.Add(campaign);
        await db.SaveChangesAsync(ct);
        return (await ListAsync(ct)).First(c => c.Id == campaign.Id);
    }

    public async Task LaunchAsync(Guid campaignId, CancellationToken ct = default)
    {
        var campaign = await db.CommunicationCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct) ?? throw new InvalidOperationException("Campaign not found");
        campaign.Status = "Launched";
        campaign.LaunchedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AvailabilityService(CommunicationDbContext db) : IAvailabilityService
{
    public async Task<AvailabilityDto?> GetAsync(Guid personId, CancellationToken ct = default)
    {
        var profile = await db.AvailabilityProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.PersonId == personId, ct);
        if (profile == null) return null;
        var hours = await db.WorkingHours.AsNoTracking().Where(w => w.AvailabilityProfileId == profile.Id)
            .Select(w => new WorkingHoursDto { DayOfWeek = w.DayOfWeek, StartTime = w.StartTime.ToString("HH:mm"), EndTime = w.EndTime.ToString("HH:mm") }).ToListAsync(ct);
        var vacations = await db.VacationEntries.AsNoTracking().Where(v => v.AvailabilityProfileId == profile.Id)
            .Select(v => new VacationDto { StartDate = v.StartDate, EndDate = v.EndDate, Reason = v.Reason }).ToListAsync(ct);
        return new AvailabilityDto { PersonId = personId, TimeZone = profile.TimeZone, Status = profile.Status.ToString(), WorkingHours = hours, Vacations = vacations };
    }

    public async Task<AvailabilityDto> UpsertAsync(Guid personId, UpdateAvailabilityRequest request, CancellationToken ct = default)
    {
        var profile = await db.AvailabilityProfiles.FirstOrDefaultAsync(p => p.PersonId == personId, ct);
        if (profile == null)
        {
            profile = new AvailabilityProfile { Id = Guid.NewGuid(), PersonId = personId, CreatedAt = DateTime.UtcNow };
            db.AvailabilityProfiles.Add(profile);
        }
        profile.TimeZone = request.TimeZone;
        profile.Status = Enum.Parse<AvailabilityStatus>(request.Status, true);
        profile.UpdatedAt = DateTime.UtcNow;
        var existingHours = await db.WorkingHours.Where(w => w.AvailabilityProfileId == profile.Id).ToListAsync(ct);
        db.WorkingHours.RemoveRange(existingHours);
        if (request.WorkingHours != null)
        {
            foreach (var h in request.WorkingHours)
            {
                db.WorkingHours.Add(new WorkingHours
                {
                    Id = Guid.NewGuid(), AvailabilityProfileId = profile.Id, DayOfWeek = h.DayOfWeek,
                    StartTime = TimeOnly.Parse(h.StartTime), EndTime = TimeOnly.Parse(h.EndTime)
                });
            }
        }
        var existingVac = await db.VacationEntries.Where(v => v.AvailabilityProfileId == profile.Id).ToListAsync(ct);
        db.VacationEntries.RemoveRange(existingVac);
        if (request.Vacations != null)
        {
            foreach (var v in request.Vacations)
            {
                db.VacationEntries.Add(new VacationEntry { Id = Guid.NewGuid(), AvailabilityProfileId = profile.Id, StartDate = v.StartDate, EndDate = v.EndDate, Reason = v.Reason });
            }
        }
        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = profile.Id, OccurredOn = DateTime.UtcNow, Type = "AvailabilityChanged", Payload = "{}" });
        await db.SaveChangesAsync(ct);
        return (await GetAsync(personId, ct))!;
    }
}

public sealed class EventSubscriptionProcessor(CommunicationDbContext db, ICommunicationRequestService sender) : IEventSubscriptionProcessor
{
    public async Task ProcessEventAsync(string sourceEventType, string moduleCode, Guid personId, Dictionary<string, string>? variables, bool critical, CancellationToken ct = default)
    {
        var rule = await db.EventSubscriptionRules.AsNoTracking().FirstOrDefaultAsync(r => r.Enabled && r.SourceEventType == sourceEventType, ct);
        if (rule == null) return;
        var escalation = await db.EscalationRules.AsNoTracking().FirstOrDefaultAsync(e => e.Enabled && e.SourceEventType == sourceEventType, ct);
        var templateCode = escalation?.TemplateCode ?? rule.TemplateCode;
        await sender.SendAsync(new SendCommunicationRequest
        {
            ModuleCode = moduleCode,
            EventType = sourceEventType,
            PersonId = personId,
            TemplateCode = templateCode,
            Variables = variables,
            Critical = critical,
            DedupeKey = $"{sourceEventType}:{personId}:{variables?.GetValueOrDefault("resourceId")}"
        }, ct);
    }
}
