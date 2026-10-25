using Azure.Messaging.ServiceBus;
using Cronos;
using IntegrationService.Application.Contracts;
using IntegrationService.Application.Integration.Platform;
using IntegrationService.Application.Integration.Synchronization;
using IntegrationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationService.Infrastructure.Background;

public sealed class SyncSchedulerHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SyncSchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue("Integration:SyncSchedulerIntervalSeconds", 60);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
                var engine = scope.ServiceProvider.GetRequiredService<ISynchronizationEngine>();
                var due = await db.SynchronizationSchedules
                    .Where(s => s.IsEnabled && s.NextRunUtc <= DateTime.UtcNow)
                    .ToListAsync(stoppingToken);
                foreach (var schedule in due)
                {
                    var job = await db.SynchronizationJobs.FirstOrDefaultAsync(j => j.Id == schedule.SynchronizationJobId && j.IsEnabled, stoppingToken);
                    if (job is null) continue;
                    await engine.RunAsync(job.Id, Guid.NewGuid().ToString(), null, stoppingToken);
                    schedule.LastRunUtc = DateTime.UtcNow;
                    schedule.NextRunUtc = GetNextCronOccurrence(schedule.CronExpression, DateTime.UtcNow);
                }
                if (due.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) { logger.LogError(ex, "Sync scheduler failed"); }
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }

    internal static DateTime GetNextCronOccurrence(string cronExpression, DateTime fromUtc)
    {
        try
        {
            var expr = CronExpression.Parse(cronExpression, CronFormat.Standard);
            return expr.GetNextOccurrence(fromUtc, TimeZoneInfo.Utc) ?? fromUtc.AddHours(6);
        }
        catch
        {
            return fromUtc.AddHours(6);
        }
    }
}

public sealed class IntegrationEventConsumerHostedService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<IntegrationEventConsumerHostedService> logger) : BackgroundService
{
    private readonly List<ServiceBusProcessor> _processors = [];
    private ServiceBusClient? _client;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogInformation("ServiceBus not configured; integration consumer disabled.");
            return;
        }

        _client = new ServiceBusClient(connectionString);
        foreach (var topic in new[] { "workforce-events", "directory-events", "compliance-events" })
        {
            var processor = _client.CreateProcessor(topic, new ServiceBusProcessorOptions { AutoCompleteMessages = false, MaxConcurrentCalls = 2 });
            processor.ProcessMessageAsync += OnMessageAsync;
            processor.ProcessErrorAsync += OnErrorAsync;
            await processor.StartProcessingAsync(stoppingToken);
            _processors.Add(processor);
            logger.LogInformation("Integration consumer subscribed to {Topic}", topic);
        }

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        using var scope = scopeFactory.CreateScope();
        var webhooks = scope.ServiceProvider.GetRequiredService<IWebhookService>();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var eventType = args.Message.Subject ?? "Unknown";
        var orgId = Guid.TryParse(args.Message.CorrelationId, out var id) ? id : Guid.Empty;
        if (orgId != Guid.Empty)
        {
            await webhooks.DeliverOutboundAsync(eventType, args.Message.Body.ToString(), orgId, args.CancellationToken);
            var jobs = await db.SynchronizationJobs.AsNoTracking()
                .Where(j => j.OrganizationId == orgId && j.IsEnabled && !j.IsDeleted)
                .Select(j => j.Id)
                .ToListAsync(args.CancellationToken);
            if (jobs.Count > 0)
                logger.LogDebug("Event {EventType} received for org {OrgId}; {JobCount} sync jobs may consume watermark updates", eventType, orgId, jobs.Count);
        }
        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Integration consumer error on {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in _processors)
            await processor.StopProcessingAsync(cancellationToken);
        if (_client is not null)
            await _client.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}

public sealed class IntegrationOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<IntegrationOutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
                var pending = await db.OutboxMessages.Where(x => x.ProcessedOn == null).OrderBy(x => x.OccurredOn).Take(50).ToListAsync(stoppingToken);
                foreach (var msg in pending) msg.ProcessedOn = DateTime.UtcNow;
                if (pending.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) { logger.LogError(ex, "Integration outbox processor failed"); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

public sealed class WebhookRetryHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookRetryHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
                var dlq = await db.WebhookDeliveries.CountAsync(d => d.IsDeadLetter, stoppingToken);
                if (dlq > 0) logger.LogInformation("Webhook DLQ count: {Count}", dlq);
            }
            catch (Exception ex) { logger.LogError(ex, "Webhook retry monitor failed"); }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
