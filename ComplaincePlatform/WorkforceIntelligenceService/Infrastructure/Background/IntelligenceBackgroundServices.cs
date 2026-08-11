using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WorkforceIntelligenceService.Application.Contracts;
using WorkforceIntelligenceService.Application.Intelligence.Platform;
using WorkforceIntelligenceService.Domain.ReadModels;
using WorkforceIntelligenceService.Infrastructure.Persistence;

namespace WorkforceIntelligenceService.Infrastructure.Background;

public sealed class SnapshotSchedulerHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SnapshotSchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue("Intelligence:SnapshotIntervalMinutes", 60);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WorkforceIntelligenceDbContext>();
                var snapshot = scope.ServiceProvider.GetRequiredService<IWorkforceSnapshotService>();
                var forecast = scope.ServiceProvider.GetRequiredService<IForecastEngine>();
                var recommendations = scope.ServiceProvider.GetRequiredService<IRecommendationOrchestrator>();
                var outbox = scope.ServiceProvider.GetRequiredService<IIntelligenceOutboxWriter>();

                var orgIds = await db.WorkforceSnapshots.Select(s => s.OrganizationId).Distinct().ToListAsync(stoppingToken);
                if (orgIds.Count == 0)
                {
                    orgIds.Add(Guid.Empty);
                }

                foreach (var orgId in orgIds.Where(id => id != Guid.Empty))
                {
                    var correlationId = Guid.NewGuid().ToString();
                    await snapshot.RefreshOrganizationAsync(orgId, correlationId, false, stoppingToken);
                    await forecast.GenerateAsync(orgId, "CertificationExpiry", correlationId, stoppingToken);
                    await forecast.GenerateAsync(orgId, "Attrition", correlationId, stoppingToken);
                    await recommendations.GenerateAsync(orgId, correlationId, stoppingToken);
                    await outbox.EnqueueAsync("DashboardRefreshed", orgId, new { organizationId = orgId }, stoppingToken);
                }

                logger.LogInformation("Snapshot scheduler completed for {Count} organizations", orgIds.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Snapshot scheduler failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
        }
    }
}

public sealed class IntelligenceEventConsumerHostedService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<IntelligenceEventConsumerHostedService> logger) : BackgroundService
{
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogInformation("ServiceBus not configured; intelligence event consumer disabled.");
            return;
        }

        var client = new ServiceBusClient(connectionString);
        _processor = client.CreateProcessor("workforce-events", new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 2
        });

        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "workforce-events processor error");
            return Task.CompletedTask;
        };

        await _processor.StartProcessingAsync(stoppingToken);
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        using var scope = scopeFactory.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IIntelligenceProjectionRegistry>();
        var body = args.Message.Body.ToString();
        var envelope = new IntelligenceEventEnvelope
        {
            Type = args.Message.Subject ?? "Unknown",
            Payload = body,
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

public sealed class IntelligenceOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<IntelligenceOutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WorkforceIntelligenceDbContext>();
                var pending = await db.OutboxMessages.Where(m => m.ProcessedOn == null).OrderBy(m => m.OccurredOn).Take(50).ToListAsync(stoppingToken);
                foreach (var msg in pending)
                {
                    logger.LogInformation("Intelligence outbox {Type} aggregate {AggregateId}", msg.Type, msg.AggregateId);
                    msg.ProcessedOn = DateTime.UtcNow;
                }
                if (pending.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Intelligence outbox processor failed");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
