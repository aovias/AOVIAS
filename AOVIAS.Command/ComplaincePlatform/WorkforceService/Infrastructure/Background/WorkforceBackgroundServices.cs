using WorkforceService.Infrastructure.Persistence;
using WorkforceService.Infrastructure.MasterData;
using Microsoft.EntityFrameworkCore;

namespace WorkforceService.Infrastructure.Background;

public sealed class WorkforceMasterDataBootstrapHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    public WorkforceMasterDataBootstrapHostedService(IServiceProvider services) => _services = services;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkforceDbContext>();
        await WorkforceMasterDataSeeder.SeedAsync(db, stoppingToken);
    }
}

public class WorkforceOutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WorkforceOutboxProcessor> _logger;

    public WorkforceOutboxProcessor(IServiceProvider services, ILogger<WorkforceOutboxProcessor> logger)
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
                var db = scope.ServiceProvider.GetRequiredService<WorkforceDbContext>();
                var pending = await db.OutboxMessages.Where(m => m.ProcessedOn == null && m.RetryCount < 5)
                    .OrderBy(m => m.OccurredOn).Take(20).ToListAsync(stoppingToken);
                foreach (var msg in pending)
                {
                    _logger.LogInformation("Workforce outbox {Type} {AggregateId}", msg.Type, msg.AggregateId);
                    msg.ProcessedOn = DateTime.UtcNow;
                }
                if (pending.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) { _logger.LogError(ex, "Workforce outbox error"); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
