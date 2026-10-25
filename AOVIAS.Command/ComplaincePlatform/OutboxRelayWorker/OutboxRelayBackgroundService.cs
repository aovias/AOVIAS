using Azure.Messaging.ServiceBus;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class OutboxRelayBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<OutboxRelayBackgroundService> _logger;
    private ServiceBusSender? _sender;

    public OutboxRelayBackgroundService(
        IServiceScopeFactory scopeFactory,
        ServiceBusClient serviceBusClient,
        ILogger<OutboxRelayBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _serviceBusClient = serviceBusClient;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _sender = _serviceBusClient.CreateSender("compliance-events");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RelayBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox relay loop failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task RelayBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ComplianceDbContext>();
        var sender = _sender ?? throw new InvalidOperationException("ServiceBus sender not initialized.");

        var pending = await dbContext.OutboxMessages
            .Where(x => x.RelayProcessedOn == null && x.RetryCount < OutboxConstants.MaxRetries)
            .OrderBy(x => x.OccurredOn)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            try
            {
                var serviceBusMessage = new ServiceBusMessage(message.Payload)
                {
                    MessageId = message.Id.ToString(),
                    Subject = message.Type,
                    CorrelationId = message.AggregateId.ToString()
                };

                await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                message.RelayProcessedOn = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount += 1;
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_sender is not null)
        {
            await _sender.DisposeAsync();
        }

        await _serviceBusClient.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
