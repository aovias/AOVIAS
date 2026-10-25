using CommunicationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunicationService.Infrastructure.Background;

public sealed class CommunicationOutboxProcessor(IServiceProvider services, ILogger<CommunicationOutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
                var pending = await db.OutboxMessages.Where(x => x.ProcessedOn == null).Take(100).ToListAsync(stoppingToken);
                foreach (var msg in pending)
                {
                    logger.LogInformation("Communication outbox {Type} aggregate={AggregateId}", msg.Type, msg.AggregateId);
                    msg.ProcessedOn = DateTime.UtcNow;
                }
                if (pending.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) { logger.LogError(ex, "Communication outbox processor failed"); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

public sealed class OutboundMessageProcessor(IServiceProvider services, ILogger<OutboundMessageProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<Application.Contracts.ICommunicationRequestService>();
                await sender.ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) { logger.LogError(ex, "Outbound message processor failed"); }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}

public sealed class ReminderSchedulerHostedService(IServiceProvider services, ILogger<ReminderSchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
                var sender = scope.ServiceProvider.GetRequiredService<Application.Contracts.ICommunicationRequestService>();
                var rules = await db.ReminderRules.Where(r => r.Enabled && !r.IsDeleted && r.RuleType == Domain.Enums.ReminderRuleType.TimeBased).ToListAsync(stoppingToken);
                foreach (var rule in rules)
                {
                    logger.LogDebug("Reminder rule tick {Name}", rule.Name);
                    db.OutboxMessages.Add(new Domain.Entities.OutboxMessage
                    {
                        Id = Guid.NewGuid(), AggregateId = rule.Id, OccurredOn = DateTime.UtcNow, Type = "ReminderTriggered", Payload = $"{{\"ruleId\":\"{rule.Id}\"}}"
                    });
                }
                if (rules.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) { logger.LogError(ex, "Reminder scheduler failed"); }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

public sealed class ComplianceEventConsumerHostedService(
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<ComplianceEventConsumerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Dev / local: use HTTP enqueue via POST /internal/communication/events or ICommunicationClient.SendAsync.
            logger.LogWarning(
                "ServiceBus:ConnectionString not configured; ComplianceEventConsumer disabled. " +
                "Use HTTP enqueue (POST /internal/communication/events) or module ICommunicationClient in Dev.");
            return;
        }

        await using var client = new Azure.Messaging.ServiceBus.ServiceBusClient(connectionString);
        await using var processor = client.CreateProcessor("compliance-events", new Azure.Messaging.ServiceBus.ServiceBusProcessorOptions { AutoCompleteMessages = false });
        processor.ProcessMessageAsync += async args =>
        {
            try
            {
                var subject = args.Message.Subject ?? string.Empty;
                if (!subject.StartsWith("ComplianceEvent.", StringComparison.OrdinalIgnoreCase))
                {
                    await args.CompleteMessageAsync(args.Message, stoppingToken);
                    return;
                }
                var eventType = subject["ComplianceEvent.".Length..];
                using var scope = services.CreateScope();
                var processorSvc = scope.ServiceProvider.GetRequiredService<Application.Contracts.IEventSubscriptionProcessor>();
                var payload = args.Message.Body.ToString();
                var personId = ExtractPersonId(payload);
                if (personId.HasValue)
                {
                    await processorSvc.ProcessEventAsync(eventType, "Compliance", personId.Value, new Dictionary<string, string> { ["resourceId"] = args.Message.CorrelationId ?? "" }, critical: eventType.Contains("Breached", StringComparison.OrdinalIgnoreCase) || eventType.Contains("Escalated", StringComparison.OrdinalIgnoreCase), stoppingToken);
                }
                await args.CompleteMessageAsync(args.Message, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed processing compliance event");
                await args.AbandonMessageAsync(args.Message, cancellationToken: stoppingToken);
            }
        };
        processor.ProcessErrorAsync += args => { logger.LogError(args.Exception, "ServiceBus error"); return Task.CompletedTask; };
        await processor.StartProcessingAsync(stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static Guid? ExtractPersonId(string payload)
    {
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(payload);
            foreach (var name in new[] { "personId", "targetPersonId", "assigneePersonId", "userId" })
            {
                if (doc.RootElement.TryGetProperty(name, out var prop) && prop.TryGetGuid(out var id))
                    return id;
            }
        }
        catch { }
        return null;
    }
}

/// <summary>
/// Flushes digest / daily-summary deferred outbound messages on a cadence (hosted job; Hangfire optional later).
/// </summary>
public sealed class DigestNotificationHostedService(IServiceProvider services, ILogger<DigestNotificationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<Application.Contracts.ICommunicationRequestService>();
                await sender.ProcessDigestAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Digest notification processor failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}

public sealed class CommunicationBootstrapHostedService(IServiceProvider services, ILogger<CommunicationBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
        if (await db.EventSubscriptionRules.AnyAsync(cancellationToken)) return;

        var rules = new[]
        {
            ("TaskAssigned", "Compliance", "task-assigned"),
            ("OeOwnershipAssigned", "Compliance", "task-assigned"),
            ("DeficiencySlaWarning", "Compliance", "sla-warning"),
            ("DeficiencySlaBreached", "Compliance", "sla-breached"),
            ("DeficiencyEscalated", "Compliance", "deficiency-escalated"),
            ("EvidenceApproved", "Compliance", "evidence-approved"),
            ("EvidenceRejected", "Compliance", "evidence-rejected"),
            ("FrameworkActivated", "Compliance", "framework-activated"),
            ("CapaOverdue", "Compliance", "capa-overdue"),
            ("AuditScheduled", "Compliance", "audit-scheduled"),
            ("EscalationTriggered", "Governance", "governance-escalation"),
            ("InvitationCreated", "Organization", "member-invitation")
        };
        foreach (var (evt, mod, tmpl) in rules)
        {
            db.EventSubscriptionRules.Add(new Domain.Entities.EventSubscriptionRule
            {
                Id = Guid.NewGuid(), SourceEventType = evt, ModuleCode = mod, TemplateCode = tmpl, Enabled = true
            });
        }

        if (!await db.CommunicationTemplates.AnyAsync(cancellationToken))
        {
            var templates = new (string Code, string Name, string Mod, string Subject, string Body)[]
            {
                ("task-assigned", "Task Assigned", "Compliance", "Task assigned: {{taskTitle}}",
                    "<p>Hello {{assigneeName}},</p><p>Task <strong>{{taskTitle}}</strong> is assigned to you.</p><p>Due: {{dueDate}}</p><p>{{message}}</p>"),
                ("capa-overdue", "CAPA Overdue", "Compliance", "CAPA overdue: {{capaNumber}}",
                    "<p>CAPA <strong>{{capaNumber}}</strong> is overdue.</p><p>Owner: {{ownerName}}</p><p>Due: {{dueDate}}</p><p>{{message}}</p>"),
                ("audit-scheduled", "Audit Scheduled", "Compliance", "Audit scheduled: {{auditName}}",
                    "<p>Audit <strong>{{auditName}}</strong> is scheduled for {{scheduledDate}}.</p><p>Location: {{location}}</p><p>{{message}}</p>"),
                ("evidence-approved", "Evidence Approved", "Compliance", "Evidence approved",
                    "<p>Evidence {{evidenceId}} was approved.</p><p>Framework: {{frameworkId}}</p><p>{{message}}</p>"),
                ("evidence-rejected", "Evidence Rejected", "Compliance", "Evidence rejected",
                    "<p>Evidence {{evidenceId}} was rejected.</p><p>Status: {{status}}</p><p>{{message}}</p>"),
                ("framework-activated", "Framework Activated", "Compliance", "Framework activated: {{name}} {{version}}",
                    "<p>Framework <strong>{{name}}</strong> version {{version}} is now active.</p><p>{{message}}</p>"),
                ("sla-warning", "SLA Warning", "Compliance", "SLA Warning", "<p>{{message}}</p>"),
                ("sla-breached", "SLA Breached", "Compliance", "SLA Breached", "<p>{{message}}</p>"),
                ("deficiency-escalated", "Deficiency Escalated", "Compliance", "Deficiency Escalated", "<p>{{message}}</p>"),
                ("governance-escalation", "Governance Escalation", "Governance", "Governance Escalation", "<p>{{message}}</p>"),
                ("member-invitation", "Member Invitation", "Organization", "Member Invitation", "<p>{{message}}</p>"),
                ("ai-approval-requested", "AI Approval Requested", "AI", "AI Approval Requested", "<p>{{message}}</p>"),
            };
            foreach (var (code, name, mod, subject, body) in templates)
            {
                var t = new Domain.Entities.CommunicationTemplate { Id = Guid.NewGuid(), Code = code, Name = name, ModuleCode = mod, Channel = Domain.Enums.CommunicationChannelType.Email, Status = Domain.Enums.TemplateStatus.Published, CurrentVersion = 1, CreatedAt = DateTime.UtcNow };
                db.CommunicationTemplates.Add(t);
                db.TemplateVersions.Add(new Domain.Entities.TemplateVersion { Id = Guid.NewGuid(), TemplateId = t.Id, Version = 1, Locale = "en-IN", Subject = subject, Body = body, PublishedAt = DateTime.UtcNow });
            }
        }

        if (!await db.ChannelConfigurations.AnyAsync(cancellationToken))
        {
            db.ChannelConfigurations.Add(new Domain.Entities.ChannelConfiguration { Id = Guid.NewGuid(), Channel = Domain.Enums.CommunicationChannelType.Email, ProviderType = "Console", Enabled = true, CreatedAt = DateTime.UtcNow });
            db.ChannelConfigurations.Add(new Domain.Entities.ChannelConfiguration { Id = Guid.NewGuid(), Channel = Domain.Enums.CommunicationChannelType.InApp, ProviderType = "InApp", Enabled = true, CreatedAt = DateTime.UtcNow });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Communication bootstrap completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
