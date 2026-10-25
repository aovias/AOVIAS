using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;

namespace OutboxRelayWorker;

public sealed class IntegrationOutboxRelayBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<IntegrationOutboxRelayBackgroundService> _logger;
    private ServiceBusSender? _sender;

    public IntegrationOutboxRelayBackgroundService(
        ServiceBusClient serviceBusClient,
        IConfiguration configuration,
        ILogger<IntegrationOutboxRelayBackgroundService> logger)
    {
        _serviceBusClient = serviceBusClient;
        _configuration = configuration;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _sender = _serviceBusClient.CreateSender("integration-events");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "integration-events sender not available; integration relay disabled.");
        }
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_sender is null) return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RelayAsync(_configuration.GetConnectionString("IntegrationConnection"), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Integration outbox relay loop failed.");
            }
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task RelayAsync(string? connectionString, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<RelayOutboxDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new RelayOutboxDbContext(options);
        var sender = _sender ?? throw new InvalidOperationException("Sender not initialized.");

        var pending = await db.OutboxMessages
            .Where(x => x.ProcessedOn != null && x.RelayProcessedOn == null)
            .OrderBy(x => x.OccurredOn)
            .Take(100)
            .ToListAsync(cancellationToken);

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

        if (pending.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_sender is not null) await _sender.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
