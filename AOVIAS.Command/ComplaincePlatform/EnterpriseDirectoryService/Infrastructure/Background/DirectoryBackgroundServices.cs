using Azure.Messaging.ServiceBus;
using EnterpriseDirectoryService.Application.Contracts;
using EnterpriseDirectoryService.Application.Directory.Indexing;
using EnterpriseDirectoryService.Application.Directory.Platform;
using EnterpriseDirectoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirectoryService.Infrastructure.Background;

public sealed class IndexRebuildSchedulerHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<IndexRebuildSchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue("Directory:IndexRebuildIntervalMinutes", 360);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EnterpriseDirectoryDbContext>();
                var projector = scope.ServiceProvider.GetRequiredService<IDirectoryIndexProjector>();
                var orgIds = await db.DirectorySnapshots.Select(s => s.Id).AnyAsync(stoppingToken)
                    ? await db.DirectoryIndexes.Select(x => x.OrganizationId).Distinct().ToListAsync(stoppingToken)
                    : [];
                if (orgIds.Count == 0)
                {
                    logger.LogInformation("No organizations to rebuild; scheduler idle.");
                }
                else
                {
                    foreach (var orgId in orgIds)
                        await projector.RebuildOrganizationAsync(orgId, Guid.NewGuid().ToString(), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Directory index rebuild scheduler failed");
            }
            await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
        }
    }
}

public sealed class DirectoryEventConsumerHostedService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<DirectoryEventConsumerHostedService> logger) : BackgroundService
{
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogInformation("ServiceBus not configured; directory event consumer disabled.");
            return;
        }

        var client = new ServiceBusClient(connectionString);
        _processor = client.CreateProcessor("workforce-events", new ServiceBusProcessorOptions { AutoCompleteMessages = false, MaxConcurrentCalls = 2 });
        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += args => { logger.LogError(args.Exception, "workforce-events processor error"); return Task.CompletedTask; };
        await _processor.StartProcessingAsync(stoppingToken);
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        using var scope = scopeFactory.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IDirectoryProjectionRegistry>();
        var envelope = new DirectoryEventEnvelope
        {
            Type = args.Message.Subject ?? "Unknown",
            Payload = args.Message.Body.ToString(),
            AggregateId = Guid.TryParse(args.Message.CorrelationId, out var id) ? id : Guid.NewGuid(),
            OccurredOn = args.Message.EnqueuedTime.UtcDateTime
        };
        await registry.ProcessAsync(envelope);
        await args.CompleteMessageAsync(args.Message);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor != null) await _processor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

public sealed class DirectoryOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<DirectoryOutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EnterpriseDirectoryDbContext>();
                var pending = await db.OutboxMessages.Where(x => x.ProcessedOn == null).OrderBy(x => x.OccurredOn).Take(50).ToListAsync(stoppingToken);
                foreach (var msg in pending)
                {
                    msg.ProcessedOn = DateTime.UtcNow;
                }
                if (pending.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Directory outbox processor failed");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
