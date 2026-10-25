using PersonService.Infrastructure.Persistence;
using PersonService.Infrastructure.MasterData;
using Microsoft.EntityFrameworkCore;

namespace PersonService.Infrastructure.Background;

public sealed class PersonMasterDataBootstrapHostedService : BackgroundService
{
    private readonly IServiceProvider _services;

    public PersonMasterDataBootstrapHostedService(IServiceProvider services) => _services = services;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PersonDbContext>();
        await PersonMasterDataSeeder.SeedAsync(db, stoppingToken);
    }
}

public class PersonOutboxProcessor : BackgroundService
{
    private const int MaxRetries = 5;
    private readonly IServiceProvider _services;
    private readonly ILogger<PersonOutboxProcessor> _logger;

    public PersonOutboxProcessor(IServiceProvider services, ILogger<PersonOutboxProcessor> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PersonDbContext>();
                var pending = await db.OutboxMessages
                    .Where(m => m.ProcessedOn == null && m.RetryCount < MaxRetries)
                    .OrderBy(m => m.OccurredOn).Take(20).ToListAsync(stoppingToken);
                foreach (var msg in pending)
                {
                    _logger.LogInformation("Person outbox {Type} {AggregateId}", msg.Type, msg.AggregateId);
                    msg.ProcessedOn = DateTime.UtcNow;
                }
                if (pending.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) { _logger.LogError(ex, "Person outbox error"); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
